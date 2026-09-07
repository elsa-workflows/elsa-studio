import AxeBuilder from '@axe-core/playwright';
import { randomUUID } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import type { Locator, Page, TestInfo } from '@playwright/test';
import { CoreApiSession, expect, openRoles, signIn, test, assertCleanRuntime } from './fixtures';

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

async function captureEvidence(page: Page, testInfo: TestInfo, label: string): Promise<void> {
  const configuredDirectory = process.env.ROLE_E2E_EVIDENCE_DIR?.trim();
  if (!configuredDirectory)
    return;

  const repositoryRoot = path.resolve(process.cwd(), '../../..');
  const evidenceDirectory = path.resolve(process.cwd(), configuredDirectory);
  if (evidenceDirectory !== repositoryRoot && !evidenceDirectory.startsWith(`${repositoryRoot}${path.sep}`))
    throw new Error('ROLE_E2E_EVIDENCE_DIR must resolve inside the Studio repository.');

  await mkdir(evidenceDirectory, { recursive: true });
  await page.screenshot({
    path: path.join(evidenceDirectory, `${testInfo.project.name}-${label}.png`),
    fullPage: true
  });
}

async function createRoleInEditor(
  page: Page,
  adminApi: CoreApiSession,
  registerRole: (id: string) => void,
  name: string
): Promise<string> {
  await page.goto('/security/roles/new');
  await expect(page.getByRole('heading', { name: 'New role' })).toBeVisible();
  await page.getByLabel('Role name').fill(name);

  await page.getByLabel('Filter permissions').fill('identity/roles');
  const viewGrant = page.getByLabel('identity/roles:view');
  await expect(viewGrant).toHaveCount(1);
  const categoryBulk = page.getByRole('button', { name: 'Select all', exact: true }).first();
  await expect(categoryBulk).toBeVisible();
  await categoryBulk.click();
  await expect(viewGrant).toBeChecked();
  await expect(page.getByText('Direct grant', { exact: true })).toHaveCount(0);

  const advancedTab = page.locator('[role="tab"]').filter({ hasText: 'Advanced grants' }).first();
  await advancedTab.click();
  await expect(page.getByText('No advanced grants.', { exact: false })).toBeVisible();
  await page.getByRole('textbox', { name: 'Advanced grant' }).fill('identity/roles:*');
  await page.getByRole('button', { name: 'Add advanced grant' }).click();
  await expect(page.getByText('identity/roles:*', { exact: true })).toBeVisible();
  await expect(page.getByText('Broad access', { exact: true })).toHaveCount(0);
  await expect(page.getByText(/\d+ resources today/)).toBeVisible();
  await expect(page.getByText('Future reach:', { exact: false })).toBeVisible();

  const createButton = page.getByRole('button', { name: 'Create role', exact: true });
  try {
    await expect(createButton).toBeEnabled();
  } catch {
    const pageText = (await page.locator('body').innerText()).replaceAll(name, '[role-name]');
    throw new Error(`Create role remained disabled. ${pageText.slice(0, 1200)}`);
  }
  await createButton.click();
  await expect(page).not.toHaveURL(/\/security\/roles\/new(?:$|[?#])/);
  await expect(page).toHaveURL(/\/security\/roles\/[^/]+$/);
  const id = roleIdFromUrl(page.url());
  registerRole(id);
  await expect.poll(async () => (await adminApi.findRole(id))?.name).toBe(name);
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

async function expectInsideViewport(locator: Locator, viewportWidth: number): Promise<void> {
  await expect(locator).toBeVisible();
  const bounds = await locator.boundingBox();
  expect(bounds).not.toBeNull();
  const description = await locator.evaluate(element => `${element.tagName.toLowerCase()}.${element.className}`);
  expect(bounds!.x, `${description} starts outside the viewport`).toBeGreaterThanOrEqual(0);
  expect(bounds!.x + bounds!.width, `${description} ends outside the viewport`).toBeLessThanOrEqual(viewportWidth);
}

async function expectPinnedNearViewportBottom(locator: Locator, viewportHeight: number): Promise<void> {
  await expect(locator).toBeVisible();
  const bounds = await locator.boundingBox();
  expect(bounds).not.toBeNull();
  expect(bounds!.y, 'Action bar starts above the viewport').toBeGreaterThanOrEqual(0);
  expect(viewportHeight - (bounds!.y + bounds!.height), 'Action bar is not pinned near the viewport bottom')
    .toBeGreaterThanOrEqual(0);
  expect(viewportHeight - (bounds!.y + bounds!.height), 'Action bar is not pinned near the viewport bottom')
    .toBeLessThanOrEqual(20);
}

async function expectContentNotClipped(locator: Locator): Promise<void> {
  const size = await locator.evaluate(element => ({
    clientWidth: element.clientWidth,
    scrollWidth: element.scrollWidth
  }));
  expect(size.scrollWidth, 'Element content exceeds its visible width').toBeLessThanOrEqual(size.clientWidth + 1);
}

async function expectNoBlockingAccessibilityViolations(page: Page): Promise<void> {
  const results = await new AxeBuilder({ page }).analyze();
  const blockingViolations = results.violations.filter(violation =>
    violation.impact === 'serious' || violation.impact === 'critical');
  const violationSummary = blockingViolations
    .map(violation => `${violation.id}: ${violation.nodes.map(node => node.target.join(' ')).join(', ')}`)
    .join('\n');
  expect(blockingViolations, violationSummary).toHaveLength(0);
}

test.describe('role management against a real Core host', () => {
  test('Elsa account sign-in submits with Enter', async ({ page, config, diagnostics }) => {
    await page.goto('/workflows/definitions');
    await expect(page).toHaveURL(/\/login\?returnUrl=%2Fworkflows%2Fdefinitions/i);
    await page.waitForLoadState('networkidle');
    await page.getByLabel('User name').fill(config.admin.username);
    await page.getByLabel('Password').fill(config.admin.password);
    await page.getByLabel('Password').press('Enter');

    await expect(page).toHaveURL(/\/workflows\/definitions(?:$|[?#])/);
    await assertCleanRuntime(diagnostics);
  });

  const externalAuthenticationRoutes = [
    {
      name: 'external identity links',
      path: '/security/external-authentication/identity-links',
      heading: 'External identity links',
      listSelector: '.identity-link-list'
    },
    {
      name: 'identity provider connections',
      path: '/security/external-authentication/connections',
      heading: 'Identity provider connections',
      listSelector: '.connection-list'
    }
  ];

  for (const route of externalAuthenticationRoutes) {
    test(`${route.name} loads with authenticated API clients`, async ({ page, config, diagnostics }) => {
      await signIn(page, config.admin);
      await page.goto(route.path);

      await expect(page.getByRole('heading', { level: 1, name: route.heading })).toBeVisible();
      await expect(page.locator(route.listSelector)).toBeVisible();
      await expect(page.getByText(/InnerHandler.*must be null/i)).toHaveCount(0);
      await assertCleanRuntime(diagnostics);
    });
  }

  test('dashboard header keeps only primary controls', async ({ page, config, diagnostics }) => {
    await signIn(page, config.admin);
    await page.goto('/');

    const header = page.locator('.dashboard-header');
    await expect(header.getByText('Dashboard', { exact: true })).toBeVisible();
    await expect(header.getByText('Selected backend', { exact: true })).toHaveCount(0);
    await expect(header.getByText('Not refreshed yet', { exact: true })).toHaveCount(0);
    await expect(header.getByText(/^Refreshed /)).toHaveCount(0);
    await expect(header.locator('.mud-chip')).toHaveCount(0);
    await expect(header.getByRole('button', { name: 'Refresh dashboard' })).toBeVisible();
    await expect(header.getByText('1h', { exact: true })).toBeVisible();
    await expect(header.getByText('24h', { exact: true })).toBeVisible();
    await expect(header.getByText('7d', { exact: true })).toBeVisible();

    const operationalHealth = page.locator('.dashboard-operational-health');
    const executionTrend = page.locator('.dashboard-chart-panel');
    await expect(operationalHealth).toBeVisible();
    await expect(executionTrend).toBeVisible();
    const operationalHealthBox = await operationalHealth.boundingBox();
    const executionTrendBox = await executionTrend.boundingBox();
    expect(operationalHealthBox).not.toBeNull();
    expect(executionTrendBox).not.toBeNull();
    expect(executionTrendBox!.y - (operationalHealthBox!.y + operationalHealthBox!.height)).toBeGreaterThanOrEqual(8);

    await expectNoBlockingAccessibilityViolations(page);
    await page.getByRole('button', { name: 'Use dark theme' }).click();
    await expect(page.getByRole('button', { name: 'Use light theme' })).toBeVisible();
    await expect(header.locator('.mud-chip')).toHaveCount(0);
    await assertCleanRuntime(diagnostics);
  });

  test('administrator completes create, save, reload, update, reload, and safe delete', async ({ page, config, adminApi, registerRole, diagnostics }) => {
    await openRoles(page, config.admin);
    const name = roleName('browser-role');
    const id = await createRoleInEditor(page, adminApi, registerRole, name);

    await page.reload();
    await expect(page.getByLabel('Role name')).toHaveValue(name);
    await expect(page.getByLabel('identity/roles:view')).toBeChecked();
    const advancedTab = page.locator('[role="tab"]').filter({ hasText: /Advanced grants/ }).first();
    await advancedTab.click();
    await expect(page.getByText('identity/roles:*', { exact: true })).toBeVisible();
    await expect(page.getByText(/Future reach:/)).toBeVisible();
    await page.getByRole('button', { name: 'Edit advanced grant identity/roles:*', exact: true }).click();
    await page.getByRole('textbox', { name: 'Grant expression for identity/roles:*', exact: true }).fill('identity/*:view');
    await page.getByRole('button', { name: 'Save advanced grant identity/roles:*', exact: true }).click();
    await expect(page.getByText('identity/*:view', { exact: true })).toBeVisible();

    const updatedName = `${name}-updated`;
    await page.getByLabel('Role name').fill(updatedName);
    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    await expect.poll(async () => (await adminApi.findRole(id))?.name).toBe(updatedName);

    await page.reload();
    await expect(page.getByLabel('Role name')).toHaveValue(updatedName);
    await expect(page.getByText(id, { exact: true })).toBeVisible();
    await page.locator('[role="tab"]').filter({ hasText: /Advanced grants/ }).first().click();
    await expect(page.getByText('identity/*:view', { exact: true })).toBeVisible();

    await page.getByRole('button', { name: 'Delete role', exact: true }).click();
    await expect(page.getByTestId('role-deletion-safe')).toBeVisible();
    await expect(page.getByText(/Existing access tokens are not changed/)).toBeVisible();
    await page.getByRole('button', { name: `Delete ${updatedName}`, exact: true }).click();
    await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);
    await expect.poll(async () => await adminApi.findRole(id)).toBeUndefined();

    // The role was deleted by the UI; cleanup accepts the expected 404.
    await assertCleanRuntime(diagnostics);
  });

  test('role list is keyboard accessible and switches layout at the responsive breakpoint', async ({ page, config, adminApi, registerRole, diagnostics }, testInfo) => {
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

    await expectNoBlockingAccessibilityViolations(page);
    await captureEvidence(page, testInfo, 'roles-list');
    await assertCleanRuntime(diagnostics);
  });

  test('role editor header follows the administration detail-page hierarchy', async ({ page, config, diagnostics }, testInfo) => {
    await signIn(page, config.admin);
    await page.goto('/security/roles/admin');

    const viewport = page.viewportSize();
    const viewportWidth = viewport?.width ?? 0;
    const viewportHeight = viewport?.height ?? 0;
    const backLink = page.locator('.role-editor-back');
    const summary = page.locator('.role-editor-summary');
    const actions = page.getByRole('group', { name: 'Role form actions' });

    await expect(page.getByRole('heading', { level: 1, name: 'Edit role — admin' })).toBeVisible();
    await expect(backLink).toHaveText('Roles');
    await expect(backLink).toHaveAttribute('href', /security\/roles$/);
    await expect(page.getByText('Role ID admin', { exact: true })).toBeVisible();
    await expectInsideViewport(summary, viewportWidth);
    await expectInsideViewport(actions, viewportWidth);
    await expectPinnedNearViewportBottom(actions, viewportHeight);
    await expect(actions).toHaveCSS('position', 'sticky');
    await expect(actions).toHaveCSS('bottom', '0px');
    await expect(actions.locator('.mud-button-filled, .mud-button-outlined')).toHaveCount(0);
    await expect(actions.getByRole('button', { name: 'Delete role', exact: true })).toHaveClass(/mud-button-text-error/);
    await expect(actions.getByRole('button', { name: 'Save changes', exact: true })).toHaveClass(/mud-button-text-primary/);
    for (const button of await actions.getByRole('button').all())
      await expectInsideViewport(button, viewportWidth);

    const workflowCategory = page.locator('.role-category-panel').filter({ hasText: /^Workflows/ });
    await workflowCategory.locator('.mud-expand-panel-header').click();
    await page.locator('.role-resource-row:visible').last().scrollIntoViewIfNeeded();
    await expectPinnedNearViewportBottom(actions, viewportHeight);

    await page.getByLabel('Filter permissions').fill('workflows/definitions');
    await expect(page.getByLabel('workflows/definitions:publish')).toBeVisible();
    await expect(page.getByText(/^Non-core:/)).toHaveCount(0);

    await expectNoBlockingAccessibilityViolations(page);
    await captureEvidence(page, testInfo, 'role-editor-header');
    await assertCleanRuntime(diagnostics);
  });

  test('exact permissions support global and category bulk selection', async ({ page, config, diagnostics }, testInfo) => {
    await signIn(page, config.admin);
    await page.goto('/security/roles/new');

    const viewportWidth = page.viewportSize()?.width ?? 0;
    const globalSelect = page.getByRole('button', { name: 'Select all exact permissions', exact: true });
    await expect(globalSelect).toBeVisible();
    await expectInsideViewport(globalSelect, viewportWidth);

    await globalSelect.click();
    await expect(page.getByRole('button', { name: 'Clear all exact permissions', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Clear all permissions in Workflows', exact: true })).toBeVisible();

    await page.getByRole('button', { name: 'Clear all exact permissions', exact: true }).click();
    await expect(globalSelect).toBeVisible();

    const workflowSelect = page.getByRole('button', { name: 'Select all permissions in Workflows', exact: true });
    await workflowSelect.click();
    await expect(page.getByRole('button', { name: 'Clear all permissions in Workflows', exact: true })).toBeVisible();
    await expect(globalSelect).toBeVisible();

    await expectNoBlockingAccessibilityViolations(page);
    await captureEvidence(page, testInfo, 'role-editor-bulk-permissions');
    await assertCleanRuntime(diagnostics);
  });

  test('mobile role editor and deletion remediation controls stay within the viewport', async ({ page, config, diagnostics }, testInfo) => {
    const viewportWidth = page.viewportSize()?.width ?? 0;
    test.skip(viewportWidth >= 600, 'This regression proof targets the phone layout.');

    await signIn(page, config.admin);
    await page.goto('/security/roles/new');

    const tabs = page.locator('.role-permissions-tabs [role="tab"]');
    await expect(tabs).toHaveCount(2);
    const advancedTab = tabs.filter({ hasText: 'Advanced grants' });
    await advancedTab.click();
    const exactTab = tabs.filter({ hasText: 'Exact permissions' });
    await expectInsideViewport(exactTab, viewportWidth);
    await expectInsideViewport(advancedTab, viewportWidth);
    await expectContentNotClipped(exactTab);
    await expectContentNotClipped(advancedTab);
    await captureEvidence(page, testInfo, 'role-editor-tabs');

    if (config.unresolvedRoleId) {
      await page.goto(`/security/roles/${encodeURIComponent(config.unresolvedRoleId)}`);
      const repairActions = page.locator('.role-unresolved-repair-actions').first();
      const replacement = repairActions.getByLabel('Replacement grant');
      await expectInsideViewport(repairActions, viewportWidth);
      await expectInsideViewport(replacement, viewportWidth);
      expect((await replacement.boundingBox())!.width).toBeGreaterThanOrEqual(180);
      await expectInsideViewport(repairActions.getByRole('button', { name: 'Replace', exact: true }), viewportWidth);
      await expectInsideViewport(repairActions.getByRole('button', { name: 'Remove', exact: true }), viewportWidth);
      await captureEvidence(page, testInfo, 'role-editor-repair');
    }

    if (config.remediableRoleId) {
      await page.goto(`/security/roles/${encodeURIComponent(config.remediableRoleId)}`);
      await page.getByRole('button', { name: 'Delete role', exact: true }).click();
      const dialog = page.getByTestId('role-deletion-remediation');
      await expect(dialog).toBeVisible();
      const referenceRows = dialog.locator('.role-deletion-reference-row');
      for (let index = 0; index < await referenceRows.count(); index++) {
        const row = referenceRows.nth(index);
        await expectInsideViewport(row, viewportWidth);
        await expectInsideViewport(row.getByRole('checkbox'), viewportWidth);
      }
      await captureEvidence(page, testInfo, 'role-deletion-remediation');
    }

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

  test('restricted actor sees read-only roles and Core rejects mutation', async ({ page, request, config, adminApi, diagnostics }) => {
    const fixture = config.restricted
      ? undefined
      : await adminApi.createRole(roleName('restricted-role'), [
          'identity/roles:view',
          'system/features:view',
          'user-tasks:view'
        ]);
    const generatedUser = fixture ? await adminApi.createUser(roleName('restricted-user'), [fixture.id]) : undefined;
    const restrictedActor = config.restricted ?? { username: generatedUser!.name, password: generatedUser!.password };
    const restrictedSession = await CoreApiSession.signIn(request, config.backendUrl, restrictedActor);
    try {
      await openRoles(page, restrictedActor);
      await expect(page.getByText('You can view roles, but you cannot create, edit, or delete them.')).toBeVisible();
      await expect(page.getByRole('button', { name: 'New role', exact: true })).toHaveCount(0);

      const navigation = page.locator('.studio-nav');
      await navigation.getByRole('button', { name: 'Toggle Identity & access', exact: true }).click();
      await expect(navigation.getByRole('link', { name: 'Roles', exact: true })).toBeVisible();
      await expect(navigation.getByRole('link', { name: 'Dashboard', exact: true })).toHaveCount(0);
      for (const href of [
        '/workflows/definitions',
        '/workflows/instances',
        '/alterations',
        '/alterations/instances',
        '/ai/weaver',
        '/diagnostics/opentelemetry',
        '/diagnostics/console',
        '/diagnostics/structured-logs',
        '/security/secrets'
      ])
        await expect(navigation.locator(`a[href$="${href}"]`)).toHaveCount(0);

      await page.goto('/security/roles/new');
      await expect(page.getByText('You can view roles, but your current sign-in cannot create a role.')).toBeVisible();
      await restrictedSession.expectForbiddenRoleCreation();
      await assertCleanRuntime(diagnostics);
    } finally {
      if (generatedUser)
        await adminApi.deleteUser(generatedUser.id);
      if (fixture)
        await adminApi.deleteRole(fixture.id);
    }
  });

  test('role changes reach an existing user only after token refresh', async ({ request, config, adminApi }) => {
    const assignedRole = await adminApi.createRole(roleName('refresh-role'), ['identity/roles:view']);
    const generatedUser = await adminApi.createUser(roleName('refresh-user'), [assignedRole.id]);
    let createdAfterRefresh: string | undefined;
    try {
      const originalSession = await CoreApiSession.signIn(request, config.backendUrl, {
        username: generatedUser.name,
        password: generatedUser.password
      });
      await adminApi.updateRole(assignedRole.id, assignedRole.name, ['identity/roles:view', 'identity/roles:create']);

      await originalSession.expectForbiddenRoleCreation();
      const refreshedSession = await originalSession.refresh();
      const created = await refreshedSession.createRole(roleName('refresh-proof'));
      createdAfterRefresh = created.id;
    } finally {
      if (createdAfterRefresh)
        await adminApi.deleteRole(createdAfterRefresh);
      await adminApi.deleteUser(generatedUser.id);
      await adminApi.deleteRole(assignedRole.id);
    }
  });

  test('recognized unverified catalog entries remain selectable', async ({ page, config, diagnostics }) => {
    test.skip(!config.requireUnverifiedCatalog,
      'Set ROLE_E2E_REQUIRE_UNVERIFIED_CATALOG=true only when the isolated Core catalog includes a verified:false descriptor.');
    await signIn(page, config.admin);
    await page.goto('/security/roles/new');
    await page.getByLabel('Filter permissions').fill('e2e/role-management/unverified');

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

  test('editable dependency remediation submits the inspected version and deletes after confirmation', async ({ page, config, adminApi, diagnostics }) => {
    test.skip(!config.remediableRoleId, 'Set ROLE_E2E_REMEDIABLE_ROLE_ID to an isolated role with database-owned editable references.');
    await openDeletionDialog(page, config.admin, config.remediableRoleId!);
    await expect(page.getByTestId('role-deletion-remediation')).toBeVisible();
    await confirmRemediation(page);
    await page.getByRole('button', { name: 'Apply remediation & delete', exact: true }).click();
    await expect(page).toHaveURL(/\/security\/roles(?:$|[?#])/);
    await expect.poll(async () => await adminApi.findRole(config.remediableRoleId!)).toBeUndefined();
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

  test('unresolved legacy grants stay verbatim and block save until repaired', async ({ page, config, adminApi, diagnostics }) => {
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

    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    await expect.poll(async () => (await adminApi.findRole(config.unresolvedRoleId!))?.permissions)
      .toContain('identity/roles:view');
    await page.reload();
    await expect(page.getByText(/Review and repair ·/)).toHaveCount(0);
    await expect(page.getByLabel('identity/roles:view')).toBeChecked();
    await assertCleanRuntime(diagnostics);
  });
});
