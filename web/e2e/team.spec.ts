import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS, adminToken, createUser, E2E_PASSWORD } from './accounts';
import { latestMailTo } from './mail';
import { expectNoSeriousAxeViolations, loginAs, mainNavigation, signIn } from './support';

const list = messages.team.list;
const invite = messages.team.invite;
const member = messages.team.member;
const invitation = messages.auth.invitation;

async function openTeam(page: Page): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: messages.screens.team }).click();
  await expect(page.getByRole('heading', { level: 1, name: messages.screens.team })).toBeVisible();
}

function card(page: Page, name: string) {
  return page
    .getByRole('listitem')
    .filter({ has: page.getByRole('link', { name: list.open(name) }) });
}

async function invitationLink(email: string): Promise<string> {
  let link = '';
  await expect
    .poll(
      () => {
        const html = latestMailTo(email) ?? '';
        link = /href="([^"]+)"/.exec(html)?.[1]?.replaceAll('&amp;', '&') ?? '';
        return link;
      },
      { timeout: 20_000 },
    )
    .toContain('/invite#token=');
  return link;
}

test.describe('team against the real API', () => {
  test.describe.configure({ timeout: 120_000 });

  test('invites a mechanic who creates the own password and signs in', async ({
    page,
    browser,
  }, testInfo) => {
    const email = `convidado.${testInfo.project.name}.${Date.now()}@oficina.local`;
    const name = `Convidado ${testInfo.project.name}`;
    await loginAs(page, ACCOUNTS.admin);
    await openTeam(page);

    await page.getByRole('button', { name: list.invite }).click();
    const dialog = page.getByRole('dialog', { name: invite.title });
    await expect(dialog.getByText(invite.notice)).toBeVisible();
    await dialog.getByLabel(invite.name).fill(name);
    await dialog.getByLabel(invite.email).fill(email);
    await dialog.getByLabel(invite.role).selectOption('Mechanic');
    await dialog.getByRole('button', { name: invite.submit }).click();
    await expect(card(page, name).getByText(messages.team.statuses.InvitationPending)).toBeVisible({
      timeout: 20_000,
    });
    await expectNoSeriousAxeViolations(page);

    await page.getByRole('button', { name: list.invite }).click();
    const again = page.getByRole('dialog', { name: invite.title });
    await again.getByLabel(invite.name).fill(name);
    await again.getByLabel(invite.email).fill(email);
    await again.getByRole('button', { name: invite.submit }).click();
    await expect(again.getByText('Já existe um usuário cadastrado com este e-mail.')).toBeVisible();
    await again.getByRole('button', { name: invite.cancel }).click();

    const link = await invitationLink(email);
    const guest = await browser.newPage();
    await guest.goto(link.replace(/^https?:\/\/[^/]+/, ''));
    await expect(guest).toHaveURL(/\/invite$/);
    await expect(guest.getByTestId('invite-details')).toContainText('c•••@oficina.local');
    await guest.locator('input[autocomplete="new-password"]').first().fill('fraca');
    await guest.getByRole('button', { name: invitation.submit }).click();
    await expect(guest.getByText(invitation.weak)).toBeVisible();
    await guest.locator('input[autocomplete="new-password"]').first().fill(E2E_PASSWORD);
    await guest.locator('input[autocomplete="new-password"]').nth(1).fill(E2E_PASSWORD);
    await guest.getByRole('button', { name: invitation.submit }).click();
    await expect(guest.getByRole('heading', { name: invitation.doneTitle })).toBeVisible();

    await guest.getByRole('link', { name: invitation.goToLogin }).click();
    await signIn(guest, { email, password: E2E_PASSWORD, name, role: 'Mechanic' });
    await expect(guest).toHaveURL(/\/mechanic/, { timeout: 20_000 });
    await guest.close();

    await page.goto(link.replace(/^https?:\/\/[^/]+/, ''));
    await expect(page.getByTestId('invite-error')).toHaveText(invitation.used);
  });

  test('deactivates and reactivates a member and protects the own account', async ({
    page,
    request,
  }, testInfo) => {
    const token = await adminToken(request);
    const name = `Equipe ${testInfo.project.name} ${Date.now() % 100000}`;
    await createUser(request, token, {
      email: `equipe.${testInfo.project.name}.${Date.now()}@oficina.local`,
      password: E2E_PASSWORD,
      name,
      role: 'Attendant',
    });
    await loginAs(page, ACCOUNTS.admin);
    await openTeam(page);

    const admin = card(page, ACCOUNTS.admin.name);
    await expect(
      admin.getByRole('button', { name: list.deactivateOf(ACCOUNTS.admin.name) }),
    ).toBeDisabled();
    await expect(admin.getByText(list.selfReason)).toBeVisible();

    await card(page, name)
      .getByRole('button', { name: list.deactivateOf(name) })
      .click();
    await page.getByRole('dialog').getByRole('button', { name: list.deactivate }).click();
    await expect(card(page, name).getByText(messages.team.statuses.Inactive)).toBeVisible();
    await card(page, name)
      .getByRole('button', { name: list.reactivateOf(name) })
      .click();
    await page.getByRole('dialog').getByRole('button', { name: list.reactivate }).click();
    await expect(card(page, name).getByText(messages.team.statuses.Inactive)).toHaveCount(0);
  });

  test('saves the professional profile and shows the fixed permissions', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openTeam(page);
    await expect(page.getByText(messages.pending.production)).toHaveCount(0);
    await page.getByRole('radio', { name: messages.roles.Mechanic }).click();
    await page.getByRole('list', { name: list.caption }).getByRole('link').first().click();
    await expect(page.getByRole('heading', { level: 2, name: member.profile })).toBeVisible();
    await expect(page.getByText(messages.pending.production)).toHaveCount(0);
    await page.getByLabel(member.commission).fill('12');
    await page.getByRole('button', { name: member.saveProfile }).click();
    await expect(page.getByText(member.profileSaved)).toBeVisible();

    await openTeam(page);
    await page.getByRole('link', { name: list.permissions }).click();
    await expect(
      page.getByRole('heading', { level: 1, name: messages.team.permissions.title }),
    ).toBeVisible();
    await expect(page.getByText(messages.pending.roles)).toHaveCount(0);
    await expect(page.getByText(messages.team.permissions.readOnly)).toBeVisible();
  });
});
