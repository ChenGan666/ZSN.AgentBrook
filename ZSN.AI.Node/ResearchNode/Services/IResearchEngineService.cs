using ZSN.AI.Entity;
using ZSN.AI.Node.ResearchNode.Models;

namespace ZSN.AI.Node.ResearchNode.Services
{
    public interface IResearchEngineService
    {
        Task<SearchPlan> GenerateSearchPlanAsync(
            string researchGoal,
            List<string> previousKeywords,
            string previousFindings,
            LargeModelConfig modelConfig,
            IProgress<string> progress = null);

        Task<AnalysisResult> AnalyzeAndReflectAsync(
            string researchGoal,
            List<WebPageContent> newContents,
            string accumulatedSummary,
            int currentIteration,
            LargeModelConfig modelConfig,
            IProgress<string> progress = null);

        Task<ResearchResult> FormatFinalResultAsync(
            string researchGoal,
            string accumulatedSummary,
            List<SourceInfo> allSources,
            ResearchStats stats,
            LargeModelConfig modelConfig,
            IProgress<string> progress = null);
    }
}
