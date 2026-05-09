# Agent-Browser API 测试脚本
# 使用方法: .\test-api.ps1

$baseUrl = "http://localhost:5000"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Agent-Browser API 测试脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 测试1: 根路由
Write-Host "测试1: 根路由 (GET /)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/" -Method Get
    Write-Host "✓ 成功" -ForegroundColor Green
    Write-Host "响应: $($response | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "✗ 失败: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 测试2: 打开URL
Write-Host "测试2: 打开URL (POST /api/browser/open)" -ForegroundColor Yellow
try {
    $body = @{
        url = "https://www.example.com"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/browser/open" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body
    
    Write-Host "✓ 成功" -ForegroundColor Green
    Write-Host "响应: $($response | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "✗ 失败: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 测试3: 获取快照
Write-Host "测试3: 获取快照 (POST /api/browser/snapshot)" -ForegroundColor Yellow
try {
    $body = @{
        includeInteractive = $true
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/browser/snapshot" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body
    
    Write-Host "✓ 成功" -ForegroundColor Green
    Write-Host "找到 $($response.data.elements.Count) 个元素" -ForegroundColor Green
} catch {
    Write-Host "✗ 失败: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 测试4: 获取当前URL
Write-Host "测试4: 获取当前URL (GET /api/browser/url)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/browser/url" -Method Get
    Write-Host "✓ 成功" -ForegroundColor Green
    Write-Host "当前URL: $($response.data.url)" -ForegroundColor Green
} catch {
    Write-Host "✗ 失败: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 测试5: 关闭浏览器
Write-Host "测试5: 关闭浏览器 (POST /api/browser/close)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/browser/close" `
        -Method Post `
        -ContentType "application/json" `
        -Body "{}"
    
    Write-Host "✓ 成功" -ForegroundColor Green
    Write-Host "浏览器已关闭" -ForegroundColor Green
} catch {
    Write-Host "✗ 失败: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "测试完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
