import AxeBuilder from '@axe-core/playwright';
import { randomUUID } from 'node:crypto';
import type { Page } from '@playwright/test';
import { expect, openRoles, signIn, test, assertCleanRuntime } from './fixtures';

const roleHostConfigured = ['SERVER', 'WASM'].some(prefix =>
  Boolean(process.env[`ROLE_E2E_${prefix}_STUDIO_URL`] && process.env[`ROLE_E2E_${prefix}_BACKEND_URL`]));
const actorsConfigured = Boolean(process.env.ROLE_E2E_ADMIN_USERNAME && process.env.ROLE_E2E_ADMIN_PASSWORD);

test.skip(!roleHostConfigured || !actorsConfigured,
  'Set local Studio/Core URLs and ROLE_E2E_ADMIN_USERNAME/ROLE_E2E_ADMIN_PASSWORD to run real-host role tests.');

function roleName(prefix: string): string {
  return `${prefix}-${randomUUID()}`;
}

function roleIdFromUrl(url: string): string {
  const segment = new URL(url).pathname.split('/').filter(Boolean).pop();
  if (!segment)
    throw new Error('The role editor did not navigate to a role ID.');
  return decodeURIComponent(segment);
}

function hasRoleCollectionEndpoint(response: { url(): string; request(): { method(): string } }, method: string): boolean {
  return response.request().method() === method && new URL(response.url()).pathname.endsWith('/identity/roles');
}

function hasRoleItemEndpoint(response: { url(): string; request(): { method(): string } }, method: string): boolean {
  return response.request().method() === method && /\/identity\/roles\/[^/]+$/.test(new URL(response.url()).pathname);
}

async function createRoleInEditor(
  page: Page,
  registerRole: (id: string) => void,
  name: string
): Promise<string> {
  await page.goto('/security/roles/new');
  await expect(page.getByRole('heading', { name: 'New role' })).toBeVisible();
  await page.getByLabel('Role name').fill(name);

  await page.getByLabel('Filter permissions').fill('identity/roles');
  const viewGrant = page.getByLabel('identity/roles:view');
  await expect(viewGrant).toHaveCount(1);
  const categoryBulk = page.getByRole('button', { name: 'Select all exact', exact: true }).first();
  await expect(categoryBulk).toBeVisible();
  await categoryBulk.click();
  await expect(viewGrant).toBeChecked();

  const advancedTab = page.locator('[role="tab"]').filter({ hasText: 'Advanced grants' }).first();
  await advancedTab.click();
  await expect(page.getByText('No advanced grants.', { exact: false })).toBeVisible();
  await page.getByLabel('Advanced grant').fill('identity/roles:*');
  await page.getByRole('button', { name: 'Add advanced grant' }).click();
  await expect(page.getByText('identity/roles:*', { exact: true })).toBeVisible();
  await expect(page.getByText(/\d+ resources today/)).toBeVisible();
  await expect(page.getByText('Future reach:', { exact: false })).toBeVisible();

  const createResponse = page.waitForResponse(response => hasRoleCollectionEndpoint(response, 'POST'));
  await page.getByRole('button', { name: 'Create role', exact: true }).click();
  const response = await createResponse;
  expect(response.status()).toBe(200);

  await expect(page).toHaveURL(/\/security\/roles\/[^/]+$/);
  const id = roleIdFromUrl(page.url());
  registerRole(id);
  return id;
}

async function openDeletionDialog(page: Page, actor: Parameters<typeof openRoles>[1], roleId: string): Promise<void> {
  await signIn(page, actor);
  await page.goto(`/security/roles/${encodeURIComponent(roleId)}`);
  await expect(page.getByRole('button', { name: 'Delete role' })).toBeVisible();
  await page.getByRole('button', { name: 'Delete role' }).click();
}

async function confirmRemediation(page: Page): Promise<void> {
  const dialog = page.locator('.delete-role-dialog').last();
  await expect(dialog).toBeVisible();
  const confirmations = dialog.getByRole('checkbox');
  for (let index = 0; index < await confirmations.count(); index++) {
    const checkbox = confirmations.nth(index);
    if (await checkbox.isVisible() && !(await checkbox.isChecked()))
      await checkbox.check();
  }
}

