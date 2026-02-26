import { expect, test } from "@playwright/test";

test.describe("Smart add visual baseline", () => {
  test.beforeEach(async ({ page }) => {
    await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
    await page.route("https://fonts.gstatic.com/**", (route) => route.abort());
    await page.goto("/preview/smart-add");
  });

  test("normal state", async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    await expect(page).toHaveScreenshot("smart-add-normal.png", { fullPage: true });
  });

  test("focus state", async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.getByRole("button", { name: "Новый важнее" }).focus();
    await expect(page).toHaveScreenshot("smart-add-focus.png", { fullPage: true });
  });

  test("mobile layout", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page).toHaveScreenshot("smart-add-mobile.png", { fullPage: true });
  });
});
