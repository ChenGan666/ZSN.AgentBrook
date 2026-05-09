namespace ZSN.AgentBrowser
{
    /// <summary>
    /// 页面元素
    /// </summary>
    public class PageElement
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{Type}] {Text} (@{Ref})";
        }
    }
}