test.describe('role management against a real Core host', () => {
  test('administrator completes create, save, reload, update, reload, and safe delete', async ({ page, config, registerRole, diagnostics }) => {
    await openRoles(page, config.admin);
    const name = roleName('browser-role');
    const id = await createRoleInEditor(page, registerRole, name);

    await page.reload();
    await expect(page.getByLabel('Role name')).toHaveValue(name);
    await expect(page.getByLabel('identity/roles:view')).toBeChecked();
    const advancedTab = page.locator('[role="tab"]').filter({ hasText: /Advanced grants/ }).first();
    await advancedTab.click();
    await expect(page.getByText('identity/roles:*', { exact: true })).toBeVisible();
    await expect(page.getByText(/Future reach:/)).toBeVisible();

    const updatedName = `${name}-updated`;
    await page.getByLabel('Role name').fill(updatedName);
    const updateResponse = page.waitForResponse(response => hasRoleItemEndpoint(response, 'PUT'));
    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    expect((await updateResponse).status()).toBe(200);

    await page.reload();
    await expect(page.getByLabel('Role name')).toHaveValue(updatedName);
    await expect(page.getByText(id, { exact: true })).toBeVisible();

    await page.getByRole('button', { name: 'Delete role', exact: true }).click();
    await expect(page.getByTestId('role-deletion-safe')).toBeVisible();
    await expect(page.getByText(/Existing access tokens are not changed/)).toBeVisible();
    const deleteResponse = page.waitForResponse(response =>
      response.request().method() === 'DELETE' && new URL(response.url()).pathname.endsWith(`/identity/roles/${encodeURIComponent(id)}`));
    await page.getByRole('button', { name: `Delete ${updatedName}`, exact: true }).click();
    expect((await deleteResponse).status()).toBe(204);
    await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);

    // The role was deleted by the UI; cleanup accepts the expected 404.
    await assertCleanRuntime(diagnostics);
  });

  test('role list is keyboard accessible and switches layout at the responsive breakpoint', async ({ page, config, adminApi, registerRole, diagnostics }) => {
    const fixture = await adminApi.createRole(roleName('responsive-role'), ['identity/roles:view']);
    registerRole(fixture.id);
    await openRoles(page, config.admin);
    const width = page.viewportSize()?.width ?? 0;

    const search = page.getByLabel('Search roles by name, ID, or permission');
    await expect(search).toBeVisible();
    await expect(page.getByRole('status')).toContainText(/all loaded|matching search/);

    if (width < 960) {
      await expect(page.locator('.roles-mobile-list')).toBeVisible();
      await expect(page.locator('.roles-desktop-list')).toBeHidden();
    } else {
      await expect(page.locator('.roles-desktop-list')).toBeVisible();
      await expect(page.locator('.roles-mobile-list')).toBeHidden();
    }

    await search.focus();
    await expect(search).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.locator(':focus')).toBeVisible();

    const results = await new AxeBuilder({ page }).analyze();
    const blockingViolations = results.violations.filter(violation =>
      violation.impact === 'serious' || violation.impact === 'critical');
    expect(blockingViolations.length).toBe(0);
    await assertCleanRuntime(diagnostics);
  });

  test('role search reports an explicit filtered-empty state', async ({ page, config, adminApi, registerRole, diagnostics }) => {
    const fixture = await adminApi.createRole(roleName('filtered-role'), ['identity/roles:view']);
    registerRole(fixture.id);
    await openRoles(page, config.admin);

    const search = page.getByLabel('Search roles by name, ID, or permission');
    await search.fill(`no-match-${randomUUID()}`);
    await expect(page.getByRole('status')).toContainText('0 roles · matching search');
    await expect(page.getByRole('heading', { name: 'No matching roles' })).toBeVisible();
    await expect(page.getByText('Try a different name, ID, or permission.')).toBeVisible();
    await assertCleanRuntime(diagnostics);
  });

  test('restricted actor sees read-only roles and Core rejects mutation', async ({ page, config, adminApi, registerRole, restrictedApi, diagnostics }) => {
    test.skip(!config.restricted, 'Set ROLE_E2E_RESTRICTED_USERNAME/PASSWORD for restricted-actor proof.');
    const fixture = await adminApi.createRole(roleName('restricted-role'), ['identity/roles:view']);
    registerRole(fixture.id);
    await openRoles(page, config.restricted!);
    await expect(page.getByText('You can view roles, but you cannot create, edit, or delete them.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'New role', exact: true })).toHaveCount(0);
    await page.goto('/security/roles/new');
    await expect(page.getByText('You can view roles, but your current sign-in cannot create a role.')).toBeVisible();
    await restrictedApi!.expectForbiddenRoleCreation();
    await assertCleanRuntime(diagnostics);
  });

  test('recognized unverified catalog entries remain selectable', async ({ page, config, diagnostics }) => {
    test.skip(!config.requireUnverifiedCatalog,
      'Set ROLE_E2E_REQUIRE_UNVERIFIED_CATALOG=true only when the isolated Core catalog includes a verified:false descriptor.');
    await signIn(page, config.admin);
    await page.goto('/security/roles/new');

    const marker = page.getByText('Unverified · verified:false', { exact: true }).first();
    await expect(marker).toBeVisible();
    const row = marker.locator('xpath=ancestor::div[contains(@class,"role-resource-row")]');
    const grant = row.getByRole('checkbox').first();
    await expect(grant).toBeEnabled();
    await grant.check();
    await expect(grant).toBeChecked();
    await assertCleanRuntime(diagnostics);
  });
});

