import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

const surfaces = [
  { path: '/login?choose=true', name: 'login chooser' },
  { path: '/security/external-authentication', name: 'connection management' },
  { path: '/security/external-authentication/identity-links', name: 'identity-link management' }
];

for (const surface of surfaces) {
  test(`${surface.name} has no serious or critical axe violations`, async ({ page }) => {
    await page.goto(surface.path);

    const results = await new AxeBuilder({ page }).analyze();
    const blockingViolations = results.violations.filter(violation =>
      violation.impact === 'serious' || violation.impact === 'critical');

    expect(blockingViolations, JSON.stringify(blockingViolations, null, 2)).toEqual([]);
  });
}

test('login chooser is operable with a keyboard and exposes a visible focus indicator', async ({ page }) => {
  await page.goto('/login?choose=true');

  await page.keyboard.press('Tab');
  await expect(page.getByLabel('User name')).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByLabel('Password')).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Sign in', exact: true })).toBeFocused();
  await page.keyboard.press('Tab');

  const github = page.getByRole('button', { name: 'Sign in with GitHub' });
  await expect(github).toBeFocused();
  expect(await github.evaluate(element => getComputedStyle(element).outlineStyle)).not.toBe('none');

  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/authorize\?.*code_challenge_method=S256/);
});

test('login chooser exposes deterministic text-first screen-reader semantics', async ({ page }) => {
  await page.goto('/login?choose=true');

  const main = page.getByRole('main');
  await expect(main).toHaveAttribute('aria-labelledby', 'external-login-heading');
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toHaveId('external-login-heading');
  await expect(page.getByRole('status')).toContainText('enabled identity provider');
  await expect(page.getByLabel('User name')).toHaveAttribute('autocomplete', 'username');
  await expect(page.getByLabel('Password')).toHaveAttribute('autocomplete', 'current-password');

  const externalMethods = page.locator('button[data-external]');
  await expect(externalMethods).toHaveCount(3);
  expect(await externalMethods.evaluateAll(buttons => buttons.map(button => button.getAttribute('aria-label'))))
    .toEqual(['Sign in with GitHub', 'Sign in with Microsoft', 'Sign in with Contoso']);
  await expect(externalMethods.nth(0)).toContainText('Sign in with GitHub');
  await expect(externalMethods.nth(1)).toContainText('Sign in with Microsoft');
  await expect(externalMethods.nth(2)).toContainText('Sign in with Contoso');
});

test('login chooser accepts only same-origin assets and preserves a text fallback', async ({ page }) => {
  await page.goto('/login?choose=true');

  const assetUrls = await page.locator('[src]').evaluateAll((elements, origin) =>
    elements.map(element => new URL(element.getAttribute('src')!, document.baseURI))
      .filter(url => url.origin !== origin)
      .map(url => url.href), new URL(page.url()).origin);

  expect(assetUrls).toEqual([]);
  await expect(page.locator('img[src^="http://"], img[src^="https://"]')).toHaveCount(0);

  const fallbackMethod = page.getByRole('button', { name: 'Sign in with Contoso' });
  await expect(fallbackMethod).toContainText('Sign in with Contoso');
  await expect(fallbackMethod.locator('[aria-hidden="true"]')).toHaveText('identity provider');
});

test('management surfaces expose named landmarks, filters, tables, and row actions', async ({ page }) => {
  await page.goto('/security/external-authentication');
  await expect(page.getByRole('main')).toHaveAttribute('aria-labelledby', 'connections-heading');
  await expect(page.getByRole('heading', { level: 1, name: 'Identity Provider Connections' })).toBeVisible();
  await expect(page.getByLabel('Search connections')).toBeVisible();
  await expect(page.getByLabel('Source')).toBeVisible();
  await expect(page.getByRole('table', { name: 'Identity provider connections' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Actions' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Manage Contoso' })).toBeVisible();

  await page.goto('/security/external-authentication/identity-links');
  await expect(page.getByRole('main')).toHaveAttribute('aria-labelledby', 'identity-links-heading');
  await expect(page.getByRole('heading', { level: 1, name: 'External Identity Links' })).toBeVisible();
  await expect(page.getByLabel('Find Elsa user')).toBeVisible();
  await expect(page.getByLabel('Issuer namespace')).toBeVisible();
  await expect(page.getByRole('table', { name: 'External identity links' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Actions' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Unlink Ada Lovelace from Contoso' })).toBeVisible();
});
