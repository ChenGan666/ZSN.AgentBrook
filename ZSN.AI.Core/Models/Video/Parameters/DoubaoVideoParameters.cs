namespace ZSN.AI.Core.Models.Video.Parameters
{
    /// <summary>
    /// Doubao (豆包) 模型参数
    /// 支持模型: doubao-seedance-1-5-pro-251215
    /// </summary>
    public class DoubaoVideoParameters : VideoGenerationRequest
    {
        /// <summary>
        /// 视频时长(秒)
        /// 根据模型文档设置默认值
        /// </summary>
        public new int? Duration { get; set; }

        /// <summary>
        /// 分辨率
        /// 根据模型文档设置支持的分辨率
        /// </summary>
        public new string? Resolution { get; set; }

        /// <summary>
        /// 获取对应的模型名称
        /// </summary>
        /// <returns>模型名称</returns>
        public static string GetModelName()
        {
            return "doubao-seedance-1-5-pro-251215";
        }
    }
}
