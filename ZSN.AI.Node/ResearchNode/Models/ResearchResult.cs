namespace ZSN.AI.Node.ResearchNode.Models
{
    public class ResearchResult
    {
        public string Summary { get; set; }
        public string DetailedContent { get; set; }
        public List<KeyFinding> KeyFindings { get; set; } = new();
        public List<SourceInfo> Sources { get; set; } = new();
        public ResearchStats Stats { get; set; } = new();
    }

    public class KeyFinding
    {
        public string Finding { get; set; }
        public string SourceUrl { get; set; }
        public string Dimension { get; set; }
    }

    public class SourceInfo
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime FetchTime { get; set; }
        public string Snippet { get; set; }
    }

    public class ResearchStats
    {
        public int Iterations { get; set; }
        public int TotalPagesFetched { get; set; }
        public int TotalSourcesUsed { get; set; }
        public int LLMCallsUsed { get; set; }
        public double FinalCompletenessScore { get; set; }
        public long TotalElapsedMs { get; set; }
    }
}
