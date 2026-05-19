using ZSN.AI.Node.VoiceNode.Models;

namespace ZSN.AI.Node.VoiceNode.Services
{
    /// <summary>
    /// 说话人标签规范化
    /// </summary>
    public static class SpeakerLabelNormalizer
    {
        private static readonly string[] DefaultLabels = {
            "发言人A", "发言人B", "发言人C", "发言人D", "发言人E",
            "发言人F", "发言人G", "发言人H", "发言人I", "发言人J"
        };

        public static void Normalize(TranscriptionResult result, Dictionary<string, string> labelMap)
        {
            if (result.Speakers == null || result.Speakers.Count == 0) return;

            for (int i = 0; i < result.Speakers.Count; i++)
            {
                var speaker = result.Speakers[i];
                if (labelMap != null && labelMap.TryGetValue(speaker.SpeakerId, out var customLabel))
                    speaker.Label = customLabel;
                else
                    speaker.Label = i < DefaultLabels.Length ? DefaultLabels[i] : $"发言人{i + 1}";
            }

            foreach (var segment in result.Segments)
            {
                if (string.IsNullOrEmpty(segment.SpeakerId)) continue;
                var speaker = result.Speakers.FirstOrDefault(s => s.SpeakerId == segment.SpeakerId);
                segment.SpeakerLabel = speaker?.Label ?? segment.SpeakerId;
            }
        }
    }
}
