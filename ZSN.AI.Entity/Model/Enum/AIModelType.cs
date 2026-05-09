using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ZSN.Utils.Core.Helpers;
using static log4net.Appender.ColoredConsoleAppender;

namespace ZSN.AI.Entity.Model.Enum
{
    /// <summary>
    /// AI类型
    /// </summary>
    public enum AIType
    {
        
        [Display(Name = "Open AI")]
        OpenAI = 1,

        [Display(Name = "Bigmodel")]
        Bigmodel = 2,

        [Display(Name = "QWen")]
        QWen = 3,

        [Display(Name = "DeepSeek")]
        DeepSeek = 4,

        [Display(Name = "Ollama")]
        Ollama = 10,

        [Display(Name = "Compshare")]
        Compshare = 11,


        [Display(Name = "模拟输出")]
        Mock = 100,

    }
    public partial class AIOrganization
    {
        public AIOrganization() { }

        public int ID { get; set; }
        public string Name { get; set; }
    }
    public partial class AIOrganizationList
    {
        public AIOrganizationList()
        {
        }
        public List<AIOrganization> List()
        {
            return AIType.GetValues(typeof(AIType)).Cast<AIType>()
            .Select(e =>
            {
                var fieldInfo = typeof(AIType).GetField(e.ToString());
                var displayAttribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
                return new AIOrganization
                {
                    Name = displayAttribute != null ? displayAttribute.Name : e.ToString(),
                    ID = (int)e
                };
            })
            .ToList();
        }
    }

    
    /// <summary>
    /// 模型类型
    /// </summary>
    public enum AIModelType
    {
        [Display(Name = "Chat")]
        Chat = 1,

        [Display(Name = "Embedding")]
        Embedding = 2,

        [Display(Name = "Rerank")]
        Rerank = 3,

        [Display(Name = "Txt2Image")]
        T2Image =4,

        [Display(Name = "Image2Image")]
        I2Image = 5,

        [Display(Name = "Txt2Video")]
        T2Video = 6,

        [Display(Name = "Image2Video")]
        I2Video = 7
    }
    public partial class ModelType
    {
        public ModelType() { }

        public int ID { get; set; }
        public string Name { get; set; }
    }

    public partial class AIModelTypeList
    {
        public AIModelTypeList()
        {
        }
        public List<ModelType> List()
        {
            return AIModelType.GetValues(typeof(AIModelType)).Cast<AIModelType>()
            .Select(e =>
            {
                var fieldInfo = typeof(AIModelType).GetField(e.ToString());
                var displayAttribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
                return new ModelType
                {
                    Name = displayAttribute != null ? displayAttribute.Name : e.ToString(),
                    ID = (int)e
                };
            })
            .ToList();
        }
    }

}
