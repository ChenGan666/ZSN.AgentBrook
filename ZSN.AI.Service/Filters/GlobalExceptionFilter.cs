using Microsoft.AspNetCore.Mvc.Filters;
using ZSN.AI.Service.WebHelpers;

namespace ZSN.AI.Service.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            DefaultLogService.AddOperationLog(1, filterContext.Exception);
            filterContext.ExceptionHandled = false;
        }
    }
}
