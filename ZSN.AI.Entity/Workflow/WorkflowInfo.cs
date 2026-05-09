using NPOI.HSSF.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.Entity
{

    public enum MainType
    {
        Unknown = 0,
        APP = 1,
        Agent = 2,
    }
    public enum WorkflowStatus
    {
        Disable = -1,
        Unreleased = 0,
        Normal = 1,
    }

    /// <summary>
    /// tb_workflow_info
    /// </summary>
    public partial class WorkflowInfo
    {
        public WorkflowInfo() { }
        #region AutoField
        /// <summary>
        /// WorkflowID
        /// </summary>
        public string WorkflowID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// MainType
        /// </summary>
        public MainType MainType { get; set; } = 0;
        /// <summary>
        /// MainID
        /// </summary>
        public string MainID { get; set; }
        public string WorkflowName { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// SystemStatus
        /// </summary>
        public WorkflowStatus SystemStatus { get; set; } = WorkflowStatus.Unreleased;
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime? CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// LastUpdateTime
        /// </summary>
        public DateTime? LastUpdateTime { get; set; } = DateTime.Now;
        public object? Config {get;set;}
        #endregion
    }

    public partial class WorkFlowConfig
    {
        public WorkFlowConfig() { }
        public string SessionID { get; set; } = "";
        public string ProcessesID { get; set; } = "";
    }

    public partial class WorkFlow
    {
        public WorkFlow() { }
        public string WorkflowID { get; set; } = "";
        public string MainID { get; set; } = "";
        public MainType MainType { get; set; } = 0;
        public WorkflowInfo Info { get; set; } = new WorkflowInfo();
        public List<WorkflowNodeInfo> Nodes { get; set; } = new List<WorkflowNodeInfo>();
        public List<WorkflowEdgeInfo> Edges { get; set; } = new List<WorkflowEdgeInfo>();

        public WorkFlowConfig Config { get; set; } = new WorkFlowConfig();
    }

    public partial class WorkFlowProcesses
    {
        public WorkFlowProcesses() { }
        public string WorkflowID { get; set; }
        public string ProcessesID { get; set; }
    }
}


