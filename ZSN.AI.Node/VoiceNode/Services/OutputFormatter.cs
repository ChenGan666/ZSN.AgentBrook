using System.Text;
using Newtonsoft.Json;
using ZSN.AI.Node.VoiceNode.Models;

namespace ZSN.AI.Node.VoiceNode.Services
{
    /// <summary>
    /// 输出格式化器
    /// </summary>
    public static class OutputFormatter
    {
        public static string Format(TranscriptionResult result, VoiceOutputFormat format)
        {
            return format switch
            {
                VoiceOutputFormat.PlainText => FormatPlainText(result),
                VoiceOutputFormat.SegmentsJson => JsonConvert.SerializeObject(result.Segments, Formatting.Indented),
                VoiceOutputFormat.SRT => FormatSRT(result),
                VoiceOutputFormat.VTT => FormatVTT(result),
                _ => result.FullText
            };
        }

        private static string FormatPlainText(TranscriptionResult result)
        {
            if (result.Speakers == null || result.Speakers.Count <= 1)
                return result.FullText;

            var sb = new StringBuilder();
            foreach (var segment in result.Segments)
            {
                var label = segment.SpeakerLabel ?? "未知";
                sb.AppendLine($"[{label}] {segment.Text}");
            }
            return sb.ToString().Trim();
        }

        private static string FormatSRT(TranscriptionResult result)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < result.Segments.Count; i++)
            {
                var seg = result.Segments[i];
                sb.AppendLine((i + 1).ToString());
                sb.AppendLine($"{FormatSRTTime(seg.StartTimeMs)} --> {FormatSRTTime(seg.EndTimeMs)}");
                var text = seg.SpeakerLabel != null ? $"[{seg.SpeakerLabel}] {seg.Text}" : seg.Text;
                sb.AppendLine(text);
                sb.AppendLine();
            }
            return sb.ToString().Trim();
        }

        private static string FormatVTT(TranscriptionResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("WEBVTT");
            sb.AppendLine();
            foreach (var seg in result.Segments)
            {
                sb.AppendLine($"{FormatVTTTime(seg.StartTimeMs)} --> {FormatVTTTime(seg.EndTimeMs)}");
                var text = seg.SpeakerLabel != null ? $"<v {seg.SpeakerLabel}>{seg.Text}" : seg.Text;
                sb.AppendLine(text);
                sb.AppendLine();
            }
            return sb.ToString().Trim();
        }

        private static string FormatSRTTime(long ms) =>
            TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss\,fff");

        private static string FormatVTTTime(long ms) =>
            TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss\.fff");
    }
}
