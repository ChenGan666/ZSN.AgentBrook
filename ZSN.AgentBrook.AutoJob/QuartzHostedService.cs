using Microsoft.Extensions.Hosting;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AgentBrook.AutoJob;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.AutoJob
{
    public class QuartzHostedService : IHostedService
    {
        private readonly IScheduler _scheduler;

        // Job配置映射：JobName -> (Job类型, Trigger名称, Trigger组名)
        private static readonly Dictionary<string, (Type JobType, string TriggerName, string GroupName)> JobMappings = new Dictionary<string, (Type, string, string)>
        {
            { "TimeTrigger", (typeof(TimeTrigger), "TimeTrigger_Trigger", "TimeTrigger_Group") },
            { "AIDispatcher", (typeof(AIDispatcher), "AIDispatcher_Trigger", "AIDispatcher_Group") },
            { "FileChunk", (typeof(FileChunkJob), "FileChunk_Trigger", "FileChunk_Group") },
            { "Node", (typeof(NodeJob), "Node_Trigger", "Node_Group") },
            { "SessionTopic", (typeof(SessionTopicJob), "SessionTopic_Trigger", "SessionTopic_Group") },
            { "Markdown", (typeof(MarkdownJob), "Markdown_Trigger", "Markdown_Group") },
            { "Cleaner", (typeof(CleanerJob), "Cleaner_Trigger", "Cleaner_Group") },
            { "FileToKnowledgeBase", (typeof(FileToKnowledgeBaseJob), "FileToKnowledgeBase_Trigger", "FileToKnowledgeBase_Group") },
            { "MemoryConsolidation", (typeof(MemoryConsolidationJob), "MemoryConsolidation_Trigger", "MemoryConsolidation_Group") },
            { "ClawAIStepTimeout", (typeof(ClawAIStepTimeoutJob), "ClawAIStepTimeout_Trigger", "ClawAIStepTimeout_Group") }
        };

        public QuartzHostedService(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var jobs = ConfigHelper.GetSection("Job").GetChildren().ToArray();

            foreach (var job in jobs)
            {
                var jobName = job.GetSection("JobName").Value;
                if (!JobMappings.TryGetValue(jobName, out var jobConfig))
                    continue;

                var loopTimerSeconds = int.Parse(job.GetSection("LoopTimerSeconds").Value ?? "1000");
                var jobDetail = JobBuilder.Create(jobConfig.JobType).Build();

                // 支持 CronSchedule 和 SimpleSchedule 两种调度方式
                if (loopTimerSeconds <= 0)
                {
                    // 使用 Cron 表达式调度
                    var cronSchedule = job.GetSection("WithCronSchedule").Value;
                    if (!string.IsNullOrEmpty(cronSchedule))
                    {
                        var trigger = TriggerBuilder.Create()
                            .WithIdentity(jobConfig.TriggerName, jobConfig.GroupName)
                            .WithCronSchedule(cronSchedule)
                            .Build();
                        await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
                        Console.WriteLine($"已调度Job: {jobName}, Cron: {cronSchedule}");
                    }
                }
                else
                {
                    // 使用固定间隔调度
                    var trigger = TriggerBuilder.Create()
                        .StartNow()
                        .WithIdentity(jobConfig.TriggerName, jobConfig.GroupName)
                        .WithSimpleSchedule(t => t.WithIntervalInSeconds(loopTimerSeconds).RepeatForever())
                        .Build();
                    await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
                    Console.WriteLine($"已调度Job: {jobName}, 间隔: {loopTimerSeconds}秒");
                }
            }

            // 所有任务添加完成后，启动调度器
            await _scheduler.Start(cancellationToken);
            Console.WriteLine("Quartz Scheduler 已启动");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // 停止调度器
            await _scheduler.Shutdown(cancellationToken);
            Console.WriteLine("Quartz Scheduler stopped.");
        }
    }
}