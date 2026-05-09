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
            }
            finally
            {
                // 关闭浏览器
                await _browser.CloseAsync();
            }
        }

        private async Task Demo1_OpenWebsiteAsync()
        {

            var result = await _browser.OpenAsync("https://example.com");
            
            if (result.Success)
            {
            }
            else
            {
            }

            await Task.Delay(1000);
        }

        private async Task Demo2_GetSnapshotAsync()
        {

            var snapshot = await _browser.SnapshotAsync(includeInteractive: true);

            if (snapshot.Success)
            {

                // 按类型统计
                var typeGroups = snapshot.Elements.GroupBy(e => e.Type).ToList();
                foreach (var group in typeGroups)
                {
                    foreach (var element in group.Take(3))
                    {
                    }
                    if (group.Count() > 3)
                    {
                    }
                }

                // 打印所有链接
                var links = snapshot.GetAllLinks();
                if (links.Count > 0)
                {
                    foreach (var link in links.Take(5))
                    {
                    }
                }
            }
            else
            {
            }
        }

        private async Task Demo3_InteractionAsync()
        {

            // 获取当前 URL
            var currentUrl = await _browser.GetUrlAsync();

            // 演示类型查找
            var snapshot = await _browser.SnapshotAsync();
            if (snapshot.Success && snapshot.Elements.Count > 0)
            {
                var firstElement = snapshot.Elements.First();

                // 演示按文本查找
                var elementByText = snapshot.FindByText(firstElement.Text);
                if (elementByText != null)
                {
                }

                // 演示按ref查找
                var elementByRef = snapshot.FindByRef(firstElement.Ref);
                if (elementByRef != null)
                {
                }
            }
        }

        private async Task Demo4_ScreenshotAsync()
        {

            var screenshotPath = "screenshot.png";
            var result = await _browser.ScreenshotAsync(screenshotPath);

            if (result.Success)
            {
            }
            else
            {
            }
        }
    }
}
