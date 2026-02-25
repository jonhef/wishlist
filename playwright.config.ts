import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  timeout: 45_000,
  fullyParallel: false,
  retries: 0,
  reporter: "list",
  expect: {
    toHaveScreenshot: {
      maxDiffPixelRatio: 0.01,
      animations: "disabled",
      caret: "hide"
    }
  },
  use: {
    baseURL: "http://127.0.0.1:4173",
    trace: "on-first-retry"
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] }
    }
  ],
  webServer: {
    command: "npm --workspace frontend run dev -- --host 127.0.0.1 --port 4173",
    url: "http://127.0.0.1:4173/login",
    reuseExistingServer: true,
    timeout: 90_000
  }
});
