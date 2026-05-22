using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZSN.AgentBrook.API.ConfigureSwagger
{
    public class FileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 检查方法参数是否包含文件上传参数（例如 IFormFile）
            var hasFileParam = context.ApiDescription.ActionDescriptor.Parameters
                .Any(p => p.ParameterType == typeof(IFormFile) || p.ParameterType.IsSubclassOf(typeof(IFormFile)));

            if (hasFileParam)
            {
                // 如果找到文件上传参数，则修改操作以支持 multipart/form-data
                operation.RequestBody = new OpenApiRequestBody
                {
                    Description = "The file to upload",
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "multipart/form-data", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        }
                    }
                }
                };

            }
        }
    }
}