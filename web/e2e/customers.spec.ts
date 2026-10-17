import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import { expectNoSeriousAxeViolations, loginAs, openInApp } from './support';

const list = messages.customers.list;
const detail = messages.customers.detail;
const form = messages.customers.form;
const vehicle = messages.customers.vehicle;

function cpfDigit(digits: readonly number[], weightStart: number): number {
  const sum = digits.reduce((total, digit, index) => total + digit * (weightStart - index), 0);
  return ((sum * 10) % 11) % 10;
}

export function randomCpf(): string {
  const base = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  if (new Set(base).size === 1) {
    base[0] = (base[0] ?? 0) === 9 ? 1 : (base[0] ?? 0) + 1;
  }

  const first = cpfDigit(base, 10);
  const second = cpfDigit([...base, first], 11);
  return [...base, first, second].join('');
}

function randomPlate(): string {
  const letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
  const letter = (): string => letters[Math.floor(Math.random() * letters.length)] ?? 'A';
  const digit = (): number => Math.floor(Math.random() * 10);
  return `${letter()}${letter()}${letter()}${digit()}${letter()}${digit()}${digit()}`;
}

function maskedCpf(cpf: string): string {
  return `•••.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-••`;
}

async function openCustomers(page: Page): Promise<void> {
  const menuButton = page.getByRole('button', { name: messages.shell.openMenu });
  if (await menuButton.isVisible()) {
    await menuButton.click();
  }

  await page
    .getByRole('navigation', { name: messages.shell.mainNavigation })
    .getByRole('link', { name: messages.screens.customers })
    .click();
  await expect(
    page.getByRole('heading', { level: 1, name: messages.screens.customers }),
  ).toBeVisible();
}

async function createCustomer(
  page: Page,
  name: string,
  cpf: string,
  options: { email?: string; whatsApp?: boolean } = {},
): Promise<void> {
  await page.getByRole('link', { name: list.newCustomer }).click();
  await expect(page.getByRole('heading', { level: 1, name: form.newTitle })).toBeVisible();
  await page.getByLabel(form.name).fill(name);
  await page.getByLabel(form.document).fill(cpf);
  await page.getByLabel(form.phone).fill('11987654321');
  if (options.email) {
    await page.getByLabel(form.email, { exact: true }).fill(options.email);
  }

  if (options.whatsApp) {
    await page.getByRole('checkbox', { name: form.consentWhatsApp }).check();
  }

  await page.getByRole('button', { name: form.save }).click();
  await expect(page.getByRole('heading', { level: 2, name })).toBeVisible({ timeout: 20_000 });
}

