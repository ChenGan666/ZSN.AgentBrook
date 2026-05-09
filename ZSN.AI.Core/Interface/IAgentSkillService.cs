using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity.Model;

namespace ZSN.AI.Core.Interface
{
    public interface IAgentSkillService
    {
        Task<AgentSkillResponse> PlanAsync(
            Skill skill,
            SkillsToolsOptions options,
            string prompt,
            List<AttachmentItem> attachments = null,
            IProgress<string> progress = null,
            LargeModelConfig modelConfig = null,
            CancellationToken ct = default);

        Task<AgentSkillResponse> ExecutePlanAsync(
            Skill skill,
            SkillsToolsOptions options,
            string planJson,
            string sessionId = null,
            string processesId = null,
            IProgress<string> progress = null,
            CancellationToken ct = default);

        Task<AgentSkillResponse> ExecuteAsync(
            Skill skill,
            SkillsToolsOptions options,
            string prompt,
            List<AttachmentItem> attachments = null,
            IProgress<string> progress = null,
            LargeModelConfig modelConfig = null,
            string sessionId = null,
            string processesId = null,
            CancellationToken ct = default);

        Task<AgentSkillResponse> ExecuteWithPlanTrackingAsync(
            Skill skill,
            SkillsToolsOptions options,
            string prompt,
            List<AttachmentItem> attachments = null,
            IProgress<string> progress = null,
            LargeModelConfig modelConfig = null,
            string sessionId = null,
            string processesId = null,
            CancellationToken ct = default);
    }

    public sealed class AgentSkillResponse
    {
        public string Output { get; set; } = string.Empty;
        public List<Output> Outputs { get; set; } = new List<Output>();
        public List<string> Logs { get; set; } = new List<string>();
    }
}
