# ZSN.AgentBrowser - Agent-Browser REST API 服务

## 📋 项目概述

**ZSN.AgentBrowser** 是一个为AI代理设计的无头浏览器自动化REST API服务，基于 Agent-Browser CLI 工具构建。支持跨平台（Windows/Linux/macOS），提供完整的浏览器自动化功能，包括导航、表单填充、元素交互、截图等。

### 核心特性

- ✅ **Agent优先设计** - 紧凑文本输出，token消耗少
- ✅ **Ref引用系统** - 快照返回带ref的可访问性树，支持确定性元素选择
- ✅ **完整REST API** - 10个端点覆盖所有功能
- ✅ **Swagger文档** - 自动生成的API文档
- ✅ **CORS支持** - 支持跨域请求
- ✅ **异步操作** - 所有操作都是异步的
- ✅ **错误处理** - 统一的错误响应格式
- ✅ **日志记录** - 完整的操作日志

---

## 🚀 快速开始

### 前置要求

1. **.NET 10.0 SDK** 或更高版本
2. **Agent-Browser CLI** - 需要在PATH中或指定路径
3. **Chrome浏览器** - Agent-Browser依赖Chrome

### 安装 Agent-Browser

```bash
# Windows (使用 Scoop)
scoop install agent-browser

# Linux/macOS (使用 Homebrew)
brew install browserbase/tap/agent-browser

# 或从官方下载
# https://github.com/browserbase/agent-browser/releases
```

### 验证安装

```bash
agent-browser --version
```

### 启动服务

```bash
# 进入项目目录
cd ZSN.AgentBrowser

# 编译
dotnet build

# 运行
dotnet run

# 或发布后运行
dotnet publish -c Release
```

**默认服务地址:**
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### 快速验证

使用提供的测试脚本验证API是否正常工作：

```bash
# PowerShell (Windows)
.\test-api.ps1

# 或手动测试根路由
curl http://localhost:5000/

# 测试API端点
curl -X POST http://localhost:5000/api/browser/open \
  -H "Content-Type: application/json" \
  -d '{"url":"https://www.example.com"}'
```

**预期响应**:
```json
{
  "success": true,
  "message": "URL 打开成功",
  "data": {
    "success": true,
    "output": "Page loaded successfully",
    "error": "",
    "exitCode": 0
  }
}
```

---

## 📡 REST API 接口文档

### 📖 API 文档访问

启动服务后，可以通过以下方式访问API文档：

- **Swagger UI** (推荐): `http://localhost:5000/swagger`
- **OpenAPI JSON**: `http://localhost:5000/openapi/v1.json`
- **根路由**: `http://localhost:5000/` (显示服务信息和文档链接)

**Swagger UI 特性**:
- ✅ 可视化API文档
- ✅ 在线测试API端点
- ✅ 查看请求/响应示例
- ✅ 自动生成客户端代码

### 基础信息

- **基础URL**: `http://localhost:5000/api/browser` 或 `https://localhost:5001/api/browser`
- **请求格式**: JSON
- **响应格式**: JSON
- **认证**: 无（可根据需要添加）

### 响应格式

所有API响应遵循统一的格式：

```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    // 具体响应数据
  },
  "error": null
}
```

**响应字段说明:**
- **success** (boolean): 操作是否成功
- **message** (string): 操作消息
- **data** (object): 响应数据（成功时包含）
- **error** (string): 错误信息（失败时包含）

---

## 🔌 API 端点详解

### 1. 打开 URL

**端点**: `POST /api/browser/open`

**功能**: 打开指定的URL

**请求体**:
```json
{
  "url": "https://www.example.com"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "URL 打开成功",
  "data": {
    "success": true,
    "output": "Page loaded successfully",
    "error": "",
    "exitCode": 0
  }
}
```

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/open \
  -H "Content-Type: application/json" \
  -d '{"url":"https://www.example.com"}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.OpenAsync("https://www.example.com");
