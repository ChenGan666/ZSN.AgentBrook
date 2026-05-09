using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// LLM输出验证器
    /// 验证和修复LLM输出，确保数据质量和完整性
    /// </summary>
    public static class OutputValidator
    {
        /// <summary>
        /// 验证并修复实体提取响应
        /// </summary>
        public static (List<ZSN.AI.Entity.KnowledgeBase.Entity> entities, bool isValid, string errorMessage)
            ValidateAndRepairEntityResponse(
            string jsonResponse,
            float minConfidence,
            string originalText,
            ILogger? logger = null)
        {
            var entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();
            var isValid = true;
            var errorMessage = string.Empty;

            try
            {
                // 步骤1：提取JSON内容
                var jsonContent = ExtractJsonContent(jsonResponse);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    isValid = false;
                    errorMessage = "未能从响应中提取有效的JSON内容";
                    logger?.LogWarning("{Error}, 原始响应: {Response}",
                        errorMessage, jsonResponse.Length > 200 ? jsonResponse.Substring(0, 200) + "..." : jsonResponse);
                    return (entities, isValid, errorMessage);
                }

                // 步骤2：解析JSON
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (!root.TryGetProperty("entities", out var entitiesElement))
                {
                    isValid = false;
                    errorMessage = "JSON响应缺少'entities'字段";
                    logger?.LogWarning("{Error}, JSON: {Json}", errorMessage, jsonContent);
                    return (entities, isValid, errorMessage);
                }

                // 步骤3：验证和修复每个实体
                var entityIndex = 0;
                foreach (var entityElement in entitiesElement.EnumerateArray())
                {
                    var (entity, entityValid, entityError) =
                        ValidateAndRepairEntity(entityElement, entityIndex, originalText, minConfidence, logger);

                    if (entityValid)
                    {
                        entities.Add(entity);
                    }
                    else
                    {
                        logger?.LogWarning("实体 {Index} 验证失败: {Error}", entityIndex, entityError);
                        // 标记为部分有效，继续处理其他实体
                        isValid = false;
                        errorMessage = $"部分实体验证失败: {entityError}";
                    }

                    entityIndex++;
                }

                // 步骤4：验证实体列表
                if (entities.Count == 0)
                {
                    isValid = false;
                    errorMessage = "未能提取到任何有效实体";
                }
                else
                {
                    // 步骤5：数据一致性验证
                    var consistencyErrors = ValidateDataConsistency(entities, originalText);
                    if (consistencyErrors.Count > 0)
                    {
                        isValid = false;
                        errorMessage = string.Join("; ", consistencyErrors);
                        logger?.LogWarning("数据一致性问题: {Errors}", errorMessage);
                    }
                }

                logger?.LogInformation("成功验证 {Count}/{Total} 个实体", entities.Count, entityIndex);
            }
            catch (JsonException ex)
            {
                isValid = false;
                errorMessage = $"JSON解析失败: {ex.Message}";
                logger?.LogError(ex, "JSON解析错误，尝试修复");

                // 尝试修复JSON
                var (repairedEntities, repairSuccess) = AttemptJsonRepair(jsonResponse, minConfidence, originalText, logger);
                if (repairSuccess)
                {
                    entities = repairedEntities;
                    isValid = true;
                    errorMessage = "JSON已自动修复";
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                errorMessage = $"验证过程发生异常: {ex.Message}";
                logger?.LogError(ex, "验证实体响应失败");
            }

            return (entities, isValid, errorMessage);
        }

        /// <summary>
        /// 验证并修复单个实体
        /// </summary>
        private static (ZSN.AI.Entity.KnowledgeBase.Entity entity, bool isValid, string errorMessage)
            ValidateAndRepairEntity(
            JsonElement entityElement,
            int index,
            string originalText,
            float minConfidence,
            ILogger? logger)
        {
            var entity = new ZSN.AI.Entity.KnowledgeBase.Entity();
            var errors = new List<string>();

            // 验证text字段（必填）
            if (!entityElement.TryGetProperty("text", out var textElement) ||
                string.IsNullOrWhiteSpace(textElement.GetString()))
            {
                errors.Add("缺少或无效的'text'字段");
            }
            else
            {
                entity.Text = textElement.GetString()!.Trim();
            }

            // 验证type字段（必填）
            if (!entityElement.TryGetProperty("type", out var typeElement) ||
                string.IsNullOrWhiteSpace(typeElement.GetString()))
            {
                errors.Add("缺少或无效的'type'字段");
            }
            else
            {
                entity.Type = typeElement.GetString()!.Trim().ToUpperInvariant();
                // 验证类型是否合法
                if (!IsValidEntityType(entity.Type))
                {
                    errors.Add($"未知的实体类型: {entity.Type}");
                }
            }

            // 验证confidence字段（必填）
            if (!entityElement.TryGetProperty("confidence", out var confidenceElement))
            {
                errors.Add("缺少'confidence'字段");
                entity.Confidence = 0.5f; // 默认值
            }
            else
            {
                entity.Confidence = confidenceElement.GetSingle();
                // 验证置信度范围
                if (entity.Confidence < 0 || entity.Confidence > 1)
                {
                    logger?.LogWarning("实体 {Index} 的置信度 {Confidence} 超出范围[0,1]，已截断",
                        index, entity.Confidence);
                    entity.Confidence = Math.Clamp(entity.Confidence, 0f, 1f);
                }
            }

            // 验证position字段（可选）
            var startPos = 0;
            var endPos = 0;
            if (entityElement.TryGetProperty("start_position", out var startProp))
                startPos = startProp.GetInt32();
            if (entityElement.TryGetProperty("end_position", out var endProp))
                endPos = endProp.GetInt32();

            // 验证位置的合理性
            if (startPos < 0 || endPos < 0)
            {
                errors.Add($"位置坐标无效: start={startPos}, end={endPos}");
            }
            else if (startPos > endPos)
            {
                logger?.LogWarning("实体 {Index} 的起始位置 {Start} 大于结束位置 {End}，已交换",
                    index, startPos, endPos);
                (startPos, endPos) = (endPos, startPos);
            }
            else if (endPos > originalText.Length)
            {
                logger?.LogWarning("实体 {Index} 的结束位置 {End} 超出文本长度 {Length}，已调整",
                    index, endPos, originalText.Length);
                endPos = Math.Min(endPos, originalText.Length);
            }

            entity.StartPosition = startPos;
            entity.EndPosition = endPos;

            // 验证实体在文本中的实际内容
            if (startPos >= 0 && endPos <= originalText.Length && endPos > startPos)
            {
                var actualText = originalText.Substring(startPos, endPos - startPos);
                if (!actualText.Contains(entity.Text))
                {
                    logger?.LogWarning("实体 {Index} 声称的位置 [{Start},{End}] 与文本 '{Text}' 不匹配，实际文本: '{Actual}'",
                        index, startPos, endPos, entity.Text, actualText);
                    // 尝试在文本中查找实体
                    var foundPos = originalText.IndexOf(entity.Text);
                    if (foundPos >= 0)
                    {
                        entity.StartPosition = foundPos;
                        entity.EndPosition = foundPos + entity.Text.Length;
                        logger?.LogInformation("已将实体 {Index} 的位置修正为 [{Start},{End}]",
                            index, entity.StartPosition, entity.EndPosition);
                    }
                }
            }

            // 解析attributes字段（可选）
            if (entityElement.TryGetProperty("attributes", out var attrsElement))
            {
                foreach (var attr in attrsElement.EnumerateObject())
                {
                    var value = attr.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.Attributes[attr.Name] = value.Trim();
                    }
                }
            }

            // 过滤低置信度实体
            if (entity.Confidence < minConfidence)
            {
                return (entity, false, $"置信度 {entity.Confidence} 低于阈值 {minConfidence}");
            }

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(entity.Text) || string.IsNullOrWhiteSpace(entity.Type))
            {
                return (entity, false, string.Join("; ", errors));
            }

            return (entity, true, string.Empty);
        }

        /// <summary>
        /// 验证并修复关系抽取响应
        /// </summary>
        public static (List<Relation> relations, bool isValid, string errorMessage)
            ValidateAndRepairRelationResponse(
            string jsonResponse,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            ILogger? logger = null)
        {
            var relations = new List<Relation>();
            var isValid = true;
            var errorMessage = string.Empty;

            try
            {
                // 步骤1：提取JSON内容
                var jsonContent = ExtractJsonContent(jsonResponse);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    isValid = false;
                    errorMessage = "未能从响应中提取有效的JSON内容";
                    logger?.LogWarning("{Error}", errorMessage);
                    return (relations, isValid, errorMessage);
                }

                // 步骤2：解析JSON
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (!root.TryGetProperty("relations", out var relationsElement))
                {
                    isValid = false;
                    errorMessage = "JSON响应缺少'relations'字段";
                    return (relations, isValid, errorMessage);
                }

                // 步骤3：验证和修复每个关系
                var relationIndex = 0;
                foreach (var relationElement in relationsElement.EnumerateArray())
                {
                    var (relation, relationValid, relationError) =
                        ValidateAndRepairRelation(relationElement, relationIndex, entities, logger);

                    if (relationValid)
                    {
                        relations.Add(relation);
                    }
                    else
                    {
                        logger?.LogWarning("关系 {Index} 验证失败: {Error}", relationIndex, relationError);
                        isValid = false;
                        errorMessage = $"部分关系验证失败: {relationError}";
                    }

                    relationIndex++;
                }

                logger?.LogInformation("成功验证 {Count}/{Total} 个关系", relations.Count, relationIndex);
            }
            catch (JsonException ex)
            {
                isValid = false;
                errorMessage = $"JSON解析失败: {ex.Message}";
                logger?.LogError(ex, "验证关系响应失败");
            }
            catch (Exception ex)
            {
                isValid = false;
                errorMessage = $"验证过程发生异常: {ex.Message}";
                logger?.LogError(ex, "验证关系响应失败");
            }

            return (relations, isValid, errorMessage);
        }

        /// <summary>
        /// 验证并修复单个关系
        /// </summary>
        private static (Relation relation, bool isValid, string errorMessage)
            ValidateAndRepairRelation(
            JsonElement relationElement,
            int index,
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            ILogger? logger)
        {
            var relation = new Relation();
            var errors = new List<string>();

            // 验证head_entity字段（必填）
            if (!relationElement.TryGetProperty("head_entity", out var headElement) ||
                string.IsNullOrWhiteSpace(headElement.GetString()))
            {
                errors.Add("缺少'head_entity'字段");
            }
            else
            {
                var headText = headElement.GetString()!.Trim();
                var headEntity = entities.FirstOrDefault(e => e.Text == headText);
                if (headEntity == null)
                {
                    errors.Add($"找不到头实体: {headText}");
                }
                else
                {
                    relation.HeadEntityId = headEntity.Id;
                }
            }

            // 验证tail_entity字段（必填）
            if (!relationElement.TryGetProperty("tail_entity", out var tailElement) ||
                string.IsNullOrWhiteSpace(tailElement.GetString()))
            {
                errors.Add("缺少'tail_entity'字段");
            }
            else
            {
                var tailText = tailElement.GetString()!.Trim();
                var tailEntity = entities.FirstOrDefault(e => e.Text == tailText);
                if (tailEntity == null)
                {
                    errors.Add($"找不到尾实体: {tailText}");
                }
                else
                {
                    relation.TailEntityId = tailEntity.Id;
                }
            }

            // 验证relation_type字段（必填）
            if (!relationElement.TryGetProperty("relation_type", out var typeElement) ||
                string.IsNullOrWhiteSpace(typeElement.GetString()))
            {
                errors.Add("缺少'relation_type'字段");
            }
            else
            {
                relation.RelationType = typeElement.GetString()!.Trim().ToUpperInvariant();
            }

            // 验证confidence字段（必填）
            if (!relationElement.TryGetProperty("confidence", out var confidenceElement))
            {
                errors.Add("缺少'confidence'字段");
                relation.Confidence = 0.7f; // 默认值
            }
            else
            {
                relation.Confidence = confidenceElement.GetSingle();
                if (relation.Confidence < 0 || relation.Confidence > 1)
                {
                    relation.Confidence = Math.Clamp(relation.Confidence, 0f, 1f);
                }
            }

            // 解析description字段（可选）
            if (relationElement.TryGetProperty("description", out var descElement))
            {
                relation.Description = descElement.GetString()?.Trim();
            }

            // 检查是否有错误
            if (errors.Count > 0)
            {
                return (relation, false, string.Join("; ", errors));
            }

            return (relation, true, string.Empty);
        }

        /// <summary>
        /// 从响应中提取JSON内容（公开方法供外部使用）
        /// </summary>
        public static string ExtractJsonContent(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            // 去除可能的markdown代码块标记
            var jsonStart = response.IndexOf("{");
            var jsonEnd = response.LastIndexOf("}");

            if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
            {
                // 检查是否有 ```json 或 ``` 标记
                var codeBlockStart = response.IndexOf("```json");
                if (codeBlockStart >= 0 && codeBlockStart < jsonStart)
                {
                    var contentStart = response.IndexOf("\n", codeBlockStart) + 1;
                    var codeBlockEnd = response.LastIndexOf("```");
                    if (codeBlockEnd > jsonEnd)
                    {
                        return response.Substring(contentStart, codeBlockEnd - contentStart).Trim();
                    }
                }

                // 直接提取 {} 之间的内容
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            // 未找到 JSON 结构，返回空字符串而非原始文本
            return string.Empty;
        }

        /// <summary>
        /// 验证实体类型是否合法
        /// </summary>
        private static bool IsValidEntityType(string type)
        {
            var validTypes = new[]
            {
                "PERSON", "ORG", "LOC", "DATE", "EVENT",
                "CONCEPT", "TECHNOLOGY", "SKILL",
                "PRODUCT", "FEATURE", "SERVICE",
                "WORK", "PROJECT",
                "INDUSTRY", "DOMAIN",
                "LAW", "POLICY",
                "AWARD", "CERTIFICATE",
                "MONEY", "NUMBER", "METRIC",
                "DISEASE", "DRUG",
                "NATION", "LANGUAGE"
            };

            return validTypes.Contains(type.ToUpperInvariant());
        }

        /// <summary>
        /// 验证数据一致性
        /// </summary>
        private static List<string> ValidateDataConsistency(
            List<ZSN.AI.Entity.KnowledgeBase.Entity> entities,
            string originalText)
        {
            var errors = new List<string>();

            // 检查重复实体
            var duplicates = entities
                .GroupBy(e => e.Text)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                errors.Add($"发现重复实体: {string.Join(", ", duplicates)}");
            }

            // 检查实体位置重叠
            for (int i = 0; i < entities.Count; i++)
            {
                for (int j = i + 1; j < entities.Count; j++)
                {
                    var e1 = entities[i];
                    var e2 = entities[j];

                    // 检查位置是否重叠
                    if (e1.StartPosition < e2.EndPosition && e2.StartPosition < e1.EndPosition)
                    {
                        errors.Add($"实体位置重叠: '{e1.Text}'[{e1.StartPosition},{e1.EndPosition}] 与 '{e2.Text}'[{e2.StartPosition},{e2.EndPosition}]");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// 尝试修复损坏的JSON
        /// </summary>
        private static (List<ZSN.AI.Entity.KnowledgeBase.Entity> entities, bool success)
            AttemptJsonRepair(
            string jsonResponse,
            float minConfidence,
            string originalText,
            ILogger? logger)
        {
            var entities = new List<ZSN.AI.Entity.KnowledgeBase.Entity>();

            try
            {
                logger?.LogInformation("尝试修复损坏的JSON响应");

                // 策略1：尝试使用正则表达式提取JSON对象
                var jsonPattern = @"\{[^{}]*\}";
                var matches = System.Text.RegularExpressions.Regex.Matches(jsonResponse, jsonPattern);

                if (matches.Count > 0)
                {
                    // 尝试组合成完整的JSON
                    var repairedJson = $"{{\"entities\": [";
                    var entityCount = 0;

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        repairedJson += match.Value;
                        if (entityCount < matches.Count - 1)
                            repairedJson += ",";
                        entityCount++;
                    }

                    repairedJson += "]}";

                    logger?.LogDebug("尝试解析修复后的JSON: {Json}", repairedJson);

                    // 尝试解析
                    using var document = JsonDocument.Parse(repairedJson);
                    var root = document.RootElement;

                    if (root.TryGetProperty("entities", out var entitiesElement))
                    {
                        foreach (var entityElement in entitiesElement.EnumerateArray())
                        {
                            var (entity, isValid, _) = ValidateAndRepairEntity(
                                entityElement, 0, originalText, minConfidence, logger);

                            if (isValid)
                            {
                                entities.Add(entity);
                            }
                        }

                        if (entities.Count > 0)
                        {
                            logger?.LogInformation("JSON修复成功，提取到 {Count} 个实体", entities.Count);
                            return (entities, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "JSON修复失败");
            }

            return (entities, false);
        }
    }
}
