# ZSN Knowbase MCP Server 测试脚本
# 用于测试MCP服务器的各项功能

Write-Host "================================" -ForegroundColor Cyan
Write-Host "ZSN Knowbase MCP Server 测试工具" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5008"
$testsPassed = 0
$testsFailed = 0

# 测试函数
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Body = $null
    )
    
    Write-Host "测试: $Name" -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -ErrorAction Stop
        } else {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Body $jsonBody -ContentType "application/json" -ErrorAction Stop
        }
        
        Write-Host "✓ 测试通过" -ForegroundColor Green
        Write-Host "响应: $($response | ConvertTo-Json -Compress)" -ForegroundColor Gray
        Write-Host ""
        $script:testsPassed++
        return $true
    }
    catch {
        Write-Host "✗ 测试失败: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        $script:testsFailed++
        return $false
    }
}

# 1. 测试服务器是否运行
Write-Host "1. 测试服务器连接" -ForegroundColor Cyan
Write-Host "-----------------------------------" -ForegroundColor Cyan
Test-Endpoint -Name "服务器健康检查" -Url "$baseUrl/MCPTest"

# 2. 测试MCPTest工具
Write-Host "2. 测试MCP工具" -ForegroundColor Cyan
Write-Host "-----------------------------------" -ForegroundColor Cyan
Test-Endpoint -Name "MCPTest - 默认参数" -Url "$baseUrl/MCPTest"
Test-Endpoint -Name "MCPTest - 自定义参数" -Url "$baseUrl/MCPTest?message=Hello&number=999&flag=false"

# 3. 测试MCP端点
Write-Host "3. 测试MCP端点" -ForegroundColor Cyan
Write-Host "-----------------------------------" -ForegroundColor Cyan

# 列出工具
$mcpRequest = @{
    jsonrpc = "2.0"
    method = "tools/list"
    id = 1
}
Test-Endpoint -Name "列出所有MCP工具" -Url "$baseUrl/mcp" -Method "POST" -Body $mcpRequest

# 调用MCPTest工具
$mcpCallRequest = @{
    jsonrpc = "2.0"
    method = "tools/call"
    params = @{
        name = "MCPTest"
        arguments = @{
            message = "MCP测试"
            number = 456
            flag = $true
        }
    }
    id = 2
}
Test-Endpoint -Name "通过MCP调用MCPTest工具" -Url "$baseUrl/mcp" -Method "POST" -Body $mcpCallRequest

# 4. 测试Swagger
Write-Host "4. 测试Swagger文档" -ForegroundColor Cyan
Write-Host "-----------------------------------" -ForegroundColor Cyan
try {
    $swaggerUrl = "$baseUrl/swagger/index.html"
    $response = Invoke-WebRequest -Uri $swaggerUrl -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Swagger文档可访问" -ForegroundColor Green
        Write-Host "URL: $swaggerUrl" -ForegroundColor Gray
        Write-Host ""
        $testsPassed++
    }
}
catch {
    Write-Host "✗ Swagger文档不可访问: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    $testsFailed++
}

# 测试结果汇总
Write-Host "================================" -ForegroundColor Cyan
Write-Host "测试结果汇总" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "通过: $testsPassed" -ForegroundColor Green
Write-Host "失败: $testsFailed" -ForegroundColor Red
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "🎉 所有测试通过！MCP服务器运行正常。" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️ 部分测试失败，请检查服务器配置和日志。" -ForegroundColor Yellow
    exit 1
}
