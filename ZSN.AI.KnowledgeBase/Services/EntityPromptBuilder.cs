using System.Text;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 实体提取Prompt构建器
    /// 提供优化的Prompt模板，包含Few-Shot示例和详细指导
    /// </summary>
    public static class EntityPromptBuilder
    {
        /// <summary>
        /// 构建优化的实体提取Prompt
        /// </summary>
        public static string BuildEntityExtractionPrompt(string text, EntityExtractionConfig config)
        {
            var sb = new StringBuilder();

            // 角色定义
            sb.AppendLine("# 角色");
            sb.AppendLine("你是一个专业的实体识别专家，擅长从中文文本中准确识别和分类实体。");
            sb.AppendLine();

            // 任务说明
            sb.AppendLine("# 任务");
            sb.AppendLine("从给定的文本中识别所有重要实体，并按照指定的类型进行分类。");
            sb.AppendLine();

            // 实体类型详细定义
            sb.AppendLine("# 实体类型定义");
            BuildEntityTypeDefinitions(sb, config);
            sb.AppendLine();

            // Few-Shot示例
            sb.AppendLine("# 示例");
            BuildFewShotExamples(sb);
            sb.AppendLine();

            // 注意事项
            sb.AppendLine("# 注意事项");
            sb.AppendLine("1. 实体边界要准确，不要包含多余的标点符号");
            sb.AppendLine("2. 同一文本中多次出现的实体，每次出现都要提取");
            sb.AppendLine("3. 实体类型要准确，不确定时选择CONCEPT类型");
            sb.AppendLine("4. 置信度要真实反映提取的确定性（0.6-1.0）");
            sb.AppendLine("5. 对于嵌套实体，优先提取更具体的实体");
            sb.AppendLine("6. 不要提取过于宽泛或无意义的概念（如'情况''问题'等）");
            sb.AppendLine("7. 数字实体要提取具体数值，包括单位（如'500万元'）");
            sb.AppendLine("8. 产品功能要与产品本身分开提取");
            sb.AppendLine("9. 行业领域要与组织机构区分开");
            sb.AppendLine();

            if (config.EntityTypes.Count > 0)
            {
                sb.AppendLine($"# 限制");
                sb.AppendLine($"仅识别以下类型的实体: {string.Join(", ", config.EntityTypes)}");
                sb.AppendLine();
            }

            // 待处理文本
            sb.AppendLine("# 待处理文本");
            sb.AppendLine(text);
            sb.AppendLine();

            // 输出格式
            sb.AppendLine("# 输出格式");
            sb.AppendLine("请严格按照以下JSON格式输出，不要添加任何其他说明文字：");
            sb.AppendLine("{");
            sb.AppendLine("  \"entities\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"text\": \"实体文本\",");
            sb.AppendLine("      \"type\": \"PERSON\",");
            sb.AppendLine("      \"attributes\": {");
            sb.AppendLine("        \"别名\": \"其他称呼（如有）\",");
            sb.AppendLine("        \"职位\": \"职位信息（对于人物）\"");
            sb.AppendLine("      },");
            sb.AppendLine("      \"confidence\": 0.95,");
            sb.AppendLine("      \"start_position\": 0,");
            sb.AppendLine("      \"end_position\": 10");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 构建实体类型定义
        /// </summary>
        private static void BuildEntityTypeDefinitions(StringBuilder sb, EntityExtractionConfig config)
        {
            var entityTypes = new Dictionary<string, string>
            {
                // 基础类型
                { "PERSON", "人物：包括真实人物、虚构人物、职业人士、历史人物等，如'张三''李老师''马云''鲁迅'" },
                { "ORG", "组织机构：公司、企业、政府机构、学校、非营利组织、社团等，如'阿里巴巴''教育部''清华大学''红十字会'" },
                { "LOC", "地点：国家、省份、城市、地区、地标建筑、自然地理等，如'北京''长城''长江''天安门广场'" },
                { "DATE", "时间：具体日期、时间段、历史朝代、节日、季度等，如'2023年10月''明朝''春节''Q4'" },
                { "EVENT", "事件：历史事件、会议、活动、事故、战争等，如'奥运会''G20峰会''第二次世界大战''911事件'" },

                // 概念与技术
                { "CONCEPT", "概念：专业术语、理论思想、学科领域、原理方法等，如'人工智能''区块链''量子计算''辩证法'" },
                { "TECHNOLOGY", "技术：具体的技术方案、技术框架、工艺方法等，如'深度学习''5G技术''云计算''CRISPR'" },
                { "SKILL", "技能：能力、技巧、专业素养等，如'编程''写作''沟通能力''项目管理'" },

                // 产品与服务
                { "PRODUCT", "产品：具体产品、设备、工具、软件应用等，如'iPhone''ChatGPT''Windows系统''特斯拉汽车'" },
                { "FEATURE", "产品功能：产品的功能特性、服务能力、模块组件等，如'人脸识别''自动翻译''云存储''语音助手'" },
                { "SERVICE", "服务：商业服务、公共服务、在线服务等，如'外卖服务''云服务''咨询服务''售后服务'" },

                // 内容作品
                { "WORK", "作品：书籍、论文、电影、音乐、艺术作品等，如'红楼梦''Nature论文''阿凡达''命运交响曲'" },
                { "PROJECT", "项目：工程项目、计划、倡议、研究项目等，如'一带一路''载人航天工程''人类基因组计划'" },

                // 行业与领域
                { "INDUSTRY", "行业：产业领域、行业分类等，如'互联网行业''金融业''制造业''教育行业'" },
                { "DOMAIN", "领域：专业领域、应用场景、业务领域等，如'医疗健康''自动驾驶''电商''智能制造'" },

                // 法律与政策
                { "LAW", "法律法规：法律、法规、规章、条例等，如'宪法''民法典''个人所得税法'" },
                { "POLICY", "政策：政策文件、政策制度、方针政策等，如'双减政策''碳达峰碳中和''产业政策'" },

                // 荣誉与成就
                { "AWARD", "奖项：荣誉称号、奖励、认证等，如'诺贝尔奖''奥斯卡奖''高新技术企业认定''五一劳动奖章'" },
                { "CERTIFICATE", "证书：职业资格、认证证书、许可证等，如'CPA证书''驾驶证''ISO认证''教师资格证'" },

                // 财务与数量
                { "MONEY", "金额：货币金额、资金规模等，如'500万元''10亿美元''预算1000万'" },
                { "NUMBER", "数字：统计数据、数量指标、百分比、倍数等，如'80%''3.5倍''5000用户'" },
                { "METRIC", "指标：性能指标、评估指标、KPI等，如'准确率95%''日活用户''转化率''ROI'" },

                // 医疗健康（可选）
                { "DISEASE", "疾病：病症、疾病名称、健康问题等，如'糖尿病''高血压''新冠肺炎'" },
                { "DRUG", "药物：药品、疫苗、治疗方案等，如'阿司匹林''新冠疫苗''靶向治疗'" },

                // 其他
                { "NATION", "国家/民族：主权国家、民族群体等，如'中国''美国''中华民族'" },
                { "LANGUAGE", "语言：编程语言、自然语言等，如'Python''英语''汉语''Java'" }
            };

            if (config.EntityTypes.Count > 0)
            {
                // 只输出指定的实体类型
                foreach (var entityType in config.EntityTypes)
                {
                    if (entityTypes.TryGetValue(entityType, out var description))
                    {
                        sb.AppendLine($"- {entityType}: {description}");
                    }
                }
            }
            else
            {
                // 输出所有实体类型（分组显示）
                var groups = new Dictionary<string, List<string>>
                {
                    { "基础实体类型", new[] { "PERSON", "ORG", "LOC", "DATE", "EVENT" }.ToList() },
                    { "概念与技术", new[] { "CONCEPT", "TECHNOLOGY", "SKILL" }.ToList() },
                    { "产品与服务", new[] { "PRODUCT", "FEATURE", "SERVICE" }.ToList() },
                    { "内容与项目", new[] { "WORK", "PROJECT" }.ToList() },
                    { "行业与领域", new[] { "INDUSTRY", "DOMAIN" }.ToList() },
                    { "法律与政策", new[] { "LAW", "POLICY" }.ToList() },
                    { "荣誉与认证", new[] { "AWARD", "CERTIFICATE" }.ToList() },
                    { "财务与数量", new[] { "MONEY", "NUMBER", "METRIC" }.ToList() },
                    { "医疗（可选）", new[] { "DISEASE", "DRUG" }.ToList() },
                    { "其他", new[] { "NATION", "LANGUAGE" }.ToList() }
                };

                foreach (var (groupName, types) in groups)
                {
                    sb.AppendLine($"## {groupName}");
                    foreach (var type in types)
                    {
                        if (entityTypes.TryGetValue(type, out var description))
                        {
                            sb.AppendLine($"- {type}: {description}");
                        }
                    }
                    sb.AppendLine();
                }
            }
        }

        /// <summary>
        /// 构建Few-Shot示例
        /// </summary>
        private static void BuildFewShotExamples(StringBuilder sb)
        {
            // 示例1：科技公司新闻（展示人物、组织、地点、日期）
            sb.AppendLine("## 示例1：企业新闻");
            sb.AppendLine("文本：");
            sb.AppendLine("2023年10月，阿里巴巴集团宣布张勇将卸任董事长兼CEO，由吴泳铭接任。这一决定是在杭州总部做出的。");
            sb.AppendLine();
            sb.AppendLine("输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"entities\": [");
            sb.AppendLine("    {\"text\": \"2023年10月\", \"type\": \"DATE\", \"attributes\": {}, \"confidence\": 0.98, \"start_position\": 0, \"end_position\": 9},");
            sb.AppendLine("    {\"text\": \"阿里巴巴集团\", \"type\": \"ORG\", \"attributes\": {\"别名\": \"阿里巴巴\"}, \"confidence\": 0.99, \"start_position\": 10, \"end_position\": 17},");
            sb.AppendLine("    {\"text\": \"张勇\", \"type\": \"PERSON\", \"attributes\": {\"职位\": \"董事长兼CEO\"}, \"confidence\": 0.95, \"start_position\": 24, \"end_position\": 26},");
            sb.AppendLine("    {\"text\": \"吴泳铭\", \"type\": \"PERSON\", \"attributes\": {}, \"confidence\": 0.95, \"start_position\": 33, \"end_position\": 36},");
            sb.AppendLine("    {\"text\": \"杭州\", \"type\": \"LOC\", \"attributes\": {}, \"confidence\": 0.90, \"start_position\": 47, \"end_position\": 49}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();

            // 示例2：技术文章（展示概念、产品、功能、技术）
            sb.AppendLine("## 示例2：技术文章");
            sb.AppendLine("文本：");
            sb.AppendLine("ChatGPT是OpenAI开发的大型语言模型，基于GPT-4架构。它具备对话理解、代码生成、文本创作等功能，在自然语言处理领域取得了突破性进展。");
            sb.AppendLine();
            sb.AppendLine("输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"entities\": [");
            sb.AppendLine("    {\"text\": \"ChatGPT\", \"type\": \"PRODUCT\", \"attributes\": {\"开发者\": \"OpenAI\"}, \"confidence\": 0.99, \"start_position\": 0, \"end_position\": 7},");
            sb.AppendLine("    {\"text\": \"OpenAI\", \"type\": \"ORG\", \"attributes\": {}, \"confidence\": 0.98, \"start_position\": 11, \"end_position\": 16},");
            sb.AppendLine("    {\"text\": \"大型语言模型\", \"type\": \"CONCEPT\", \"attributes\": {\"别名\": \"LLM\"}, \"confidence\": 0.95, \"start_position\": 19, \"end_position\": 25},");
            sb.AppendLine("    {\"text\": \"GPT-4\", \"type\": \"TECHNOLOGY\", \"attributes\": {}, \"confidence\": 0.95, \"start_position\": 33, \"end_position\": 38},");
            sb.AppendLine("    {\"text\": \"对话理解\", \"type\": \"FEATURE\", \"attributes\": {}, \"confidence\": 0.90, \"start_position\": 43, \"end_position\": 47},");
            sb.AppendLine("    {\"text\": \"代码生成\", \"type\": \"FEATURE\", \"attributes\": {}, \"confidence\": 0.90, \"start_position\": 48, \"end_position\": 52},");
            sb.AppendLine("    {\"text\": \"文本创作\", \"type\": \"FEATURE\", \"attributes\": {}, \"confidence\": 0.90, \"start_position\": 53, \"end_position\": 57},");
            sb.AppendLine("    {\"text\": \"自然语言处理\", \"type\": \"DOMAIN\", \"attributes\": {\"别名\": \"NLP\"}, \"confidence\": 0.96, \"start_position\": 66, \"end_position\": 72}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();

            // 示例3：政策文件（展示政策、法规、行业、指标）
            sb.AppendLine("## 示例3：政策文件");
            sb.AppendLine("文本：");
            sb.AppendLine("2023年7月，教育部发布《关于推进高等教育数字化转型的意见》，提出到2025年，全国高校数字化教学覆盖率要达到90%以上，建设100个国家级虚拟仿真实验教学中心。");
            sb.AppendLine();
            sb.AppendLine("输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"entities\": [");
            sb.AppendLine("    {\"text\": \"2023年7月\", \"type\": \"DATE\", \"attributes\": {}, \"confidence\": 0.98, \"start_position\": 0, \"end_position\": 7},");
            sb.AppendLine("    {\"text\": \"教育部\", \"type\": \"ORG\", \"attributes\": {}, \"confidence\": 0.99, \"start_position\": 8, \"end_position\": 11},");
            sb.AppendLine("    {\"text\": \"《关于推进高等教育数字化转型的意见》\", \"type\": \"POLICY\", \"attributes\": {\"发文部门\": \"教育部\"}, \"confidence\": 0.97, \"start_position\": 12, \"end_position\": 37},");
            sb.AppendLine("    {\"text\": \"2025年\", \"type\": \"DATE\", \"attributes\": {}, \"confidence\": 0.95, \"start_position\": 50, \"end_position\": 56},");
            sb.AppendLine("    {\"text\": \"高等教育\", \"type\": \"INDUSTRY\", \"attributes\": {}, \"confidence\": 0.90, \"start_position\": 58, \"end_position\": 62},");
            sb.AppendLine("    {\"text\": \"数字化转型\", \"type\": \"CONCEPT\", \"attributes\": {}, \"confidence\": 0.90, \"start_position\": 62, \"end_position\": 68},");
            sb.AppendLine("    {\"text\": \"90%\", \"type\": \"METRIC\", \"attributes\": {\"指标名称\": \"覆盖率\"}, \"confidence\": 0.95, \"start_position\": 83, \"end_position\": 86},");
            sb.AppendLine("    {\"text\": \"100个\", \"type\": \"NUMBER\", \"attributes\": {}, \"confidence\": 0.95, \"start_position\": 93, \"end_position\": 97},");
            sb.AppendLine("    {\"text\": \"国家级虚拟仿真实验教学中心\", \"type\": \"PROJECT\", \"attributes\": {}, \"confidence\": 0.90, \"start_position\": 98, \"end_position\": 111}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
        }

        /// <summary>
        /// 构建优化的关系抽取Prompt
        /// </summary>
        public static string BuildRelationExtractionPrompt(string text, List<ZSN.AI.Entity.KnowledgeBase.Entity> entities)
        {
            var sb = new StringBuilder();

            // 角色定义
            sb.AppendLine("# 角色");
            sb.AppendLine("你是一个专业的知识图谱构建专家，擅长分析文本中实体之间的语义关系。");
            sb.AppendLine();

            // 任务说明
            sb.AppendLine("# 任务");
            sb.AppendLine("分析给定文本中实体之间的关系，只提取文本中明确表达或可合理推断的关系。");
            sb.AppendLine();

            // 已知实体
            sb.AppendLine("# 已识别的实体");
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var attrStr = entity.Attributes.Count > 0
                    ? $" ({string.Join(", ", entity.Attributes.Keys)})"
                    : "";
                sb.AppendLine($"{i + 1}. [{entity.Type}] {entity.Text}{attrStr} (置信度: {entity.Confidence:F2})");
            }
            sb.AppendLine();

            // 关系类型详细定义
            sb.AppendLine("# 关系类型定义");
            BuildRelationTypeDefinitions(sb);
            sb.AppendLine();

            // Few-Shot示例
            sb.AppendLine("# 示例");
            BuildRelationFewShotExamples(sb);
            sb.AppendLine();

            // 注意事项
            sb.AppendLine("# 注意事项");
            sb.AppendLine("1. 关系必须基于文本内容，不能编造");
            sb.AppendLine("2. 只提取有明确语义关系的三元组，避免模糊的关系");
            sb.AppendLine("3. 有向关系：注意头实体和尾实体的顺序");
            sb.AppendLine("4. 置信度评分标准：");
            sb.AppendLine("   - 0.9-1.0: 文本明确表达的关系");
            sb.AppendLine("   - 0.7-0.9: 可以合理推断的关系");
            sb.AppendLine("   - 0.5-0.7: 可能存在但不确定的关系");
            sb.AppendLine("5. 避免提取过于泛化的关系（如'相关''涉及'）");
            sb.AppendLine("6. 同一对实体之间可能存在多种关系");
            sb.AppendLine("7. 产品与功能之间要提取PROVIDES关系");
            sb.AppendLine("8. 技术与领域之间要提取APPLIES_TO关系");
            sb.AppendLine();

            // 待处理文本
            sb.AppendLine("# 待处理文本");
            sb.AppendLine(text);
            sb.AppendLine();

            // 输出格式
            sb.AppendLine("# 输出格式");
            sb.AppendLine("请严格按照以下JSON格式输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"relations\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"head_entity\": \"实体1文本\",");
            sb.AppendLine("      \"tail_entity\": \"实体2文本\",");
            sb.AppendLine("      \"relation_type\": \"关系类型\",");
            sb.AppendLine("      \"description\": \"关系描述（说明关系的具体内容和依据）\",");
            sb.AppendLine("      \"confidence\": 0.90");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 构建关系类型定义
        /// </summary>
        private static void BuildRelationTypeDefinitions(StringBuilder sb)
        {
            var relationCategories = new[]
            {
                ("人物关系", new[] {
                    ("COLLEAGUE", "同事", "同一组织或机构的同事关系"),
                    ("SUPERIOR", "上级", "直接的上下级关系"),
                    ("SUBORDINATE", "下级", "直接的下属关系"),
                    ("RELATIVE", "亲属", "家庭成员、亲戚关系"),
                    ("FRIEND", "朋友", "朋友、好友关系"),
                    ("SPOUSE", "配偶", "夫妻、伴侣关系"),
                    ("MENTOR", "导师", "师生、师徒关系"),
                    ("PARTNER", "搭档", "合作伙伴、搭档关系")
                }),
                ("组织关系", new[] {
                    ("BELONGS_TO", "属于/任职于", "个人属于或任职于某组织"),
                    ("PARTNERS_WITH", "合作", "组织间的合作关系"),
                    ("COMPETES_WITH", "竞争", "组织间的竞争关系"),
                    ("ACQUIRED", "收购", "收购、并购关系"),
                    ("INVESTS_IN", "投资", "投资关系"),
                    ("SUBSIDIARY_OF", "是...的子公司", "子公司与母公司关系"),
                    ("JOINT_VENTURE", "合资", "合资关系"),
                    ("SUPPLIES_TO", "供应", "供应链关系")
                }),
                ("地理关系", new[] {
                    ("LOCATED_IN", "位于", "地理位置包含关系"),
                    ("CONTAINS", "包含", "地理范围包含"),
                    ("NEIGHBOR_OF", "邻接", "地理上相邻"),
                    ("CAPITAL_OF", "是...的首都", "首都与国家关系"),
                    ("ADMINISTERS", "管辖", "行政管辖关系")
                }),
                ("时间关系", new[] {
                    ("OCCURRED_AT", "发生时间", "事件发生的时间"),
                    ("LASTED", "持续", "时间段持续关系"),
                    ("PRECEDED", "先于", "时间先后关系"),
                    ("SIMULTANEOUS_WITH", "同时", "同时发生"),
                    ("FREQUENCY", "频率", "发生频率关系")
                }),
                ("产品与技术", new[] {
                    ("PRODUCES", "生产", "生产制造产品"),
                    ("PROVIDES", "提供", "提供服务或功能"),
                    ("BASED_ON", "基于", "技术或产品基于某技术"),
                    ("IMPROVES", "改进", "改进升级关系"),
                    ("DEVELOPED_BY", "由...开发", "产品开发关系"),
                    ("USES", "使用", "使用某技术或工具"),
                    ("DEPENDS_ON", "依赖", "技术依赖关系"),
                    ("INTEGRATES", "集成", "功能集成关系"),
                    ("COMPATIBLE_WITH", "兼容", "兼容关系"),
                    ("REPLACES", "替代", "产品替代关系")
                }),
                ("概念关系", new[] {
                    ("IS_A", "是...的一种", "概念层级关系（is-a关系）"),
                    ("INSTANCE_OF", "是...的实例", "实例与类关系"),
                    ("RELATED_TO", "相关", "相关概念"),
                    ("PART_OF", "是...的一部分", "组成关系"),
                    ("APPLIES_TO", "应用于", "技术应用于领域"),
                    ("EVOLVES_FROM", "演进自", "技术演进关系"),
                    ("SIMILAR_TO", "相似于", "相似概念"),
                    ("OPPOSED_TO", "对立于", "对立概念")
                }),
                ("因果关系", new[] {
                    ("CAUSED", "导致", "因果关系"),
                    ("RESULTED_FROM", "结果", "因果反向关系"),
                    ("CONDITION_FOR", "条件", "条件关系"),
                    ("PREVENTS", "防止", "防止关系"),
                    ("ENABLES", "使能/促进", "促进、使能关系")
                }),
                ("内容关系", new[] {
                    ("AUTHOR_OF", "作者", "作者与作品关系"),
                    ("PUBLISHED_BY", "发布于", "发布关系"),
                    ("CITES", "引用", "引用关系"),
                    ("DERIVED_FROM", "改编自", "改编、衍生关系"),
                    ("SEQUEL_TO", "续作", "续作关系"),
                    ("TRANSLATION_OF", "翻译版", "翻译关系")
                }),
                ("评价关系", new[] {
                    ("PRAISES", "赞扬", "正面评价"),
                    ("CRITICIZES", "批评", "负面评价"),
                    ("EVALUATES", "评价", "评价关系"),
                    ("RANKS_HIGHER_THAN", "优于", "比较排名"),
                    ("RANKS_LOWER_THAN", "劣于", "比较排名")
                }),
                ("数量与统计", new[] {
                    ("HAS_METRIC", "指标为", "具有某指标"),
                    ("INCREASED_BY", "增长了", "增长关系"),
                    ("DECREASED_BY", "下降了", "下降关系"),
                    ("ACCOUNTS_FOR", "占比", "占比关系"),
                    ("EXCEEDS", "超过", "超过某数值")
                }),
                ("法律与政策", new[] {
                    ("REGULATES", "监管", "监管关系"),
                    ("COMPLIES_WITH", "遵守", "遵守法规"),
                    ("VIOLATES", "违反", "违反法规"),
                    ("ISSUED_BY", "由...发布", "法规发布关系"),
                    ("AMENDS", "修订", "法规修订关系")
                }),
                ("行业与领域", new[] {
                    ("BELONGS_TO_INDUSTRY", "属于...行业", "行业归属"),
                    ("SERVES_DOMAIN", "服务于...领域", "领域服务关系"),
                    ("OPERATES_IN", "运营于", "运营领域"),
                    ("EXPANDS_TO", "拓展到", "业务拓展关系")
                })
            };

            foreach (var (category, relations) in relationCategories)
            {
                sb.AppendLine($"## {category}");
                foreach (var (code, name, description) in relations)
                {
                    sb.AppendLine($"- {code}: {name} - {description}");
                }
                sb.AppendLine();
            }
        }

        /// <summary>
        /// 构建关系抽取Few-Shot示例
        /// </summary>
        private static void BuildRelationFewShotExamples(StringBuilder sb)
        {
            // 示例1：企业人事关系
            sb.AppendLine("## 示例1：企业人事变动");
            sb.AppendLine("实体：");
            sb.AppendLine("1. [ORG] 阿里巴巴集团");
            sb.AppendLine("2. [PERSON] 张勇");
            sb.AppendLine("3. [PERSON] 吴泳铭");
            sb.AppendLine("4. [LOC] 杭州");
            sb.AppendLine();
            sb.AppendLine("文本：");
            sb.AppendLine("2023年10月，阿里巴巴集团宣布张勇将卸任董事长兼CEO，由吴泳铭接任。这一决定是在杭州总部做出的。");
            sb.AppendLine();
            sb.AppendLine("输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"relations\": [");
            sb.AppendLine("    {\"head_entity\": \"张勇\", \"tail_entity\": \"阿里巴巴集团\", \"relation_type\": \"SUPERIOR\", \"description\": \"张勇曾任阿里巴巴集团董事长兼CEO\", \"confidence\": 0.95},");
            sb.AppendLine("    {\"head_entity\": \"吴泳铭\", \"tail_entity\": \"阿里巴巴集团\", \"relation_type\": \"SUPERIOR\", \"description\": \"吴泳铭接任阿里巴巴集团董事长兼CEO\", \"confidence\": 0.95},");
            sb.AppendLine("    {\"head_entity\": \"阿里巴巴集团\", \"tail_entity\": \"杭州\", \"relation_type\": \"LOCATED_IN\", \"description\": \"阿里巴巴集团总部位于杭州\", \"confidence\": 0.90}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();

            // 示例2：产品技术关系
            sb.AppendLine("## 示例2：产品功能关系");
            sb.AppendLine("实体：");
            sb.AppendLine("1. [PRODUCT] ChatGPT");
            sb.AppendLine("2. [ORG] OpenAI");
            sb.AppendLine("3. [TECHNOLOGY] GPT-4");
            sb.AppendLine("4. [FEATURE] 对话理解");
            sb.AppendLine("5. [FEATURE] 代码生成");
            sb.AppendLine("6. [DOMAIN] 自然语言处理");
            sb.AppendLine();
            sb.AppendLine("文本：");
            sb.AppendLine("ChatGPT是OpenAI开发的大型语言模型，基于GPT-4架构。它具备对话理解、代码生成、文本创作等功能，在自然语言处理领域取得了突破性进展。");
            sb.AppendLine();
            sb.AppendLine("输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"relations\": [");
            sb.AppendLine("    {\"head_entity\": \"ChatGPT\", \"tail_entity\": \"OpenAI\", \"relation_type\": \"DEVELOPED_BY\", \"description\": \"ChatGPT由OpenAI开发\", \"confidence\": 0.95},");
            sb.AppendLine("    {\"head_entity\": \"ChatGPT\", \"tail_entity\": \"GPT-4\", \"relation_type\": \"BASED_ON\", \"description\": \"ChatGPT基于GPT-4架构\", \"confidence\": 0.95},");
            sb.AppendLine("    {\"head_entity\": \"ChatGPT\", \"tail_entity\": \"对话理解\", \"relation_type\": \"PROVIDES\", \"description\": \"ChatGPT提供对话理解功能\", \"confidence\": 0.90},");
            sb.AppendLine("    {\"head_entity\": \"ChatGPT\", \"tail_entity\": \"代码生成\", \"relation_type\": \"PROVIDES\", \"description\": \"ChatGPT提供代码生成功能\", \"confidence\": 0.90},");
            sb.AppendLine("    {\"head_entity\": \"ChatGPT\", \"tail_entity\": \"自然语言处理\", \"relation_type\": \"APPLIES_TO\", \"description\": \"ChatGPT应用于自然语言处理领域\", \"confidence\": 0.85}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();

            // 示例3：技术概念关系
            sb.AppendLine("## 示例3：技术概念关系");
            sb.AppendLine("实体：");
            sb.AppendLine("1. [CONCEPT] 深度学习");
            sb.AppendLine("2. [CONCEPT] 机器学习");
            sb.AppendLine("3. [TECHNOLOGY] 神经网络");
            sb.AppendLine("4. [CONCEPT] 人工智能");
            sb.AppendLine();
            sb.AppendLine("文本：");
            sb.AppendLine("深度学习是机器学习的一个分支，它使用多层神经网络来学习数据的表示。作为人工智能的重要发展方向，深度学习在图像识别、自然语言处理等领域取得了显著成果。");
            sb.AppendLine();
            sb.AppendLine("输出：");
            sb.AppendLine("{");
            sb.AppendLine("  \"relations\": [");
            sb.AppendLine("    {\"head_entity\": \"深度学习\", \"tail_entity\": \"机器学习\", \"relation_type\": \"IS_A\", \"description\": \"深度学习是机器学习的一个分支\", \"confidence\": 0.98},");
            sb.AppendLine("    {\"head_entity\": \"深度学习\", \"tail_entity\": \"神经网络\", \"relation_type\": \"USES\", \"description\": \"深度学习使用多层神经网络\", \"confidence\": 0.95},");
            sb.AppendLine("    {\"head_entity\": \"机器学习\", \"tail_entity\": \"人工智能\", \"relation_type\": \"PART_OF\", \"description\": \"机器学习是人工智能的组成部分\", \"confidence\": 0.90},");
            sb.AppendLine("    {\"head_entity\": \"深度学习\", \"tail_entity\": \"人工智能\", \"relation_type\": \"EVOLVES_FROM\", \"description\": \"深度学习是人工智能的发展方向\", \"confidence\": 0.85}");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
        }
    }
}
