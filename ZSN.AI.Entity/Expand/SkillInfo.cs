using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_skill_info
    /// </summary>
    public partial class SkillInfo
    {
		public SkillInfo() { }
        #region AutoField
		/// <summary>
        /// SkillID
        /// </summary>
		public string SkillID { get; set; } = string.Empty;
		/// <summary>
        /// sName
        /// </summary>
		public string SName { get; set; } = string.Empty;
		/// <summary>
        /// sDescription
        /// </summary>
		public string SDescription { get; set; } 
		/// <summary>
        /// SkillDirectory
        /// </summary>
		public string SkillDirectory { get; set; } = string.Empty;
		/// <summary>
        /// CreateTime
        /// </summary>
		public DateTime CreateTime { get; set; } = DateTime.Now;
		/// <summary>
        /// UpdateTime
        /// </summary>
		public DateTime UpdateTime { get; set; } = DateTime.Now;
		/// <summary>
        /// SystemStatus
        /// </summary>
		public Int32 SystemStatus { get; set; } = (Int32)(1);
        #endregion
    }
    public enum SkillStatus
    {
        Disabled = 1,
        Normal = 0
    }
}
