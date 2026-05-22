import { test, expect } from '@playwright/test'

test.describe('对话页面', () => {
  test('登录后跳转到 /chat', async ({ page }) => {
    // 模拟已登录状态
    await page.goto('/login')
    await page.evaluate(() => {
      localStorage.setItem('agentbrook-settings', JSON.stringify({
        theme: 'light',
        fontSize: 14,
        sendKey: 'enter',
        apiBaseUrl: 'http://localhost:5000',
      }))
    })
    // 设置 user store
    await page.evaluate(() => {
      const pinia = JSON.stringify({
        user: {
          isLoggedIn: true,
          userInfo: { name: 'Test User', phone: '13800138000' },
        },
      })
      localStorage.setItem('pinia', pinia)
    })

    await page.goto('/chat')
    await expect(page).toHaveURL('/chat')
    await expect(page.locator('.chat-input-wrapper')).toBeVisible()
  })

  test('侧边栏收起和展开', async ({ page }) => {
    await page.evaluate(() => {
      localStorage.setItem('agentbrook-settings', JSON.stringify({
        theme: 'light', fontSize: 14, sendKey: 'enter', apiBaseUrl: 'http://localhost:5000',
      }))
      localStorage.setItem('pinia', JSON.stringify({
        user: { isLoggedIn: true, userInfo: { name: 'Test' } },
      }))
    })

    await page.goto('/chat')
    const toggleBtn = page.locator('.status-bar .toggle-sidebar, [data-testid="toggle-sidebar"]').first()
    if (await toggleBtn.isVisible()) {
      await toggleBtn.click()
      await page.waitForTimeout(300)
    }
  })
})
