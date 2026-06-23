using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZSN.AgentBrowser
{
    public class AgentBrowserDemo
    {
        private readonly AgentBrowserService _browser;

        public AgentBrowserDemo()
        {
            _browser = new AgentBrowserService();
        }

        public async Task RunAsync()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Agent-Browser C# 测试 Demo");
            Console.WriteLine("========================================\n");

            try
            {
                // Demo 1: 打开网页
                await Demo1_OpenWebsiteAsync();

                // Demo 2: 获取页面快照
                await Demo2_GetSnapshotAsync();

                // Demo 3: 互动操作
                await Demo3_InteractionAsync();

                // Demo 4: 截图
                await Demo4_ScreenshotAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n错误: {ex.Message}");
            }
            finally
            {
                // 关闭浏览器
                await _browser.CloseAsync();
                Console.WriteLine("\n测试完成");
            }
        }

        private async Task Demo1_OpenWebsiteAsync()
        {
            Console.WriteLine("\n[Demo 1] 打开网页");
            Console.WriteLine("-----------------------------------------");

            var result = await _browser.OpenAsync("https://example.com");
            
            if (result.Success)
            {
                Console.WriteLine("✓ 成功打开 https://example.com");
            }
            else
            {
                Console.WriteLine($"✗ 打开失败: {result.Error}");
            }

            await Task.Delay(1000);
        }

        private async Task Demo2_GetSnapshotAsync()
        {
            Console.WriteLine("\n[Demo 2] 获取页面快照");
            Console.WriteLine("-----------------------------------------");

            var snapshot = await _browser.SnapshotAsync(includeInteractive: true);

            if (snapshot.Success)
            {
                Console.WriteLine($"✓ 成功获取快照，找到 {snapshot.Elements.Count} 个元素");
                Console.WriteLine("\n页面元素列表:");

                // 按类型统计
                var typeGroups = snapshot.Elements.GroupBy(e => e.Type).ToList();
                foreach (var group in typeGroups)
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()} 个");
                    foreach (var element in group.Take(3))
                    {
                        Console.WriteLine($"    - {element.Text} [@{element.Ref}]");
                    }
                    if (group.Count() > 3)
                    {
                        Console.WriteLine($"    ... 还有 {group.Count() - 3} 个");
                    }
                }

                // 打印所有链接
                var links = snapshot.GetAllLinks();
                if (links.Count > 0)
                {
                    Console.WriteLine($"\n找到 {links.Count} 个链接:");
                    foreach (var link in links.Take(5))
                    {
                        Console.WriteLine($"  - {link.Text} [@{link.Ref}]");
                    }
                }
            }
            else
            {
                Console.WriteLine($"✗ 获取快照失败: {snapshot.Error}");
            }
        }

        private async Task Demo3_InteractionAsync()
        {
            Console.WriteLine("\n[Demo 3] 互动操作演示");
            Console.WriteLine("-----------------------------------------");

            // 获取当前 URL
            var currentUrl = await _browser.GetUrlAsync();
            Console.WriteLine($"✓ 当前 URL: {currentUrl}");

            // 演示类型查找
            var snapshot = await _browser.SnapshotAsync();
            if (snapshot.Success && snapshot.Elements.Count > 0)
            {
                var firstElement = snapshot.Elements.First();
                Console.WriteLine($"\n✓ 找到第一个元素: {firstElement}");

                // 演示按文本查找
                var elementByText = snapshot.FindByText(firstElement.Text);
                if (elementByText != null)
                {
                    Console.WriteLine($"✓ 按文本查找成功: {elementByText}");
                }

                // 演示按ref查找
                var elementByRef = snapshot.FindByRef(firstElement.Ref);
                if (elementByRef != null)
                {
                    Console.WriteLine($"✓ 按ref查找成功: {elementByRef}");
                }
            }
        }

        private async Task Demo4_ScreenshotAsync()
        {
            Console.WriteLine("\n[Demo 4] 截图演示");
            Console.WriteLine("-----------------------------------------");

            var screenshotPath = "screenshot.png";
            var result = await _browser.ScreenshotAsync(screenshotPath);

            if (result.Success)
            {
                Console.WriteLine($"✓ 截图成功: {screenshotPath}");
            }
            else
            {
                Console.WriteLine($"✗ 截图失败: {result.Error}");
            }
        }
    }
}
