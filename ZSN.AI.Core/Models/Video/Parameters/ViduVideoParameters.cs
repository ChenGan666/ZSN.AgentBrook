namespace ZSN.AI.Core.Models.Video.Parameters
{
    /// <summary>
    /// Vidu 模型参数
    /// 支持模型: viduq2, viduq2-pro, viduq2-turbo
    /// </summary>
    public class ViduVideoParameters : VideoGenerationRequest
    {
        /// <summary>
        /// Vidu 生成类型
        /// 可选值: "text2video", "img2video", "reference2video", "start-end2video"
        /// </summary>
        public string ViduType { get; set; }

        /// <summary>
        /// 视频时长(秒)
        /// 默认: 5
        /// </summary>
        public new int Duration { get; set; } = 5;

        /// <summary>
        /// 宽高比 (用于text2video和reference2video)
        /// 支持: "16:9", "9:16", "4:3", "3:4", "1:1"
        /// 默认: "16:9"
        /// </summary>
        public new string AspectRatio { get; set; } = "16:9";

        /// <summary>
        /// 分辨率 (用于img2video和start-end2video)
        /// 支持: "720p", "1080p", "auto"
        /// 默认: "1080p"
        /// </summary>
        public new string Resolution { get; set; } = "1080p";

        /// <summary>
        /// 运动幅度 (用于img2video, reference2video, start-end2video)
        /// 支持: "auto", "small", "medium", "large"
        /// 默认: "auto"
        /// </summary>
        public string MovementAmplitude { get; set; } = "auto";

        /// <summary>
        /// 是否添加背景音乐
        /// 默认: false
        /// </summary>
        public bool Bgm { get; set; } = false;

        /// <summary>
        /// 获取对应的模型名称
        /// </summary>
        /// <param name="modelVersion">模型版本: "q2", "q2-pro", "q2-turbo"</param>
        /// <returns>模型名称</returns>
        public static string GetModelName(string modelVersion = "q2")
        {
            return modelVersion.ToLower() switch
            {
                "q2-pro" => "viduq2-pro",
                "q2-turbo" => "viduq2-turbo",
                _ => "viduq2"
            };
        }

        /// <summary>
        /// 根据生成类型设置ViduType
        /// </summary>
        public void SetViduTypeFromGenerationType()
        {
            ViduType = GenerationType switch
            {
                VideoGenerationType.TextToVideo => "text2video",
                VideoGenerationType.ImageToVideo => "img2video",
                VideoGenerationType.ReferenceToVideo => "reference2video",
                VideoGenerationType.StartEndToVideo => "start-end2video",
                _ => "text2video"
            };
        }
    }
}
