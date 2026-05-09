using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_app_chat_log_info
    /// </summary>
    public partial class AppChatLogInfo
    {
        public AppChatLogInfo() { }
        #region AutoField
        /// <summary>
        /// ChatLogID
        /// </summary>
        public string ChatLogID { get; set; } = string.Empty;
        public string AppID {  get; set; } = string.Empty;
        /// <summary>
        /// ChatSessionID
        /// </summary>
        public string ChatSessionID { get; set; }
        /// <summary>
        /// Direction
        /// </summary>
        public Int32 Direction { get; set; }
        /// <summary>
        /// Role
        /// </summary>
        public string Role { get; set; } = string.Empty;
        /// <summary>
        /// LargeModelID
        /// </summary>
        public Int32? LargeModelID { get; set; }
        /// <summary>
        /// Content
        /// </summary>
        public object Content { get; set; }
        /// <summary>
        /// CreateTime
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// LogOrder
        /// </summary>
        public int LogOrder { get; set; }

        public GptMsg ContentToGptMsg
        {
            get
            {
                if (Content != null)
                {
                    return JsonConvert.DeserializeObject<GptMsg>(Content.ToString());
                }
                return null;
            }
        }
        #endregion
    }
    public partial class ChatLog
    {
        public ChatLog() { }
        public List<AppChatLogInfo> Log { get; set; } = new List<AppChatLogInfo>();
    }
}
