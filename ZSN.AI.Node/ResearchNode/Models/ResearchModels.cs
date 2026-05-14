namespace ZSN.AI.Node.ResearchNode.Models
{
    public class SearchPlan
    {
        public List<string> Keywords { get; set; } = new();
        public string Language { get; set; } = "zh-CN";
        public List<string> Categories { get; set; } = new() { "general" };
        public string TimeRange { get; set; } = "";
    }

    public class SearchResultItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Snippet { get; set; }
        public string Engine { get; set; }
        public double Score { get; set; }
    }

    public class WebPageContent
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime FetchTime { get; set; }
        public int ContentLength { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class AnalysisResult
    {
        public string OrganizedSummary { get; set; }
        public double CompletenessScore { get; set; }
        public bool IsSatisfied { get; set; }
        public List<string> Gaps { get; set; } = new();
        public List<string> SuggestedKeywords { get; set; } = new();
        public string Reasoning { get; set; }
    }
}
