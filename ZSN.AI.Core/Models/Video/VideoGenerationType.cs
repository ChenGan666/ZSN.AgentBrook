namespace ZSN.AI.Core.Models.Video
{
    /// <summary>
    /// 视频生成类型枚举
    /// </summary>
    public enum VideoGenerationType
    {
        /// <summary>
        /// 文本生成视频 (Text to Video)
        /// </summary>
        TextToVideo = 1,

        /// <summary>
        /// 图片生成视频 (Image to Video)
        /// </summary>
        ImageToVideo = 2,

        /// <summary>
        /// 参考图生成视频 (Reference Images to Video)
        /// 使用1-7张参考图片生成具有主体一致性的视频
        /// </summary>
        ReferenceToVideo = 3,

        /// <summary>
        /// 首尾帧生成视频 (Start-End Frames to Video)
        /// 使用首帧和尾帧图片生成中间过渡视频
        /// </summary>
        StartEndToVideo = 4
    }
}