test.describe('optional real-host deletion dependency outcomes', () => {
  test('configuration-owned dependency is visibly blocked without a mutation action', async ({ page, config, diagnostics }) => {
    test.skip(!config.blockedRoleId, 'Set ROLE_E2E_BLOCKED_ROLE_ID to an isolated role referenced by configuration.');
    await openDeletionDialog(page, config.admin, config.blockedRoleId!);
    await expect(page.getByTestId('role-deletion-blocked')).toBeVisible();
    await expect(page.getByText('cannot be changed from Studio')).toBeVisible();
    await expect(page.getByRole('button', { name: /Apply remediation & delete/ })).toHaveCount(0);
    await assertCleanRuntime(diagnostics);
  });

  test('editable dependency remediation submits the inspected version and deletes after confirmation', async ({ page, config, diagnostics }) => {
    test.skip(!config.remediableRoleId, 'Set ROLE_E2E_REMEDIABLE_ROLE_ID to an isolated role with database-owned editable references.');
    await openDeletionDialog(page, config.admin, config.remediableRoleId!);
    await expect(page.getByTestId('role-deletion-remediation')).toBeVisible();
    await confirmRemediation(page);
    const operation = page.waitForResponse(response =>
      response.request().method() === 'POST' && new URL(response.url()).pathname.includes('/remove-from-jit-policies-and-delete'));
    await page.getByRole('button', { name: 'Apply remediation & delete', exact: true }).click();
    expect((await operation).status()).toBe(204);
    await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);
    await assertCleanRuntime(diagnostics);
  });

  test('dependency conflict clears confirmation and presents refreshed impact', async ({ page, config, adminApi, diagnostics }) => {
    test.skip(!config.conflictRoleId || !config.conflictTriggerUrl,
      'Set ROLE_E2E_CONFLICT_ROLE_ID and a local ROLE_E2E_CONFLICT_TRIGGER_URL fixture hook.');
    await openDeletionDialog(page, config.admin, config.conflictRoleId!);
    await expect(page.getByTestId('role-deletion-remediation')).toBeVisible();
    await confirmRemediation(page);
    await adminApi.triggerLocalFixture(config.conflictTriggerUrl!, config.conflictRoleId!);
    await page.getByRole('button', { name: 'Apply remediation & delete', exact: true }).click();
    await expect(page.getByTestId('role-deletion-conflict')).toBeVisible();
    await expect(page.getByText(/previous confirmation was cleared/)).toBeVisible();
    await assertCleanRuntime(diagnostics);
  });

  test('incomplete remediation retains the role and reports changed and remaining owners', async ({ page, config, adminApi, diagnostics }) => {
    test.skip(!config.incompleteRoleId || !config.incompleteTriggerUrl,
      'Set ROLE_E2E_INCOMPLETE_ROLE_ID and a local ROLE_E2E_INCOMPLETE_TRIGGER_URL fixture hook.');
    await openDeletionDialog(page, config.admin, config.incompleteRoleId!);
    await expect(page.getByTestId('role-deletion-remediation')).toBeVisible();
    await confirmRemediation(page);
    await adminApi.triggerLocalFixture(config.incompleteTriggerUrl!, config.incompleteRoleId!);
    await page.getByRole('button', { name: 'Apply remediation & delete', exact: true }).click();
    await expect(page.getByTestId('role-deletion-incomplete')).toBeVisible();
    await expect(page.getByText('Role retained')).toBeVisible();
    await expect(page.getByText(/Changed:/)).toBeVisible();
    await expect(page.getByText(/Remaining:/)).toBeVisible();
    await assertCleanRuntime(diagnostics);
  });

  test('unresolved legacy grants stay verbatim and block save until repaired', async ({ page, config, diagnostics }) => {
    test.skip(!config.unresolvedRoleId,
      'Set ROLE_E2E_UNRESOLVED_ROLE_ID to an isolated database-seeded role containing a legacy grant. Core rejects unknown grants on create/update.');
    await openRoles(page, config.admin);
    await page.goto(`/security/roles/${encodeURIComponent(config.unresolvedRoleId!)}`);

    const repair = page.getByText(/Review and repair ·/).locator('..');
    await expect(repair).toBeVisible();
    await expect(page.getByText('Unresolved legacy grant · verified:false')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Save changes', exact: true })).toBeDisabled();

    const replacement = page.getByLabel('Replacement grant').first();
    await replacement.fill('identity/roles:view');
    await page.getByRole('button', { name: 'Replace', exact: true }).first().click();
    await expect(page.getByText(/Review and repair ·/)).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Save changes', exact: true })).toBeEnabled();

    const updateResponse = page.waitForResponse(response => hasRoleItemEndpoint(response, 'PUT'));
    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    expect((await updateResponse).status()).toBe(200);
    await page.reload();
    await expect(page.getByText(/Review and repair ·/)).toHaveCount(0);
    await expect(page.getByLabel('identity/roles:view')).toBeChecked();
    await assertCleanRuntime(diagnostics);
  });
});
