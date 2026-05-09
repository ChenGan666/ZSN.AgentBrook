namespace ZSN.AI.Core.Models.Video.Parameters
{
    /// <summary>
    /// MiniMax Hailuo 模型参数
    /// 支持模型: MiniMax-Hailuo-2.3 (T2V和I2V)
    /// </summary>
    public class HailuoVideoParameters : VideoGenerationRequest
    {
        /// <summary>
        /// 视频时长(秒)
        /// 默认: 6
        /// </summary>
        public new int Duration { get; set; } = 6;

        /// <summary>
        /// 分辨率
        /// 支持: "1080P", "720P"
        /// 默认: "1080P"
        /// </summary>
        public new string Resolution { get; set; } = "1080P";

        /// <summary>
        /// 是否启用提示词优化
        /// 默认: true
        /// </summary>
        public bool PromptOptimizer { get; set; } = true;

        /// <summary>
        /// 是否快速预处理
        /// 默认: false
        /// </summary>
        public bool FastPretreatment { get; set; } = false;

        /// <summary>
        /// 是否添加AIGC水印
        /// 默认: false
        /// </summary>
        public bool AigcWatermark { get; set; } = false;

        /// <summary>
        /// 获取对应的模型名称
        /// </summary>
        /// <returns>模型名称</returns>
        public static string GetModelName()
        {
            return "MiniMax-Hailuo-2.3";
        }
    }
}
