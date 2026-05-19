namespace ZSN.AI.Node.VoiceNode.Providers.FunASR
{
    /// <summary>
    /// FunASR 配置
    /// </summary>
    public class FunASROptions
    {
        /// <summary>FunASR Server 地址</summary>
        public string ServerUrl { get; set; } = "ws://127.0.0.1:10095";

        /// <summary>音频分片大小（字节），默认 9600 ≈ 600ms @16kHz 16bit</summary>
        public int ChunkSize { get; set; } = 9600;

        /// <summary>连接超时（秒）</summary>
        public int ConnectTimeoutSeconds { get; set; } = 5;

        /// <summary>单次转写超时（分钟）</summary>
        public int TranscribeTimeoutMinutes { get; set; } = 10;
    }
}
