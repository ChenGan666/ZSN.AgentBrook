using System.Linq;
using System.Text;

namespace ZSN.AI.Entity
{
    public static class ZeroWidthCleaner
    {
        public static string Clean(string s)
        {
            if (s == null) return null;
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch == '\u200B' || ch == '\u200C' || ch == '\u200D' || ch == '\uFEFF')
                    continue;
                if (char.IsControl(ch))
                    continue;
                sb.Append(ch);
            }
            return sb.ToString().Trim();
        }

        public static string CleanNullable(string s)
        {
            return Clean(s);
        }
    }
}
