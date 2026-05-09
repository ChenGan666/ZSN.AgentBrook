using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace ZSN.AI.MCPServer.ConfigureSwagger
{
    /// <summary>
    /// 从 Swagger 文档中移除基类属性参数的过滤器
    /// 防止基类的公共属性（如 PostFile）被错误地显示为 API 参数
    /// </summary>
    public class RemoveBaseClassPropertiesFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            // 获取方法的实际参数名称
            var actualParameterNames = context.ApiDescription.ActionDescriptor.Parameters
                .Select(p => p.Name)
                .ToHashSet();

            // 要排除的基类属性名称列表
            var excludedProperties = new[] { "PostFile", "BodyParams", "Token", "MemberToken", "URL" };

            // 只移除那些不是实际方法参数的基类属性
            var parametersToRemove = operation.Parameters
                .Where(p => excludedProperties.Contains(p.Name) && !actualParameterNames.Contains(p.Name))
                .ToList();

            foreach (var parameter in parametersToRemove)
            {
                operation.Parameters.Remove(parameter);
            }

            // 如果这是一个 GET 请求，确保没有 RequestBody
            if (context.ApiDescription.HttpMethod?.ToUpper() == "GET")
            {
                operation.RequestBody = null;
            }
        }
    }
}
