using ZSN.AgentBrook.MessageGateway.Models;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.MessageGateway.Interfaces
{
    public interface IMessageRouter
    {
        Task<RouteResult> RouteAsync(ReceiveMessageEvent @event, ChannelConfigInfo channelConfig);
    }

    public class RouteResult
    {
        public bool Matched { get; set; }
        public string MatchedRuleID { get; set; }
        public string CreatedTaskID { get; set; }
        public string SessionID { get; set; }
        public string ErrorMessage { get; set; }
    }
}
