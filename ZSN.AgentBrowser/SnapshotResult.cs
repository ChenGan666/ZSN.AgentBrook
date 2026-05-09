using System;
using System.Collections.Generic;
using System.Linq;

namespace ZSN.AgentBrowser
{
    /// <summary>
    /// 快照结果
    /// </summary>
    public class SnapshotResult
    {
        public bool Success { get; set; }
        public List<PageElement> Elements { get; set; } = new List<PageElement>();
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// 按类型查找元素
        /// </summary>
        public List<PageElement> FindByType(string type)
        {
            return Elements.Where(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// 按文本查找元素
        /// </summary>
        public PageElement? FindByText(string text)
        {
            return Elements.FirstOrDefault(e => e.Text.Equals(text, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 按ref查找元素
        /// </summary>
        public PageElement? FindByRef(string refId)
        {
            if (!refId.StartsWith("@"))
                refId = "@" + refId;
            return Elements.FirstOrDefault(e => e.Ref.Equals(refId.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取所有链接
        /// </summary>
        public List<PageElement> GetAllLinks()
        {
            return FindByType("link");
        }

        /// <summary>
        /// 获取所有按钮
        /// </summary>
        public List<PageElement> GetAllButtons()
        {
            return FindByType("button");
        }

        /// <summary>
        /// 获取所有输入框
        /// </summary>
        public List<PageElement> GetAllInputs()
        {
            return FindByType("input");
        }
    }
}
