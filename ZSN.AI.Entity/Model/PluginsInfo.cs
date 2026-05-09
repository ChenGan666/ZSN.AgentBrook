using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_plugins_info
    /// </summary>
    public partial class PluginsInfo
    {
        public PluginsInfo() { }
        #region AutoField
        private string _name = string.Empty;
        private string _namespaceName = string.Empty;
        private string _className = string.Empty;
        private string _entryFunctionName = string.Empty;
        /// <summary>
        /// PluginsID
        /// </summary>
        public int PluginsID { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = ZeroWidthCleaner.Clean(value) ?? string.Empty;
        }
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Namespace
        /// </summary>
        public string Namespace
        {
            get => _namespaceName;
            set => _namespaceName = ZeroWidthCleaner.Clean(value) ?? string.Empty;
        }
        /// <summary>
        /// ClassName
        /// </summary>
        public string ClassName
        {
            get => _className;
            set => _className = ZeroWidthCleaner.Clean(value) ?? string.Empty;
        }
        /// <summary>
        /// EntryFunctionName
        /// </summary>
        public string EntryFunctionName
        {
            get => _entryFunctionName;
            set => _entryFunctionName = ZeroWidthCleaner.Clean(value) ?? string.Empty;
        }
        /// <summary>
        /// ReturnValueDescription
        /// </summary>
        public string ReturnValueDescription { get; set; }
        /// <summary>
        /// SystemStatus
        /// </summary>
        public PluginsState SystemStatus { get; set; } = PluginsState.Disabled;
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// LastUpdateTime
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;

        public int PluginsType { get; set; } = 0;
        public string Config { get; set; } = string.Empty;
        #endregion
    }

    public enum PluginsState
    {
        Disabled = 1,
        Normal = 0
    }
    public partial class CallFunction
    {
        public CallFunction() { }
        public object FunctionClass { get; set; }
        public string FunctionClassName { get; set; }
        public string FunctionName { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Input { get;set; } = string.Empty;
    }
}
