import { expect, test } from "@playwright/test";

test.describe("DefaultDarkPinkNeon visual baseline", () => {
  test("login page baseline", async ({ page }) => {
    await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
    await page.route("https://fonts.gstatic.com/**", (route) => route.abort());

    await page.goto("/login");
    await page.setViewportSize({ width: 1366, height: 900 });

    await expect(page).toHaveScreenshot("login-default.png", { fullPage: true });
  });

  test("focus and interactive states", async ({ page }) => {
    await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
    await page.route("https://fonts.gstatic.com/**", (route) => route.abort());

    await page.goto("/login");
    await page.setViewportSize({ width: 1366, height: 900 });

    await page.locator("#email").focus();
    await page.locator("button:has-text('Login')").hover();

    await expect(page).toHaveScreenshot("login-focus-hover.png", { fullPage: true });
  });
});
