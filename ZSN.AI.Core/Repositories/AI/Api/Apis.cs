﻿using ZSN.AI.Entity.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ZSN.AI.Core.Repositories
{
    public partial class Apis
    {
        public string Id { get; set; }

        /// <summary>
        /// 接口名称
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// 接口描述
        /// </summary>
        [Required]
        public string Describe { get; set; }
        /// <summary>
        /// 接口地址
        /// </summary>
        [Required]
        public string Url { get; set; }
        /// <summary>
        /// 请求方法
        /// </summary>
        [Required]
        public HttpMethodType Method { get; set; }

        public string? Header { get; set; }
        /// <summary>
        /// QueryString参数
        /// </summary>
        public string? Query { get; set; }
        /// <summary>
        /// jsonBody 实体
        /// </summary>
        public string? JsonBody { get; set; }

        /// <summary>
        /// 入参提示词
        /// </summary>
        [Required]
        public string InputPrompt { get; set; }

        /// <summary>
        /// 返回提示词
        /// </summary>
        [Required]
        public string OutputPrompt { get; set; }
    }
}