if (response.Success)
{
    Console.WriteLine("URL opened successfully");
}
```

---

### 2. 获取页面快照

**端点**: `POST /api/browser/snapshot`

**功能**: 获取页面快照，包含可访问性树和元素引用

**请求体**:
```json
{
  "includeInteractive": true
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "快照获取成功，找到 15 个元素",
  "data": {
    "success": true,
    "elements": [
      {
        "type": "button",
        "text": "Search",
        "ref": "e1"
      },
      {
        "type": "input",
        "text": "Search box",
        "ref": "e2"
      },
      {
        "type": "link",
        "text": "Home",
        "ref": "e3"
      }
    ],
    "error": ""
  }
}
```

**参数说明**:
- **includeInteractive** (boolean): 是否包含交互元素（按钮、输入框等）。默认为 true

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/snapshot \
  -H "Content-Type: application/json" \
  -d '{"includeInteractive":true}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.SnapshotAsync(includeInteractive: true);
if (response.Success)
{
    foreach (var element in response.Data.Elements)
    {
        Console.WriteLine($"Type: {element.Type}, Text: {element.Text}, Ref: {element.Ref}");
    }
}
```

---

### 3. 点击元素

**端点**: `POST /api/browser/click`

**功能**: 点击指定的页面元素

**请求体**:
```json
{
  "elementRef": "e1"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "点击成功",
  "data": {
    "success": true,
    "output": "Element clicked",
    "error": "",
    "exitCode": 0
  }
}
```

**参数说明**:
- **elementRef** (string): 元素引用ID，来自快照响应中的 `ref` 字段。可以带或不带 `@` 前缀

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/click \
  -H "Content-Type: application/json" \
  -d '{"elementRef":"e1"}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.ClickAsync("e1");
if (response.Success)
{
    Console.WriteLine("Element clicked successfully");
}
```

---

### 4. 输入文本

**端点**: `POST /api/browser/type`

**功能**: 向输入框输入文本

**请求体**:
```json
{
  "elementRef": "e2",
  "text": "search query"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "输入成功",
  "data": {
    "success": true,
    "output": "Text typed",
    "error": "",
    "exitCode": 0
  }
}
```

**参数说明**:
- **elementRef** (string): 输入框元素的引用ID
- **text** (string): 要输入的文本内容

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/type \
  -H "Content-Type: application/json" \
  -d '{"elementRef":"e2","text":"search query"}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.TypeAsync("e2", "search query");
if (response.Success)
{
    Console.WriteLine("Text typed successfully");
}
```

---

### 5. 按键操作

**端点**: `POST /api/browser/press`

**功能**: 按下键盘按键（如Enter、Tab、Escape等）

**请求体**:
```json
{
  "key": "Enter"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "按键成功",
  "data": {
    "success": true,
    "output": "Key pressed",
    "error": "",
    "exitCode": 0
  }
}
```

**常用按键**:
- `Enter` - 回车键
- `Tab` - 制表键
- `Escape` - 退出键
- `ArrowUp` - 上箭头
- `ArrowDown` - 下箭头
- `ArrowLeft` - 左箭头
- `ArrowRight` - 右箭头
- `Backspace` - 退格键
- `Delete` - 删除键
- `Space` - 空格键

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/press \
  -H "Content-Type: application/json" \
  -d '{"key":"Enter"}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.PressAsync("Enter");
if (response.Success)
{
    Console.WriteLine("Key pressed successfully");
}
```

---

### 6. 获取页面内容

**端点**: `GET /api/browser/content`

**功能**: 获取当前页面的HTML内容

**响应示例**:
```json
{
  "success": true,
  "message": "内容获取成功",
  "data": {
    "content": "<!DOCTYPE html><html><head>...</head><body>...</body></html>"
  }
}
```

**cURL示例**:
```bash
curl -X GET http://localhost:5000/api/browser/content
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.GetContentAsync();
if (response.Success)
{
    Console.WriteLine("Page content:");
    Console.WriteLine(response.Data.Content);
}
```

---

### 7. 获取当前 URL

**端点**: `GET /api/browser/url`

**功能**: 获取浏览器当前加载的URL

**响应示例**:
```json
{
  "success": true,
  "message": "URL 获取成功",
  "data": {
    "url": "https://www.example.com/page"
  }
}
```

**cURL示例**:
```bash
curl -X GET http://localhost:5000/api/browser/url
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.GetUrlAsync();
if (response.Success)
{
    Console.WriteLine($"Current URL: {response.Data.Url}");
}
```

---

### 8. 截图

**端点**: `POST /api/browser/screenshot`

**功能**: 获取当前页面的截图，并保存到指定位置

**请求体**:
```json
{
  "filePath": "screenshot"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "截图成功: screenshots/2026/03/27/14/screenshot_145930_123.png",
  "data": {
    "success": true,
    "screenshotUrl": "screenshots/2026/03/27/14/screenshot_145930_123.png",
    "filePath": "W:\\...\\screenshots\\2026\\03\\27\\14\\screenshot_145930_123.png",
    "error": ""
  }
}
```

**参数说明**:
- **filePath** (string): 文件名或路径。如果为空，将自动生成时间戳文件名

**目录结构**:
```
screenshots/
├── 2026/
│   └── 03/
│       └── 27/
│           └── 14/
│               ├── screenshot_145930_123.png
│               └── screenshot_150000_456.png
```

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/screenshot \
  -H "Content-Type: application/json" \
  -d '{"filePath":"screenshot"}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.ScreenshotAsync("my_screenshot");
if (response.Success)
{
    Console.WriteLine($"Screenshot saved to: {response.Data.ScreenshotUrl}");
    Console.WriteLine($"Full path: {response.Data.FilePath}");
}
```

