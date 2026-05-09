using System;
using System.Collections.Generic;

namespace ZSN.AI.Core.Models.Video
{
    /// <summary>
    /// 视频生成响应模型
    /// </summary>
    public class VideoGenerationResponse
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// 请求ID
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public VideoTaskStatus TaskStatus { get; set; }

        /// <summary>
        /// 生成的视频URL列表
        /// </summary>
        public List<string>? VideoUrls { get; set; }

        /// <summary>
        /// 任务提交时间 (Unix时间戳)
        /// </summary>
        public long? SubmitTime { get; set; }

        /// <summary>
        /// 任务完成时间 (Unix时间戳)
        /// </summary>
        public long? FinishTime { get; set; }

        /// <summary>
        /// 错误消息 (任务失败时)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 使用统计信息
        /// </summary>
        public VideoUsageInfo? Usage { get; set; }
    }

    /// <summary>
    /// 视频使用统计信息
    /// </summary>
    public class VideoUsageInfo
    {
        /// <summary>
        /// 视频时长(秒)
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// 其他使用信息
        /// </summary>
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }
}
