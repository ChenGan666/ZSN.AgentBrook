namespace ZSN.AI.Core.Models.Video
{
    /// <summary>
    /// 视频生成任务状态枚举
    /// </summary>
    public enum VideoTaskStatus
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Pending,

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 成功
        /// </summary>
        Success,

        /// <summary>
        /// 失败
        /// </summary>
        Failure
    }
}
