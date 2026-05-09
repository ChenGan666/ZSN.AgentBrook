using System.Text;
using System.Globalization;
using System.Linq;
using Npgsql;
using Npgsql.TypeMapping;
using NpgsqlTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.Core.Repositories
{
    /// <summary>
    /// Apache AGE 图数据库仓储实现
    /// </summary>
    public class AgeGraphRepository : IGraphRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AgeGraphRepository> _logger;
        private bool _initialized = false;
        private readonly object _initLock = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        public AgeGraphRepository(
            IConfiguration configuration,
            ILogger<AgeGraphRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // 从配置中获取连接字符串
            var connection = configuration["DbConnectionStrings:KnowledgeBaseDb:Connection"];
            if (string.IsNullOrEmpty(connection))
            {
                throw new InvalidOperationException("KnowledgeBaseDb connection string not found in configuration.");
            }
            _connectionString = connection;
        }

        /// <summary>
        /// 初始化图数据库
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // 加载AGE扩展
            await using (var cmd = new NpgsqlCommand("LOAD 'age'", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 设置search_path
            await using (var cmd = new NpgsqlCommand(
                "SET search_path = ag_catalog, \"$user\", public", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            _initialized = true;
        }

        /// <summary>
        /// 准备 AGE 环境（每次新连接都需要调用）
        /// </summary>
        private async Task PrepareAgeEnvironmentAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
        {
            // 加载AGE扩展
            await using (var cmd = new NpgsqlCommand("LOAD 'age'", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 设置search_path
            await using (var cmd = new NpgsqlCommand(
                "SET search_path = ag_catalog, \"$user\", public", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 创建图
        /// </summary>
        public async Task CreateGraphAsync(
            string graphName,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await PrepareAgeEnvironmentAsync(conn, cancellationToken);

            var cypher = $"SELECT * FROM ag_catalog.create_graph('{graphName}');";

            await using var cmd = new NpgsqlCommand(cypher, conn);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// 删除图
        /// </summary>
        public async Task DropGraphAsync(
            string graphName,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await PrepareAgeEnvironmentAsync(conn, cancellationToken);

            var cypher = $"SELECT * FROM ag_catalog.drop_graph('{graphName}', true);";

            await using var cmd = new NpgsqlCommand(cypher, conn);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// 检查图是否存在
        /// </summary>
        public async Task<bool> GraphExistsAsync(
            string graphName,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var cypher = @"
                SELECT EXISTS(
                    SELECT 1 FROM ag_catalog.ag_graph WHERE name = @graphName
                );";

            await using var cmd = new NpgsqlCommand(cypher, conn);
            cmd.Parameters.AddWithValue("graphName", graphName);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null && Convert.ToBoolean(result);
        }

        /// <summary>
        /// 执行Cypher查询
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ExecuteCypherAsync(
            string graphName,
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // 每次新连接都需要重新设置 AGE 环境
            await PrepareAgeEnvironmentAsync(conn, cancellationToken);

            // AGE查询格式: SELECT * FROM cypher('graph_name', $$ ... $$) as (...) ...
            var ageQuery = BuildAgeQuery(graphName, cypherQuery);

            await using var cmd = new NpgsqlCommand(ageQuery, conn);

            // 添加参数
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var results = new List<Dictionary<string, object>>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            // 解析结果
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    // 所有列现在都应该是 text 类型
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[fieldName] = value;
                }
                results.Add(row);
            }

            return results;
        }

        /// <summary>
        /// 创建顶点
        /// </summary>
        public async Task<string> CreateVertexAsync(
            string graphName,
            string label,
            Dictionary<string, object> properties,
            CancellationToken cancellationToken = default)
        {
            var propString = PropertiesToCypherString(properties);
            // 只返回 id，避免返回整个顶点对象
            var cypher = $"CREATE (v:{label} {propString}) RETURN id(v)";

            var results = await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);

            // 从结果中提取vertex ID（列名是 a0, a1, a2...）
            if (results.FirstOrDefault() is Dictionary<string, object> firstRow &&
                firstRow.TryGetValue("a0", out var idObj))
            {
                return idObj?.ToString() ?? Guid.NewGuid().ToString();
            }

            throw new InvalidOperationException("Failed to create vertex");
        }

        /// <summary>
        /// 批量创建顶点
        /// </summary>
        public async Task<List<string>> CreateVerticesAsync(
            string graphName,
            string label,
            List<Dictionary<string, object>> propertiesList,
            CancellationToken cancellationToken = default)
        {
            var vertexIds = new List<string>();

            // 使用事务批量创建
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await PrepareAgeEnvironmentAsync(conn, cancellationToken);
            using var transaction = await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var properties in propertiesList)
                {
                    var propString = PropertiesToCypherString(properties);
                    // 只返回 id，避免返回顶点对象
                    var cypher = $"CREATE (v:{label} {propString}) RETURN id(v)";

                    var ageQuery = BuildAgeQuery(graphName, cypher);
                    await using var cmd = new NpgsqlCommand(ageQuery, conn, transaction);
                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                    if (await reader.ReadAsync(cancellationToken))
                    {
                        // 直接读取 id 值（列名是 a0）
                        var vertexId = reader.GetValue(0)?.ToString() ?? Guid.NewGuid().ToString();
                        vertexIds.Add(vertexId);
                    }

                    await reader.CloseAsync();
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return vertexIds;
        }

        /// <summary>
        /// 创建边
        /// </summary>
        public async Task<string> CreateEdgeAsync(
            string graphName,
            string startVertexId,
            string endVertexId,
            string edgeLabel,
            Dictionary<string, object>? properties = null,
            CancellationToken cancellationToken = default)
        {
            var propString = PropertiesToCypherString(properties ?? new());
            var cypher = $"MATCH (a), (b) WHERE id(a) = {startVertexId} AND id(b) = {endVertexId} " +
                        $"CREATE (a)-[r:{edgeLabel} {propString}]->(b) RETURN id(r)";

            var results = await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);

            // 从结果中提取edge ID（列名是 a0）
            if (results.FirstOrDefault() is Dictionary<string, object> firstRow &&
                firstRow.TryGetValue("a0", out var edgeObj))
            {
                return edgeObj?.ToString() ?? Guid.NewGuid().ToString();
            }

            throw new InvalidOperationException("Failed to create edge");
        }

        /// <summary>
        /// 批量创建边
        /// </summary>
        public async Task<List<string>> CreateEdgesAsync(
            string graphName,
            List<EdgeDefinition> edges,
            CancellationToken cancellationToken = default)
        {
            var edgeIds = new List<string>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await PrepareAgeEnvironmentAsync(conn, cancellationToken);
            using var transaction = await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var edge in edges)
                {
                    var propString = PropertiesToCypherString(edge.Properties);
                    var cypher = $"MATCH (a), (b) WHERE id(a) = {edge.StartVertexId} AND id(b) = {edge.EndVertexId} " +
                                $"CREATE (a)-[r:{edge.EdgeLabel} {propString}]->(b) RETURN r";

                    var ageQuery = BuildAgeQuery(graphName, cypher);
                    await using var cmd = new NpgsqlCommand(ageQuery, conn, transaction);
                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                    if (await reader.ReadAsync(cancellationToken))
                    {
                        var edgeId = ExtractEdgeId(reader.GetValue(0));
                        edgeIds.Add(edgeId);
                    }

                    await reader.CloseAsync();
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return edgeIds;
        }

        /// <summary>
        /// 查询顶点
        /// </summary>
        public async Task<Dictionary<string, object>?> GetVertexAsync(
            string graphName,
            string vertexId,
            CancellationToken cancellationToken = default)
        {
            // 返回 id 和 properties，而不是顶点对象
            var cypher = $"MATCH (v) WHERE id(v) = {vertexId} RETURN id(v) as id, properties(v) as props";

            var results = await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);

            if (results.FirstOrDefault() is Dictionary<string, object> firstRow)
            {
                // 将 a0, a1 转换为 id, props
                var vertexDict = new Dictionary<string, object>();

                if (firstRow.TryGetValue("a0", out var idValue))
                    vertexDict["id"] = idValue;

                if (firstRow.TryGetValue("a1", out var propsValue))
                    vertexDict["props"] = propsValue;

                return vertexDict;
            }

            return null;
        }

        /// <summary>
        /// 查询边
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetEdgesAsync(
            string graphName,
            string vertexId,
            string? edgeLabel = null,
            CancellationToken cancellationToken = default)
        {
            var labelFilter = string.IsNullOrEmpty(edgeLabel) ? "" : $":{edgeLabel}";
            var cypher = $"MATCH (v)-[r{labelFilter}]->(related) WHERE id(v) = {vertexId} RETURN r";

            var results = await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);

            var edges = new List<Dictionary<string, object>>();
            foreach (var row in results)
            {
                if (row.TryGetValue("r", out var edgeObj))
                {
                    var edgeDict = ParseAgTypeToDict(edgeObj);
                    if (edgeDict != null)
                        edges.Add(edgeDict);
                }
            }

            return edges;
        }

        /// <summary>
        /// 更新顶点属性
        /// </summary>
        public async Task UpdateVertexAsync(
            string graphName,
            string vertexId,
            Dictionary<string, object> properties,
            CancellationToken cancellationToken = default)
        {
            var setClause = string.Join(", ", properties.Select(p => $"v.{p.Key} = {FormatValue(p.Value)}"));
            var cypher = $"MATCH (v) WHERE id(v) = {vertexId} SET {setClause} RETURN id(v)";

            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        /// <summary>
        /// 删除顶点
        /// </summary>
        public async Task DeleteVertexAsync(
            string graphName,
            string vertexId,
            CancellationToken cancellationToken = default)
        {
            var cypher = $"MATCH (v) WHERE id(v) = {vertexId} DETACH DELETE v";
            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        /// <summary>
        /// 删除边
        /// </summary>
        public async Task DeleteEdgeAsync(
            string graphName,
            string edgeId,
            CancellationToken cancellationToken = default)
        {
            var cypher = $"MATCH ()-[r]->() WHERE id(r) = {edgeId} DELETE r";
            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        #region Private Helper Methods

        /// <summary>
        /// 确保已初始化
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                // 自动初始化（线程安全）
                lock (_initLock)
                {
                    if (!_initialized)
                    {
                        // 使用同步方式等待异步初始化完成
                        InitializeAsync().GetAwaiter().GetResult();
                    }
                }
            }
        }

        /// <summary>
        /// 构建AGE查询语句
        /// </summary>
        private string BuildAgeQuery(string graphName, string cypherQuery)
        {
            // 计算返回列数
            int columnCount = 1;

            // 检查是否有多列返回
            if (cypherQuery.Contains("RETURN"))
            {
                var returnIndex = cypherQuery.IndexOf("RETURN");
                var afterReturn = cypherQuery.Substring(returnIndex + 6);

                // 简单统计逗号数量来确定列数
                var commaCount = afterReturn.Count(c => c == ',');
                columnCount = commaCount + 1;
            }

            // 构建 SELECT 子句，根据列数动态生成
            var columns = string.Join(", ", Enumerable.Range(0, columnCount).Select(i => $"a{i} text"));
            var cypherResult = $"cypher_result({columns})";

            return $"SELECT * FROM ag_catalog.cypher('{graphName}', $$ {cypherQuery} $$) as {cypherResult}";
        }

        /// <summary>
        /// 将属性字典转换为Cypher字符串
        /// </summary>
        private string PropertiesToCypherString(Dictionary<string, object> properties)
        {
            if (properties == null || !properties.Any())
                return "{}";

            var props = properties.Select(p =>
                $"{p.Key}: {FormatValue(p.Value)}"
            );

            return $"{{{string.Join(", ", props)}}}";
        }

        /// <summary>
        /// 格式化值用于Cypher查询
        /// </summary>
        private string FormatValue(object value)
        {
            return value switch
            {
                string s => $"'{s.Replace("'", "\\'")}'",
                int i => i.ToString(),
                long l => l.ToString(),
                double d => d.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                decimal dec => dec.ToString(CultureInfo.InvariantCulture),
                bool b => b.ToString().ToLower(),
                DateTime dt => $"'{dt:yyyy-MM-ddTHH:mm:ss.fffK}'",
                null or DBNull => "null",
                _ => $"'{value?.ToString()?.Replace("'", "\\'") ?? "null"}'"
            };
        }

        /// <summary>
        /// 从agtype对象中提取顶点ID
        /// </summary>
        private string ExtractVertexId(object vertexObj)
        {
            // agtype对象通常是JSON字符串，需要解析
            if (vertexObj is string jsonStr)
            {
                // 简单解析，实际应使用JSON解析器
                var idMatch = System.Text.RegularExpressions.Regex.Match(jsonStr, @"""id""\s*:\s*(\d+)");
                if (idMatch.Success)
                {
                    return idMatch.Groups[1].Value;
                }
            }
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 从agtype对象中提取边ID
        /// </summary>
        private string ExtractEdgeId(object edgeObj)
        {
            if (edgeObj is string jsonStr)
            {
                var idMatch = System.Text.RegularExpressions.Regex.Match(jsonStr, @"""id""\s*:\s*(\d+)");
                if (idMatch.Success)
                {
                    return idMatch.Groups[1].Value;
                }
            }
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 将agtype对象解析为字典
        /// </summary>
        private Dictionary<string, object>? ParseAgTypeToDict(object agTypeObj)
        {
            try
            {
                if (agTypeObj is string jsonStr)
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse agtype object");
            }
            return null;
        }

        #endregion

        #region 高级查询方法

        /// <summary>
        /// 查询顶点的邻居
        /// </summary>
        public async Task<List<NeighborResult>> GetNeighborsAsync(
            string graphName,
            string vertexId,
            string[]? edgeLabels = null,
            string direction = "outgoing",
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            // 构建方向模式
            var pattern = direction.ToLower() switch
            {
                "outgoing" => "-[r]->(neighbor)",
                "incoming" => "<-[r]-(neighbor)",
                "both" => "-[r]-(neighbor)",
                _ => "-[r]->(neighbor)"
            };

            // 构建边标签过滤
            var edgeFilter = edgeLabels != null && edgeLabels.Length > 0
                ? string.Join("|", edgeLabels)
                : null;

            var cypher = $@"
                MATCH (v){{id: @vertexId}}{pattern}
                RETURN id(neighbor) as vertex_id, neighbor, r, labels(r)[0] as edge_label
                {(edgeFilter != null ? $"WHERE edge_label IN [{string.Join(", ", edgeLabels.Select((_, i) => $"@edgeLabel{i}"))}]" : "")}";

            var parameters = new Dictionary<string, object>
            {
                { "vertexId", vertexId }
            };

            if (edgeFilter != null)
            {
                for (int i = 0; i < edgeLabels.Length; i++)
                {
                    parameters[$"edgeLabel{i}"] = edgeLabels[i];
                }
            }

            var results = await ExecuteCypherAsync(graphName, cypher, parameters, cancellationToken);

            var neighbors = new List<NeighborResult>();
            foreach (var row in results)
            {
                neighbors.Add(new NeighborResult
                {
                    VertexId = row.GetValueOrDefault("vertex_id", "").ToString() ?? "",
                    Properties = ParseVertexProperties(row.GetValueOrDefault("neighbor", "{}")?.ToString()),
                    Edge = ParseEdgeProperties(row.GetValueOrDefault("r", "{}")?.ToString()),
                    RelationType = row.GetValueOrDefault("edge_label", "").ToString() ?? ""
                });
            }

            _logger.LogDebug("Found {Count} neighbors for vertex {VertexId}", neighbors.Count, vertexId);
            return neighbors;
        }

        /// <summary>
        /// 多跳关系查询
        /// </summary>
        public async Task<List<GraphPathResult>> FindReachableVerticesAsync(
            string graphName,
            string startVertexId,
            int maxHops = 3,
            string[]? edgeLabels = null,
            string direction = "outgoing",
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            // 构建可变长度路径模式
            var directionPattern = direction.ToLower() switch
            {
                "outgoing" => "->",
                "incoming" => "<-",
                "both" => "-",
                _ => "->"
            };

            var edgePattern = edgeLabels != null && edgeLabels.Length > 0
                ? string.Join("|", edgeLabels)
                : "";

            var cypher = $@"
                MATCH path = (start){{id: @startVertexId}}{directionPattern}[{(string.IsNullOrEmpty(edgePattern) ? "" : ":" + edgePattern)}*1..@maxHops](end)
                RETURN path, [node in nodes(path) | id(node)] as vertex_ids,
                       length(path) as path_length
                ORDER BY path_length";

            var results = await ExecuteCypherAsync(graphName, cypher,
                new Dictionary<string, object>
                {
                    { "startVertexId", startVertexId },
                    { "maxHops", maxHops }
                }, cancellationToken);

            var paths = new List<GraphPathResult>();
            foreach (var row in results)
            {
                paths.Add(new GraphPathResult
                {
                    PathLength = Convert.ToInt32(row.GetValueOrDefault("path_length", 0)),
                    VertexIds = ParseStringArray(row.GetValueOrDefault("vertex_ids", "[]")?.ToString())
                });
            }

            return paths;
        }

        /// <summary>
        /// 查询两个顶点之间的最短路径
        /// </summary>
        public async Task<GraphPathResult?> FindShortestPathAsync(
            string graphName,
            string startVertexId,
            string endVertexId,
            string[]? edgeLabels = null,
            int maxDepth = 10,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var edgePattern = edgeLabels != null && edgeLabels.Length > 0
                ? ":" + string.Join("|", edgeLabels)
                : "";

            var cypher = $@"
                MATCH path = shortestPath(
                    (start){{id: @startVertexId}}[*..@maxDepth]({{(string.IsNullOrEmpty(edgePattern) ? "" : edgePattern)}}(end){{id: @endVertexId}}
                )
                RETURN [node in nodes(path) | id(node)] as vertex_ids,
                       length(path) as path_length,
                       [rel in relationships(path) | properties(rel)] as edges
                LIMIT 1";

            var results = await ExecuteCypherAsync(graphName, cypher,
                new Dictionary<string, object>
                {
                    { "startVertexId", startVertexId },
                    { "endVertexId", endVertexId },
                    { "maxDepth", maxDepth }
                }, cancellationToken);

            if (results.Count == 0)
            {
                return null;
            }

            var row = results[0];
            return new GraphPathResult
            {
                PathLength = Convert.ToInt32(row.GetValueOrDefault("path_length", 0)),
                VertexIds = ParseStringArray(row.GetValueOrDefault("vertex_ids", "[]")?.ToString())
            };
        }

        /// <summary>
        /// 查询两个顶点之间的所有路径
        /// </summary>
        public async Task<List<GraphPathResult>> FindAllPathsAsync(
            string graphName,
            string startVertexId,
            string endVertexId,
            int maxPathLength = 5,
            string[]? edgeLabels = null,
            int maxPaths = 100,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var edgePattern = edgeLabels != null && edgeLabels.Length > 0
                ? ":" + string.Join("|", edgeLabels)
                : "";

            var cypher = $@"
                MATCH path = (start){{id: @startVertexId}}[*..@maxPathLength]({{(string.IsNullOrEmpty(edgePattern) ? "" : edgePattern)}}(end){{id: @endVertexId}}
                RETURN [node in nodes(path) | id(node)] as vertex_ids,
                       length(path) as path_length
                ORDER BY path_length
                LIMIT @maxPaths";

            var results = await ExecuteCypherAsync(graphName, cypher,
                new Dictionary<string, object>
                {
                    { "startVertexId", startVertexId },
                    { "endVertexId", endVertexId },
                    { "maxPathLength", maxPathLength },
                    { "maxPaths", maxPaths }
                }, cancellationToken);

            var paths = new List<GraphPathResult>();
            foreach (var row in results)
            {
                paths.Add(new GraphPathResult
                {
                    PathLength = Convert.ToInt32(row.GetValueOrDefault("path_length", 0)),
                    VertexIds = ParseStringArray(row.GetValueOrDefault("vertex_ids", "[]")?.ToString())
                });
            }

            return paths;
        }

        /// <summary>
        /// 提取子图
        /// </summary>
        public async Task<SubGraphResult> ExtractSubGraphAsync(
            string graphName,
            string centerVertexId,
            int hops = 1,
            string[]? edgeLabels = null,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var edgePattern = edgeLabels != null && edgeLabels.Length > 0
                ? ":" + string.Join("|", edgeLabels)
                : "";

            var cypher = $@"
                MATCH (center){{id: @centerVertexId}}-[*0..@hops]{(string.IsNullOrEmpty(edgePattern) ? "" : edgePattern)}-(neighbor)
                RETURN collect(DISTINCT center) as vertices,
                       collect(DISTINCT neighbor) as more_vertices,
                       collect(r) as edges";

            var results = await ExecuteCypherAsync(graphName, cypher,
                new Dictionary<string, object>
                {
                    { "centerVertexId", centerVertexId },
                    { "hops", hops }
                }, cancellationToken);

            // 合并顶点和边
            var subGraph = new SubGraphResult();

            if (results.Count > 0)
            {
                var row = results[0];

                // 合并顶点
                var centerVertices = ParseVertexList(row.GetValueOrDefault("vertices", "[]")?.ToString());
                var moreVertices = ParseVertexList(row.GetValueOrDefault("more_vertices", "[]")?.ToString());
                subGraph.Vertices = centerVertices.Concat(moreVertices).Distinct().ToList();

                // 解析边
                var edgesData = row.GetValueOrDefault("edges", "[]")?.ToString() ?? "[]";
                subGraph.Edges = ParseEdgeList(edgesData);
            }

            return subGraph;
        }

        /// <summary>
        /// 执行复杂的图遍历查询
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ExecuteGraphTraversalAsync(
            string graphName,
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteCypherAsync(graphName, cypherQuery, parameters, cancellationToken);
        }

        #region Index Management Methods

        /// <summary>
        /// 创建顶点标签索引
        /// </summary>
        public async Task CreateLabelIndexAsync(
            string graphName,
            string label,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            var indexName = $"idx_{label}_label";

            var cypher = $@"
                CREATE INDEX IF NOT EXISTS {indexName}
                FOR (v:{label})
                ON (v.id)";

            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        /// <summary>
        /// 创建顶点属性索引
        /// </summary>
        public async Task CreatePropertyIndexAsync(
            string graphName,
            string label,
            string property,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            var indexName = $"idx_{label}_{property}";

            var cypher = $@"
                CREATE INDEX IF NOT EXISTS {indexName}
                FOR (v:{label})
                ON (v.{property})";

            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        /// <summary>
        /// 创建边标签索引
        /// </summary>
        public async Task CreateEdgeLabelIndexAsync(
            string graphName,
            string edgeLabel,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            var indexName = $"idx_edge_{edgeLabel}";

            var cypher = $@"
                CREATE INDEX IF NOT EXISTS {indexName}
                FOR ()-[r:{edgeLabel}]-()
                ON (r.id)";

            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        /// <summary>
        /// 列出图的所有索引
        /// </summary>
        public async Task<List<GraphIndexInfo>> ListIndexesAsync(
            string graphName,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            // PostgreSQL 查询来获取索引信息
            var sql = @"
                SELECT
                    i.indexname as index_name,
                    i.indexdef as index_definition,
                    pg_size_pretty(pg_relation_size(i.indexrelid)) as index_size
                FROM
                    pg_indexes i
                WHERE
                    i.schemaname = @graphSchema
                    AND i.tablename LIKE 'ag_%'
                ORDER BY
                    i.indexname";

            var indexes = new List<GraphIndexInfo>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("graphSchema", $"graph_{graphName.ToLower()}");

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var indexName = reader.GetString(0);
                    var indexDef = reader.GetString(1);

                    indexes.Add(new GraphIndexInfo
                    {
                        IndexName = indexName,
                        IndexType = DetermineIndexType(indexDef),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list indexes for graph {GraphName}", graphName);
            }

            return indexes;
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        public async Task DropIndexAsync(
            string graphName,
            string indexName,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var cypher = $@"
                DROP INDEX IF EXISTS {indexName}";

            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        /// <summary>
        /// 创建复合索引（多属性索引）
        /// </summary>
        public async Task CreateCompositeIndexAsync(
            string graphName,
            string label,
            string[] properties,
            CancellationToken cancellationToken = default)
        {
            if (properties == null || properties.Length == 0)
                throw new ArgumentException("At least one property must be specified", nameof(properties));

            EnsureInitialized();
            var propertyList = string.Join(", ", properties.Select(p => $"v.{p}"));
            var indexName = $"idx_{label}_composite_{string.Join("_", properties)}";

            var cypher = $@"
                CREATE INDEX IF NOT EXISTS {indexName}
                FOR (v:{label})
                ON ({propertyList})";

            await ExecuteCypherAsync(graphName, cypher, null, cancellationToken);
        }

        /// <summary>
        /// 确定索引类型
        /// </summary>
        private string DetermineIndexType(string indexDefinition)
        {
            if (string.IsNullOrEmpty(indexDefinition))
                return "unknown";

            if (indexDefinition.Contains("FOR (") && indexDefinition.Contains(")-["))
                return "edge_label";

            if (indexDefinition.Contains("ON (v.id)") || indexDefinition.Contains("ON (r.id)"))
                return "label";

            if (indexDefinition.Contains("ON (v.") && indexDefinition.Split("ON (v.").Length > 1)
            {
                var props = indexDefinition.Split("ON (v.")[1].Split(")")[0];
                if (props.Contains(","))
                    return "composite";
                return "property";
            }

            return "unknown";
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 解析顶点属性
        /// </summary>
        private Dictionary<string, object> ParseVertexProperties(string? json)
        {
            if (string.IsNullOrEmpty(json) || json == "{}")
                return new Dictionary<string, object>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse vertex properties: {Json}", json);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 解析边属性
        /// </summary>
        private Dictionary<string, object> ParseEdgeProperties(string? json)
        {
            if (string.IsNullOrEmpty(json) || json == "{}")
                return new Dictionary<string, object>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse edge properties: {Json}", json);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 解析字符串数组
        /// </summary>
        private List<string> ParseStringArray(object? value)
        {
            if (value == null)
                return new List<string>();

            if (value is List<string> stringList)
                return stringList;

            if (value is string[] stringArray)
                return stringArray.ToList();

            if (value is string str)
            {
                // Try to parse as JSON array
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(str) ?? new List<string>();
                }
                catch
                {
                    return new List<string> { str };
                }
            }

            return new List<string>();
        }

        /// <summary>
        /// 解析顶点列表
        /// </summary>
        private List<Dictionary<string, object>> ParseVertexList(object? value)
        {
            if (value == null)
                return new List<Dictionary<string, object>>();

            if (value is List<Dictionary<string, object>> vertexList)
                return vertexList;

            if (value is string json)
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse vertex list: {Json}", json);
                    return new List<Dictionary<string, object>>();
                }
            }

            return new List<Dictionary<string, object>>();
        }

        /// <summary>
        /// 解析边列表
        /// </summary>
        private List<Dictionary<string, object>> ParseEdgeList(object? value)
        {
            if (value == null)
                return new List<Dictionary<string, object>>();

            if (value is List<Dictionary<string, object>> edgeList)
                return edgeList;

            if (value is string json)
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse edge list: {Json}", json);
                    return new List<Dictionary<string, object>>();
                }
            }

            return new List<Dictionary<string, object>>();
        }

        #endregion

        #endregion
    }
}