---

### 9. 关闭浏览器

**端点**: `POST /api/browser/close`

**功能**: 关闭浏览器实例

**请求体**:
```json
{}
```

**响应示例**:
```json
{
  "success": true,
  "message": "浏览器已关闭",
  "data": {
    "success": true,
    "output": "Browser closed",
    "error": "",
    "exitCode": 0
  }
}
```

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/close \
  -H "Content-Type: application/json" \
  -d '{}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.CloseAsync();
if (response.Success)
{
    Console.WriteLine("Browser closed successfully");
}
```

---

### 10. 执行自定义命令

**端点**: `POST /api/browser/execute`

**功能**: 执行自定义的 agent-browser 命令

**请求体**:
```json
{
  "command": "snapshot -i"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "命令执行成功",
  "data": {
    "success": true,
    "output": "- button \"Search\" [ref=e1]\n- input \"Search box\" [ref=e2]",
    "error": "",
    "exitCode": 0
  }
}
```

**参数说明**:
- **command** (string): agent-browser CLI 命令（不包括 `agent-browser` 前缀）

**常用命令**:
```bash
# 打开URL
open https://example.com

# 获取快照（包含交互元素）
snapshot -i

# 点击元素
click @e1

# 输入文本
type @e2 "text"

# 按键
press Enter

# 获取页面内容
content

# 获取当前URL
url

# 截图
screenshot /path/to/screenshot.png

# 关闭浏览器
close
```

**cURL示例**:
```bash
curl -X POST http://localhost:5000/api/browser/execute \
  -H "Content-Type: application/json" \
  -d '{"command":"snapshot -i"}'
```

**C# 客户端示例**:
```csharp
var client = new AgentBrowserApiClient("https://localhost:5001");
var response = await client.ExecuteCommandAsync("snapshot -i");
if (response.Success)
{
    Console.WriteLine("Command output:");
    Console.WriteLine(response.Data.Output);
}
```

---

## 💻 客户端集成示例

### C# 客户端库使用

项目提供了 `AgentBrowserApiClient` 类，可以方便地调用API：

```csharp
using ZSN.AgentBrowser;
using ZSN.AgentBrowser.Models;

// 创建客户端
var client = new AgentBrowserApiClient("https://localhost:5001");

// 1. 打开URL
var openResponse = await client.OpenAsync("https://www.google.com");

// 2. 获取快照
var snapshotResponse = await client.SnapshotAsync(includeInteractive: true);
if (snapshotResponse.Success)
{
    foreach (var element in snapshotResponse.Data.Elements)
    {
        Console.WriteLine($"{element.Type}: {element.Text} [{element.Ref}]");
    }
}

// 3. 输入搜索词
var typeResponse = await client.TypeAsync("e2", "C# programming");

// 4. 按Enter键
var pressResponse = await client.PressAsync("Enter");

// 5. 等待结果加载后获取快照
await Task.Delay(2000);
var resultSnapshot = await client.SnapshotAsync();

// 6. 截图
var screenshotResponse = await client.ScreenshotAsync("search_result");
if (screenshotResponse.Success)
{
    Console.WriteLine($"Screenshot: {screenshotResponse.Data.ScreenshotUrl}");
}

