import { test, expect } from '@playwright/test'

test.describe('登录流程', () => {
  test('未登录重定向到 /login', async ({ page }) => {
    await page.goto('/chat')
    await expect(page).toHaveURL(/\/login/)
  })

  test('登录页面正确渲染', async ({ page }) => {
    await page.goto('/login')
    await expect(page.locator('input[type="password"]')).toBeVisible()
  })

  test('空表单提交显示验证错误', async ({ page }) => {
    await page.goto('/login')
    await page.click('button:has-text("登录")')
    await expect(page.locator('.el-form-item__error')).toBeVisible()
  })
})
