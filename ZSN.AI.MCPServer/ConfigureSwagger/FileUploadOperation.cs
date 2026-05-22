using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZSN.AI.MCPServer.ConfigureSwagger
{
    public class FileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 检查方法参数是否包含文件上传参数（例如 IFormFile）
            var fileParameters = context.ApiDescription.ActionDescriptor.Parameters
                .Where(p => p.ParameterType == typeof(IFormFile) || 
                           p.ParameterType == typeof(IFormFileCollection) ||
                           typeof(IFormFile).IsAssignableFrom(p.ParameterType))
                .ToList();

            if (fileParameters.Any())
            {
                // 构建 multipart/form-data 的 schema
                var properties = new Dictionary<string, OpenApiSchema>();
                
                foreach (var param in fileParameters)
                {
                    properties.Add(param.Name, new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = $"上传的文件: {param.Name}"
                    });
                }

                // 设置 RequestBody
                operation.RequestBody = new OpenApiRequestBody
                {
                    Description = "文件上传",
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        { 
                            "multipart/form-data", 
                            new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = properties,
                                    Required = fileParameters.Select(p => p.Name).ToHashSet()
                                }
                            }
                        }
                    }
                };

                // 移除从参数列表中显示的文件参数（因为它们现在在 RequestBody 中）
                var fileParamNames = fileParameters.Select(p => p.Name).ToHashSet();
                var parametersToRemove = operation.Parameters
                    .Where(p => fileParamNames.Contains(p.Name))
                    .ToList();
                
                foreach (var param in parametersToRemove)
                {
                    operation.Parameters.Remove(param);
                }
            }
        }
    }
}