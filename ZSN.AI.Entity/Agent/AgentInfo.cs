using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_app_info
    /// </summary>
    public partial class AgentInfo
    {
		public AgentInfo() { }
        #region AutoField
		/// <summary>
        /// AppID
        /// </summary>
		public string AgentID { get; set; } = string.Empty;
        public string MemberID { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        public string AICON { get; set; } = string.Empty;
        /// <summary>
        /// DicIDList
        /// </summary>
        public string DicIDList { get; set; } = string.Empty;
        /// <summary>
        /// DicNameList
        /// </summary>
        public string DicNameList { get; set; } = string.Empty;
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// SessionModelID
        /// </summary>
        public Int32? SessionModelID { get; set; }
        public string? SessionModelName { get; set; } = string.Empty;
        /// <summary>
        /// VectorModelID
        /// </summary>
        //public Int32? VectorModelID { get; set; }
        //public string? VectorModelName { get; set; } = string.Empty;
        /// <summary>
        /// Prompt
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// TemperatureCoefficient
        /// </summary>
        public double TemperatureCoefficient { get; set; } = 0;
        /// <summary>
        /// TopPCoefficient
        /// </summary>
        public double TopPCoefficient { get; set; } = 0;
		/// <summary>
        /// SystemStatus
        /// </summary>
		public AgentStatus SystemStatus { get; set; } = AgentStatus.UnderReview;
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// LastUpdateTime
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// KnowledgeBases
        /// </summary>
        public List<AgentKnowledgeBaseInfo> KnowledgeBases { get; set; } = new List<AgentKnowledgeBaseInfo>();
        #endregion
    }
    public enum AgentStatus
    {
        /// <summary>
        /// Õý³£
        /// </summary>
        Normal = 1,
        /// <summary>
        /// ½ûÓÃ
        /// </summary>
        Disable = -1,
        /// <summary>
        /// Î´ÉóºË
        /// </summary>
        UnderReview = 0
    }
}
