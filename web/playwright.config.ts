import { defineConfig, devices } from '@playwright/test';

const port = 4300;
const isCi = Boolean(process.env['CI']);

export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.ts',
  globalTeardown: './e2e/global-teardown.ts',
  forbidOnly: isCi,
  retries: isCi ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: `http://localhost:${port}`,
    trace: 'retain-on-failure',
    serviceWorkers: 'block',
  },
  projects: [
    { name: 'desktop', testIgnore: /\.mobile\.spec\.ts$/, use: { ...devices['Desktop Chrome'] } },
    {
      name: 'mobile',
      testIgnore: /\.desktop\.spec\.ts$/,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 390, height: 844 },
        deviceScaleFactor: 3,
        isMobile: true,
        hasTouch: true,
      },
    },
  ],
  webServer: {
    command: 'node scripts/e2e-server.mjs',
    url: `http://localhost:${port}`,
    reuseExistingServer: !isCi,
    timeout: 900_000,
  },
});
