using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
	/// <summary>
    /// tb_menu_info
    /// </summary>
    public partial class MenuInfo
    {
		public MenuInfo() { }
        #region AutoField
		/// <summary>
        /// ID
        /// </summary>
		public string ID { get; set; } = string.Empty;
		/// <summary>
        /// ParentID
        /// </summary>
		public string ParentID { get; set; } 
		/// <summary>
        /// Url
        /// </summary>
		public string Url { get; set; } 
		/// <summary>
        /// Title
        /// </summary>
		public string Title { get; set; } 
		/// <summary>
        /// Params
        /// </summary>
		public string Params { get; set; } 
		/// <summary>
        /// Ico
        /// </summary>
		public string Ico { get; set; } 
		/// <summary>
        /// Sort
        /// </summary>
		public Int32? Sort { get; set; } 
		/// <summary>
        /// IcoColor
        /// </summary>
		public string IcoColor { get; set; } 
		/// <summary>
        /// MState
        /// </summary>
		public Int32? MState { get; set; } 
        #endregion
    }
}
