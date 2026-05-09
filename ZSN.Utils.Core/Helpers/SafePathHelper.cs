using System;
using System.IO;

namespace ZSN.Utils.Core.Helpers
{
    public static class SafePathHelper
    {
        public static bool TrySafePath(string baseDir, string? path, out string absPath, out string? error)
        {
            absPath = string.Empty;
            error = null;

            path = path ?? string.Empty;

            if (path.IndexOf('\0') >= 0)
            {
                error = "Invalid character";
                return false;
            }

            if (path.Contains("~"))
            {
                error = "Invalid path segment";
                return false;
            }

            // 显式拦截 .. 片段，满足安全与需求
            if (path.Contains(".."))
            {
                error = "Path traversal is not allowed";
                return false;
            }

            var invalidChars = new[] { '*', '?', '|', '"', '<', '>' };
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                error = "Invalid character";
                return false;
            }

            if (Path.IsPathRooted(path))
            {
                error = "Rooted path is not allowed";
                return false;
            }

            var absBase = Path.GetFullPath(baseDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            var combined = Path.Combine(absBase, path);
            var full = Path.GetFullPath(combined);

            var rel = Path.GetRelativePath(absBase, full);
            if (Path.IsPathRooted(rel) || rel.StartsWith("..", StringComparison.Ordinal))
            {
                error = "Path traversal detected";
                return false;
            }

            if (!full.StartsWith(absBase, StringComparison.OrdinalIgnoreCase))
            {
                error = "Outside base directory";
                return false;
            }

            absPath = full;
            return true;
        }

        public static string SafePath(string baseDir, string? path)
        {
            if (!TrySafePath(baseDir, path, out var abs, out var err))
            {
                throw new ArgumentException(err ?? "Invalid path", nameof(path));
            }
            return abs;
        }
    }
}
