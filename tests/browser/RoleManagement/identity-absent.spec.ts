import { expect, test } from '@playwright/test';

test('Identity-absent Core fails closed for navigation and direct role routes', async ({ page, baseURL }) => {
  test.skip(!baseURL || baseURL.includes('127.0.0.1:9'), 'Set at least one RoleManagement Studio URL.');

  await page.goto('/');
  await expect(page.locator('a[href="/security/roles"]')).toHaveCount(0);

  await page.goto('/security/roles');
  await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);
  await expect(page.getByText('Role administration is unavailable', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: /create role/i })).toHaveCount(0);
  await expect(page.getByRole('link', { name: /create role/i })).toHaveCount(0);
  await expect(page.getByRole('table')).toHaveCount(0);
});
