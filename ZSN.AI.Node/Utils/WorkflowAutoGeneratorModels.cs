namespace ZSN.AI.Node.Utils
{
    /// <summary>
    /// Phase 1 输出：工作流规划
    /// </summary>
    public class WorkflowPlan
    {
        public List<PlanStep> Steps { get; set; } = new();
        public List<PlanEdgeDef> Edges { get; set; } = new();
    }

    public class PlanStep
    {
        public int StepIndex { get; set; }              // 从 1 开始，0=源节点
        public string NodeType { get; set; }             // NodeType 名称，如 "LargeModel"
        public string NodeName { get; set; }             // 节点显示名称
        public string Description { get; set; }          // 功能描述
        public List<PlanIO> Inputs { get; set; } = new();
        public List<PlanIO> Outputs { get; set; } = new();
    }

    public class PlanIO
    {
        public string Varname { get; set; }              // 变量名
        public string SourceRef { get; set; }            // 占位符引用: {S}_prompt / {STEP1}_results
        public string Type { get; set; } = "string";
        public string Txt { get; set; } = "";
    }

    public class PlanEdgeDef
    {
        public int FromStepIndex { get; set; }
        public int ToStepIndex { get; set; }
    }

    /// <summary>
    /// SSE 流式输出事件
    /// </summary>
    public class StreamEvent
    {
        public string EventType { get; set; }            // progress | plan | node | error | complete
        public object Data { get; set; }
    }

    public class ProgressData
    {
        public string Phase { get; set; }                // planning | generating | assembling
        public string Message { get; set; }
        public int? StepIndex { get; set; }
        public int? TotalSteps { get; set; }
    }

    /// <summary>
    /// Phase 3 组装完成后的响应数据
    /// </summary>
    public class CompleteData
    {
        public string WorkflowID { get; set; }
        public string MainID { get; set; }
        public int MainType { get; set; }
        public List<NodeData> Nodes { get; set; } = new();
        public List<EdgeData> Edges { get; set; } = new();
    }

    public class NodeData
    {
        public string NodeID { get; set; }
        public string NodeType { get; set; }
        public string NodeName { get; set; }
        public string Description { get; set; }
        public object Config { get; set; }
    }

    public class EdgeData
    {
        public string EdgeID { get; set; }
        public string SourceNodeId { get; set; }
        public string TargetNodeId { get; set; }
        public object Config { get; set; }
    }
}