// 7. 关闭浏览器
await client.CloseAsync();
```

### 完整工作流示例

```csharp
public class BrowserAutomationExample
{
    public async Task SearchAndScreenshot()
    {
        var client = new AgentBrowserApiClient("https://localhost:5001");
        
        try
        {
            // 打开Google
            Console.WriteLine("Opening Google...");
            await client.OpenAsync("https://www.google.com");
            
            // 获取页面快照
            Console.WriteLine("Getting page snapshot...");
            var snapshot = await client.SnapshotAsync();
            
            // 找到搜索框
            var searchBox = snapshot.Data.Elements.FirstOrDefault(e => 
                e.Type == "input" && e.Text.Contains("search"));
            
            if (searchBox != null)
            {
                // 输入搜索词
                Console.WriteLine($"Typing in search box ({searchBox.Ref})...");
                await client.TypeAsync(searchBox.Ref, "C# programming");
                
                // 按Enter搜索
                Console.WriteLine("Pressing Enter...");
                await client.PressAsync("Enter");
                
                // 等待搜索结果加载
                await Task.Delay(3000);
                
                // 截图搜索结果
                Console.WriteLine("Taking screenshot...");
                var screenshot = await client.ScreenshotAsync("google_search_result");
                
                if (screenshot.Success)
                {
                    Console.WriteLine($"Screenshot saved: {screenshot.Data.ScreenshotUrl}");
                }
            }
        }
        finally
        {
            // 关闭浏览器
            Console.WriteLine("Closing browser...");
            await client.CloseAsync();
        }
    }
}
```

---

## 🔧 配置说明

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      },
      "Https": {
        "Url": "https://0.0.0.0:5001"
      }
    }
  },
  "AgentBrowser": {
    "AgentBrowserPath": "agent-browser",
    "CommandTimeoutMs": 30000
  }
}
```

**配置项说明**:
- **AgentBrowserPath**: agent-browser CLI 的路径（默认从PATH中查找）
- **CommandTimeoutMs**: 命令执行超时时间（毫秒），默认30秒

### 自定义配置

如需修改配置，编辑 `appsettings.json` 文件：

```json
{
  "AgentBrowser": {
    "AgentBrowserPath": "C:\\Tools\\agent-browser.exe",
    "CommandTimeoutMs": 60000
  }
}
```

---

## 📊 项目结构

```
ZSN.AgentBrowser/
├── Program.cs                    # ASP.NET Core 启动配置
├── appsettings.json              # 应用配置
├── appsettings.Development.json  # 开发环境配置
├── Controllers/
│   └── BrowserController.cs      # API 控制器 (10 个 REST 端点)
├── Models/
│   ├── BrowserRequest.cs         # API 请求模型 (7 个请求类)
│   └── ApiResponse.cs            # API 响应模型 (7 个响应类)
├── AgentBrowserService.cs        # 核心服务类
├── CommandResult.cs              # 命令结果类
├── SnapshotResult.cs             # 快照结果类
├── PageElement.cs                # 页面元素类
├── ApiClient.cs                  # API 客户端库
├── ZSN.AgentBrowser.csproj       # 项目文件
├── README.md                      # 本文档
└── bin/obj/                       # 编译输出目录
```

---

## 🔑 关键类说明

### AgentBrowserService

核心服务类，负责与 agent-browser CLI 交互：

```csharp
public class AgentBrowserService
{
    // 打开URL
    public async Task<CommandResult> OpenAsync(string url)
    
    // 获取页面快照
    public async Task<SnapshotResult> SnapshotAsync(bool includeInteractive = true)
    
    // 点击元素
    public async Task<CommandResult> ClickAsync(string elementRef)
    
    // 输入文本
    public async Task<CommandResult> TypeAsync(string elementRef, string text)
    
    // 按键操作
    public async Task<CommandResult> PressAsync(string key)
    
    // 获取页面内容
    public async Task<string> GetContentAsync()
    
    // 获取当前URL
    public async Task<string> GetUrlAsync()
    
    // 截图
    public async Task<CommandResult> ScreenshotAsync(string filePath)
    
    // 保存截图到指定位置
    public async Task<(bool, string, string, string)> SaveScreenshotAsync(string screenshotFileName = "")
    
    // 关闭浏览器
    public async Task<CommandResult> CloseAsync()
    
    // 执行自定义命令
    public async Task<CommandResult> ExecuteCommandAsync(string command)
}
```

