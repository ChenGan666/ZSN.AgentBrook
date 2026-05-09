using System.Collections.Generic;
using System.Threading.Tasks;
using ZSN.AI.Entity;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// 结果解析服务接口
    /// 负责从步骤执行结果中提取结构化数据
    /// </summary>
    public interface IResultParserService
    {
        /// <summary>
        /// 从结果中提取 JSON 对象
        /// </summary>
        /// <param name="result">步骤执行结果</param>
        /// <param name="jsonPath">JSON 路径（可选，如 "$.data.items"）</param>
        /// <returns>提取的 JSON 对象</returns>
        Task<object> ExtractJsonAsync(string result, string jsonPath = null);

        /// <summary>
        /// 从结果中提取数组（用于循环执行）
        /// </summary>
        /// <param name="result">步骤执行结果</param>
        /// <param name="arrayPath">数组路径（可选）</param>
        /// <returns>提取的数组</returns>
        Task<List<object>> ExtractArrayAsync(string result, string arrayPath = null);

        /// <summary>
        /// 使用正则表达式提取数据
        /// </summary>
        /// <param name="result">步骤执行结果</param>
        /// <param name="pattern">正则表达式模式</param>
        /// <param name="groupName">捕获组名称（可选）</param>
        /// <returns>提取的匹配项列表</returns>
        Task<List<string>> ExtractByRegexAsync(string result, string pattern, string groupName = null);

        /// <summary>
        /// 使用 LLM 智能提取结构化数据
        /// </summary>
        /// <param name="result">步骤执行结果</param>
        /// <param name="extractionPrompt">提取指令（如："提取所有需要生成的图片描述"）</param>
        /// <param name="modelConfig">LLM 模型配置</param>
        /// <returns>提取的结构化数据（JSON 格式）</returns>
        Task<string> ExtractByLLMAsync(string result, string extractionPrompt, LargeModelConfig modelConfig);

        /// <summary>
        /// 从结果中提取键值对
        /// </summary>
        /// <param name="result">步骤执行结果</param>
        /// <param name="keys">要提取的键列表</param>
        /// <returns>键值对字典</returns>
        Task<Dictionary<string, string>> ExtractKeyValuesAsync(string result, List<string> keys);

        /// <summary>
        /// 将提取的数据转换为 Inputs 列表（用于传递给下一步骤）
        /// </summary>
        /// <param name="data">提取的数据</param>
        /// <param name="template">参数模板（如 {"varname": "prompt", "valueKey": "description"}）</param>
        /// <returns>Inputs 列表</returns>
        Task<List<Inputs>> ConvertToInputsAsync(object data, Dictionary<string, string> template);
    }
}
