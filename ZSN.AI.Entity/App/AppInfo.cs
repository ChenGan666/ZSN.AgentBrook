using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{

    public enum AppStatus
    {
        Disable = -1,
        Unaudited = 0,
        Stop = 1,
        Running = 2
    }
    /// <summary>
    /// tb_app_info
    /// </summary>
    public partial class AppInfo
    {
		public AppInfo() { }
        #region AutoField
		/// <summary>
        /// AppID
        /// </summary>
		public string AppID { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string MemberID { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        public string AICON { get; set; } = string.Empty;
        public List<string> AICONList { get; set; } = new List<string>();
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
        public Int32 SessionModelID { get; set; }
        public string SessionModelName { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        /// <summary>
        /// TemperatureCoefficient
        /// </summary>
        public int TemperatureCoefficient { get; set; } = 70;
        /// <summary>
        /// TopPCoefficient
        /// </summary>
        public int TopPCoefficient { get; set; } = 70;
        /// <summary>
        /// SystemStatus
        /// </summary>
        public AppStatus SystemStatus { get; set; } = AppStatus.Unaudited;
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// LastUpdateTime
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;

        public string WorkFlowID { get; set; } = string.Empty;
        #endregion
    }
}
