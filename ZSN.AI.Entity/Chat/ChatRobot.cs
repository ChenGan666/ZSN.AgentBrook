using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.Utils.Core.Helpers;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AI.Entity.Chat
{
    public partial class ChatRobot
    {
        public ChatRobot() { }

        public string AppID { get; set; }
        public string Key {  get; set; }
        public string Api { get; set; }
        public string SessionID { get; set; }    
        public static ChatRobot Config
        {
            get
            {
                return ConfigHelper.GetSection("HelperAgent").Get<ChatRobot>();
            }
        }
    }

    public partial class MessageData
    {
        public MessageData() { }
        public string AppID { get; set; }
        public string SessionID { get; set; }
        public string ProcessesID {  get; set; }
        public string TaskID { get; set; }
        public string Content {  get; set; }
        public string Role { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

    }
}
