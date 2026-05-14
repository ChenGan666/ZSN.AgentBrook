namespace ZSN.AI.Node.ResearchNode
{
    public class ResearchNodeOptions
    {
        public string SearXNGBaseUrl { get; set; } = "http://10.10.10.2:8800";
        public int SearchTimeoutSeconds { get; set; } = 10;
        public int MaxResultsPerQuery { get; set; } = 10;
        public int MaxConcurrentFetches { get; set; } = 3;
        public int PageTimeoutSeconds { get; set; } = 30;
        public int MaxContentLength { get; set; } = 5000;
        public bool Headless { get; set; } = true;
        public string ChromiumExecutablePath { get; set; }
        public int DefaultMaxIterations { get; set; } = 3;
        public int DefaultMaxFetchUrls { get; set; } = 5;
        public string DefaultSearchLanguage { get; set; } = "zh-CN";
        public int DefaultMaxLLMCalls { get; set; } = 6;
        public double DefaultCompletionThreshold { get; set; } = 0.8;
        public string BrowserUserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        public List<string> BlockedDomains { get; set; } = new();

        public bool EnableContentCache { get; set; } = true;
        public int CacheExpiryHours { get; set; } = 24;

        public int OverallTimeoutMinutes { get; set; } = 5;
    }
}
