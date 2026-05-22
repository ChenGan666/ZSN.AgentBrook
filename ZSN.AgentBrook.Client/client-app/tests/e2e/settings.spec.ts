import { test, expect } from '@playwright/test'

test.describe('设置页面', () => {
  test.beforeEach(async ({ page }) => {
    await page.evaluate(() => {
      localStorage.setItem('agentbrook-settings', JSON.stringify({
        theme: 'light', fontSize: 14, sendKey: 'enter', apiBaseUrl: 'http://localhost:5000',
      }))
      localStorage.setItem('pinia', JSON.stringify({
        user: { isLoggedIn: true, userInfo: { name: 'Test' } },
      }))
    })
  })

  test('设置页面加载正常', async ({ page }) => {
    await page.goto('/settings')
    await expect(page.locator('.el-tabs')).toBeVisible()
  })

  test('主题切换到暗色', async ({ page }) => {
    await page.goto('/settings')
    const appSettingsTab = page.locator('.el-tabs__item:has-text("应用")')
    if (await appSettingsTab.isVisible()) {
      await appSettingsTab.click()
    }
    const darkRadio = page.locator('.el-radio:has-text("暗色")')
    if (await darkRadio.isVisible()) {
      await darkRadio.click()
      await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
    }
  })
})
