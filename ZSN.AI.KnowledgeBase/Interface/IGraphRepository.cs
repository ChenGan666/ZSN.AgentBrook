using System.ComponentModel.DataAnnotations;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.Core.Interface
{
    /// <summary>
    /// 图数据库仓储接口
    /// </summary>
    public interface IGraphRepository
    {
        /// <summary>
        /// 初始化图数据库
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建图
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task CreateGraphAsync(
            string graphName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除图
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task DropGraphAsync(
            string graphName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查图是否存在
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在</returns>
        Task<bool> GraphExistsAsync(
            string graphName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行Cypher查询
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="cypherQuery">Cypher查询语句</param>
        /// <param name="parameters">查询参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>查询结果列表</returns>
        Task<List<Dictionary<string, object>>> ExecuteCypherAsync(
            string graphName,
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建顶点
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="label">顶点标签</param>
        /// <param name="properties">顶点属性</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>顶点ID</returns>
        Task<string> CreateVertexAsync(
            string graphName,
            string label,
            Dictionary<string, object> properties,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量创建顶点
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="label">顶点标签</param>
        /// <param name="propertiesList">顶点属性列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>顶点ID列表</returns>
        Task<List<string>> CreateVerticesAsync(
            string graphName,
            string label,
            List<Dictionary<string, object>> propertiesList,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="startVertexId">起始顶点ID</param>
        /// <param name="endVertexId">结束顶点ID</param>
        /// <param name="edgeLabel">边标签</param>
        /// <param name="properties">边属性</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>边ID</returns>
        Task<string> CreateEdgeAsync(
            string graphName,
            string startVertexId,
            string endVertexId,
            string edgeLabel,
            Dictionary<string, object>? properties = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量创建边
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="edges">边定义列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>边ID列表</returns>
        Task<List<string>> CreateEdgesAsync(
            string graphName,
            List<EdgeDefinition> edges,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询顶点
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="vertexId">顶点ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>顶点属性字典</returns>
        Task<Dictionary<string, object>?> GetVertexAsync(
            string graphName,
            string vertexId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询边
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="vertexId">顶点ID</param>
        /// <param name="edgeLabel">边标签（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>边列表</returns>
        Task<List<Dictionary<string, object>>> GetEdgesAsync(
            string graphName,
            string vertexId,
            string? edgeLabel = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新顶点属性
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="vertexId">顶点ID</param>
        /// <param name="properties">要更新的属性</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task UpdateVertexAsync(
            string graphName,
            string vertexId,
            Dictionary<string, object> properties,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除顶点
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="vertexId">顶点ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task DeleteVertexAsync(
            string graphName,
            string vertexId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除边
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="edgeId">边ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task DeleteEdgeAsync(
            string graphName,
            string edgeId,
            CancellationToken cancellationToken = default);

        // ========== 高级查询方法 ==========

        /// <summary>
        /// 多跳关系查询 - 查找从指定顶点出发经过指定跳数能到达的所有顶点
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="startVertexId">起始顶点ID</param>
        /// <param name="maxHops">最大跳数（默认3跳）</param>
        /// <param name="edgeLabels">要遍历的边标签（null表示所有边）</param>
        /// <param name="direction">方向：outgoing, incoming, both（默认outgoing）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>可达顶点列表及其路径信息</returns>
        Task<List<GraphPathResult>> FindReachableVerticesAsync(
            string graphName,
            string startVertexId,
            int maxHops = 3,
            string[]? edgeLabels = null,
            string direction = "outgoing",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询两个顶点之间的最短路径
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="startVertexId">起始顶点ID</param>
        /// <param name="endVertexId">结束顶点ID</param>
        /// <param name="edgeLabels">要遍历的边标签（null表示所有边）</param>
        /// <param name="maxDepth">最大搜索深度（默认10）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最短路径，如果不存在则返回null</returns>
        Task<GraphPathResult?> FindShortestPathAsync(
            string graphName,
            string startVertexId,
            string endVertexId,
            string[]? edgeLabels = null,
            int maxDepth = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询两个顶点之间的所有路径（不超过指定长度）
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="startVertexId">起始顶点ID</param>
        /// <param name="endVertexId">结束顶点ID</param>
        /// <param name="maxPathLength">最大路径长度（默认5）</param>
        /// <param name="edgeLabels">要遍历的边标签（null表示所有边）</param>
        /// <param name="maxPaths">最大返回路径数（默认100）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有路径列表</returns>
        Task<List<GraphPathResult>> FindAllPathsAsync(
            string graphName,
            string startVertexId,
            string endVertexId,
            int maxPathLength = 5,
            string[]? edgeLabels = null,
            int maxPaths = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 提取子图 - 提取指定顶点及其邻居组成的子图
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="centerVertexId">中心顶点ID</param>
        /// <param name="hops">跳数（1表示直接邻居，2表示邻居的邻居，以此类推）</param>
        /// <param name="edgeLabels">要包含的边标签（null表示所有边）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>子图信息（包含顶点和边）</returns>
        Task<SubGraphResult> ExtractSubGraphAsync(
            string graphName,
            string centerVertexId,
            int hops = 1,
            string[]? edgeLabels = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询顶点的邻居
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="vertexId">顶点ID</param>
        /// <param name="edgeLabels">要查询的边标签（null表示所有边）</param>
        /// <param name="direction">方向：outgoing, incoming, both（默认outgoing）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>邻居顶点及其边信息</returns>
        Task<List<NeighborResult>> GetNeighborsAsync(
            string graphName,
            string vertexId,
            string[]? edgeLabels = null,
            string direction = "outgoing",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行复杂的图遍历查询（自定义Cypher查询）
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="cypherQuery">Cypher查询语句</param>
        /// <param name="parameters">查询参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>查询结果</returns>
        Task<List<Dictionary<string, object>>> ExecuteGraphTraversalAsync(
            string graphName,
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);

        // ========== 索引管理方法 ==========

        /// <summary>
        /// 创建顶点标签索引 - 加速按标签查询顶点
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="label">顶点标签</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task CreateLabelIndexAsync(
            string graphName,
            string label,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建顶点属性索引 - 加速按属性查询
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="label">顶点标签</param>
        /// <param name="property">属性名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task CreatePropertyIndexAsync(
            string graphName,
            string label,
            string property,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建边标签索引 - 加速按标签查询边
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="edgeLabel">边标签</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task CreateEdgeLabelIndexAsync(
            string graphName,
            string edgeLabel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 列出图的所有索引
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>索引信息列表</returns>
        Task<List<GraphIndexInfo>> ListIndexesAsync(
            string graphName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="indexName">索引名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task DropIndexAsync(
            string graphName,
            string indexName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建复合索引（多属性索引）- 加速多属性组合查询
        /// </summary>
        /// <param name="graphName">图名称</param>
        /// <param name="label">顶点标签</param>
        /// <param name="properties">属性名列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        Task CreateCompositeIndexAsync(
            string graphName,
            string label,
            string[] properties,
            CancellationToken cancellationToken = default);
    }

    
}
