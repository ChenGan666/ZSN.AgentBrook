using System;
using System.Diagnostics;
using ZSN.Utils.Core.DI;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ZSN.AI.Service.Attributes
{
    public class RunTime : ActionFilterAttribute
    {
        private readonly Stopwatch _watch = new Stopwatch();

        public const string RunTimeKey = "_$runtime$_";

        private static IHttpContextAccessor ContextAccessor => ServiceLocator.GetInstance<IHttpContextAccessor>();

        private static ISession Session => ContextAccessor.HttpContext.Session;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _watch.Start();
            Session.Set(RunTimeKey, "");
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            _watch.Stop();
            var time = $"action: {_watch.ElapsedMilliseconds}";
            Session.Set(RunTimeKey, time);
            base.OnActionExecuted(context);
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            _watch.Reset();
            _watch.Start();
            base.OnResultExecuting(context);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            _watch.Stop();
            string time = Session.Get<string>(RunTimeKey);
            time = time + Environment.NewLine + $"action: {_watch.ElapsedMilliseconds}";
            Session.Set(RunTimeKey, time);
            base.OnResultExecuted(context);
        }
    }
}