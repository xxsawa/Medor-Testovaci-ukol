import { defineConfig, devices } from '@playwright/test';

/** Headed runs by default; set HEADLESS=1 for CI or headless local. */
const headed = process.env.HEADLESS !== '1' && process.env.HEADLESS !== 'true';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? 'github' : [['list'], ['html', { open: 'never' }]],
  timeout: 60_000,
  expect: { timeout: 15_000 },
  use: {
    baseURL: 'http://localhost:5192',
    headless: !headed,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    ...devices['Desktop Chrome'],
  },
  projects: [{ name: 'chromium' }],
  webServer: {
    command: 'dotnet run --project ../Medor.Web/Medor.Web.csproj --urls http://localhost:5192',
    url: 'http://localhost:5192',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