test.describe('customers against the real API', () => {
  test.describe.configure({ timeout: 90_000 });

  test.beforeEach(async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openCustomers(page);
  });

  test('creates, finds, reveals, edits and deletes a customer', async ({ page }) => {
    const cpf = randomCpf();
    const name = `Cliente E2E ${cpf.slice(0, 5)}`;

    await createCustomer(page, name, cpf, { email: `e2e.${cpf}@exemplo.com`, whatsApp: true });

    await expect(page.getByTestId('customer-document')).toHaveText(maskedCpf(cpf));
    await page.getByRole('button', { name: detail.reveal(detail.document) }).click();
    await expect(page.getByTestId('customer-document')).toHaveText(cpf);
    await expect(page.getByRole('button', { name: new RegExp(detail.sendWhatsApp) })).toBeEnabled();
    await expectNoSeriousAxeViolations(page);

    await page.getByRole('link', { name: detail.edit }).click();
    await expect(page.getByLabel(form.phone)).toHaveValue('(11) 98765-4321');
    await page.getByLabel(form.name).fill(`${name} Editado`);
    await page.getByRole('button', { name: form.save }).click();
    await expect(page.getByRole('heading', { level: 2, name: `${name} Editado` })).toBeVisible();

    await openInApp(page, '/customers');
    await page.getByRole('searchbox', { name: list.searchLabel }).fill(cpf);
    await expect(page.getByRole('link', { name: list.open(`${name} Editado`) })).toBeVisible();
    await page.getByRole('link', { name: list.open(`${name} Editado`) }).click();

    await page.getByRole('button', { name: new RegExp(detail.delete) }).click();
    await page.getByRole('dialog').getByRole('button', { name: detail.deleteConfirm }).click();
    await expect(page.getByText(detail.deleted)).toBeVisible();
    await page.getByRole('searchbox', { name: list.searchLabel }).fill(cpf);
    await expect(page.getByText(list.empty)).toBeVisible();
  });

  test('rejects an invalid document and a duplicated one', async ({ page }) => {
    const cpf = randomCpf();
    await createCustomer(page, 'Duplicado E2E', cpf);

    await openInApp(page, '/customers');
    await page.getByRole('link', { name: list.newCustomer }).click();
    await page.getByLabel(form.name).fill('Documento ruim');
    await page.getByLabel(form.document).fill('12345678900');
    await page.getByLabel(form.phone).fill('11987654321');
    await page.getByRole('button', { name: form.save }).click();
    await expect(page.getByText(form.invalidDocument)).toBeVisible();

    await page.getByLabel(form.document).fill(cpf);
    await page.getByRole('button', { name: form.save }).click();
    await expect(page.getByTestId('customer-form-error')).toContainText(
      'Já existe um cliente cadastrado com este CPF ou CNPJ.',
    );
  });

  test('disables messages without consent and explains why', async ({ page }) => {
    await createCustomer(page, 'Sem Consentimento E2E', randomCpf());

    const whatsApp = page.getByRole('button', { name: new RegExp(detail.sendWhatsApp) });
    await expect(whatsApp).toBeDisabled();
    await expect(page.getByText(detail.noWhatsAppConsent)).toBeVisible();
    await expect(page.getByText(detail.noEmailAddress)).toBeVisible();
  });

  test('registers and edits a vehicle on the real API, keeping summary and history pending', async ({
    page,
  }) => {
    await createCustomer(page, `Veículo E2E ${Date.now() % 100000}`, randomCpf());
    const vehicles = page.getByRole('region', { name: detail.vehicles });
    const plate = randomPlate();

    await expect(vehicles.getByText(messages.pending.vehicles)).toHaveCount(0);
    await expect(page.getByText(messages.pending.history)).toBeVisible();
    await expect(page.getByText(messages.pending.customerSummary).last()).toBeVisible();

    await page.getByRole('button', { name: detail.addVehicle }).click();
    const dialog = page.getByRole('dialog', { name: vehicle.newTitle });
    await dialog.getByLabel(vehicle.plate).fill(plate.toLowerCase());
    await dialog.getByLabel(vehicle.brand).fill('Chevrolet');
    await dialog.getByLabel(vehicle.model).fill('Onix');
    await dialog.getByLabel(vehicle.year).fill('2023');
    await dialog.getByLabel(vehicle.mileage).fill('15000');
    await dialog.getByRole('button', { name: vehicle.save }).click();
    await expect(dialog).toHaveCount(0);
    await expect(vehicles.getByText(vehicle.summary('Chevrolet', 'Onix', 2023))).toBeVisible();

    await vehicles.getByRole('button', { name: vehicle.edit(plate) }).click();
    const edit = page.getByRole('dialog', { name: vehicle.editTitle });
    await edit.getByLabel(vehicle.mileage).fill('1000');
    await edit.getByRole('button', { name: vehicle.save }).click();
    await expect(edit.getByText(messages.fake.mileageDecrease)).toBeVisible();
    await edit.getByLabel(vehicle.mileage).fill('16000');
    await edit.getByRole('button', { name: vehicle.save }).click();
    await expect(edit).toHaveCount(0);
    await expectNoSeriousAxeViolations(page);
  });
});