### AgentBrowserApiClient

HTTP客户端库，用于调用REST API：

```csharp
public class AgentBrowserApiClient
{
    public AgentBrowserApiClient(string baseUrl = "https://localhost:5001")
    
    public async Task<ApiResponse<CommandResponse>> OpenAsync(string url)
    public async Task<ApiResponse<SnapshotResponse>> SnapshotAsync(bool includeInteractive = true)
    public async Task<ApiResponse<CommandResponse>> ClickAsync(string elementRef)
    public async Task<ApiResponse<CommandResponse>> TypeAsync(string elementRef, string text)
    public async Task<ApiResponse<CommandResponse>> PressAsync(string key)
    public async Task<ApiResponse<ContentResponse>> GetContentAsync()
    public async Task<ApiResponse<UrlResponse>> GetUrlAsync()
    public async Task<ApiResponse<ScreenshotResponse>> ScreenshotAsync(string filePath = "")
    public async Task<ApiResponse<CommandResponse>> CloseAsync()
    public async Task<ApiResponse<CommandResponse>> ExecuteCommandAsync(string command)
}
```

---

## 🐛 故障排除

### 问题0: 访问API返回404

**错误信息**: `404 Not Found`

**原因分析**:
1. 路由配置不正确
2. 中间件顺序错误
3. 控制器未正确映射

**解决方案**:

确保 `Program.cs` 中的中间件顺序正确：

```csharp
var app = builder.Build();

// 配置中间件顺序（重要！）
app.UseRouting();           // 1. 路由
app.UseCors("AllowAll");    // 2. CORS
app.UseAuthorization();     // 3. 授权

// 映射端点
app.MapControllers();       // 4. 映射控制器
app.MapGet("/", () => new { message = "Agent-Browser API Service" }); // 5. 根路由

app.Run();
```

**验证步骤**:

1. 重新编译项目
```bash
dotnet clean
dotnet build
```

2. 启动服务
```bash
dotnet run
```

3. 测试根路由（应返回服务信息）
```bash
curl http://localhost:5000/
```

4. 测试API端点
```bash
curl -X POST http://localhost:5000/api/browser/open \
  -H "Content-Type: application/json" \
  -d '{"url":"https://www.example.com"}'
```

### 问题1: agent-browser 命令未找到

**错误信息**: `Failed to start agent-browser process`

**解决方案**:
1. 确保已安装 agent-browser
2. 检查 agent-browser 是否在 PATH 中
3. 在 appsettings.json 中指定完整路径

```json
{
  "AgentBrowser": {
    "AgentBrowserPath": "C:\\Program Files\\agent-browser\\agent-browser.exe"
  }
}
```

### 问题2: 命令执行超时

**错误信息**: `Command timeout`

**解决方案**:
1. 增加超时时间
2. 检查网络连接
3. 检查目标网站是否可访问

```json
{
  "AgentBrowser": {
    "CommandTimeoutMs": 60000
  }
}
```

### 问题3: HTTPS 证书错误

**错误信息**: `The SSL connection could not be established`

**解决方案**:
1. 在开发环境中使用 HTTP
2. 信任自签名证书
3. 禁用证书验证（仅开发环境）

```csharp
var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
var httpClient = new HttpClient(handler);
```

---

## 📚 相关资源

- **Agent-Browser 官方**: https://agent-browser.dev/
- **GitHub 仓库**: https://github.com/browserbase/agent-browser
- **.NET 10.0 文档**: https://learn.microsoft.com/en-us/dotnet/
- **ASP.NET Core 文档**: https://learn.microsoft.com/en-us/aspnet/core/

---

## 📝 更新日志

### v1.0.0 (2026-03-27)

- ✅ 完成 10 个 REST API 端点
- ✅ 实现 C# 客户端库
- ✅ 添加 Swagger/OpenAPI 文档
- ✅ 支持 CORS 跨域请求
- ✅ 完整的错误处理和日志记录
- ✅ 支持截图自动目录组织

---

## 📄 许可证

本项目遵循 MIT 许可证

---

## 👥 贡献

欢迎提交 Issue 和 Pull Request！

---

## 📞 联系方式

如有问题或建议，请联系项目维护者。

---

**最后更新**: 2026-03-27
