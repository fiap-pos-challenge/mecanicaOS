import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { cents } from '../../../shared/util/money';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideCatalogData } from '../data/catalog.gateway';
import { FakeCatalog } from '../../../../testing/fake-api/catalog/catalog.seed';
import {
  amountText,
  formatMoneyControl,
  moneyValidator,
  numberRange,
  optionalText,
  quantityValidator,
} from './catalog-form';
import { CatalogPage } from './catalog-page';
import { mileageRangeValidator, normalizeSearch, PackageFormPage } from './package-form-page';
import { barcodeValidator, codeValidator, PartFormPage } from './part-form-page';
import { ServiceFormPage } from './service-form-page';

const form = messages.catalog.form;

describe('catalog form helpers', () => {
  it('validates money, ranges, quantities and codes', () => {
    expect(moneyValidator(new FormControl('1.234,50'))).toBeNull();
    expect(moneyValidator(new FormControl(''))).toBeNull();
    expect(moneyValidator(new FormControl('abc'))).toEqual({ money: true });
    expect(moneyValidator(new FormControl('-1,00'))).toEqual({ money: true });
    expect(numberRange(0, 10, true)(new FormControl(5))).toBeNull();
    expect(numberRange(0, 10, true)(new FormControl(5.5))).toEqual({ range: true });
    expect(numberRange(0, 10)(new FormControl(''))).toBeNull();
    expect(quantityValidator(new FormControl(0.001))).toBeNull();
    expect(quantityValidator(new FormControl(0))).toEqual({ quantity: true });
    expect(codeValidator(new FormControl('pec-1'))).toBeNull();
    expect(codeValidator(new FormControl('pec 1'))).toEqual({ code: true });
    expect(barcodeValidator(new FormControl(''))).toBeNull();
    expect(barcodeValidator(new FormControl('123'))).toEqual({ barcode: true });
    expect(
      mileageRangeValidator(
        new FormGroup({ minMileage: new FormControl(10), maxMileage: new FormControl(5) }),
      ),
    ).toEqual({ mileageRange: true });
    expect(normalizeSearch('  Óleo ')).toBe('oleo');
  });

  it('formats and parses amounts', () => {
    const control = new FormControl('1234,5', { nonNullable: true });
    formatMoneyControl(control);
    expect(control.value).toBe('1.234,50');
    const invalid = new FormControl('x', { nonNullable: true });
    formatMoneyControl(invalid);
    expect(invalid.value).toBe('x');
    expect(amountText(null)).toBe('');
    expect(amountText(cents(990))).toBe('9,90');
    expect(optionalText('  ')).toBeNull();
  });
});

describe('catalog forms', () => {
  async function setup<T>(
    component: Type<T>,
    pick: ((catalog: FakeCatalog) => string | undefined) | null,
    role: BackendRole = 'Admin',
  ) {
    let id: string | null = null;
    const view = await render(component, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideCatalogData()],
      configureTestBed: () => {
        signInWithFakeApi(role);
        id = pick ? (pick(TestBed.inject(FakeBackend).state.database.catalog) ?? 'missing') : null;
      },
    });
    view.fixture.componentRef.setInput('id', id);
    view.fixture.detectChanges();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    await screen.findByRole('heading', { level: 1 });
    return { view, id, navigate, user: userEvent.setup() };
  }

  const field = (label: string): HTMLInputElement =>
    screen.getByLabelText(new RegExp(`^\\s*${label.replace(/[()$]/g, '\\$&')}(\\s*\\*)?\\s*$`), {
      selector: 'input, select, textarea',
    });
  const catalog = (): FakeCatalog => TestBed.inject(FakeBackend).state.database.catalog;

  describe('PartFormPage', () => {
    const part = form.parts;

    it('creates a part with live margin and rejects a duplicated code', async () => {
      const { view, user, navigate } = await setup(PartFormPage, null);
      await expectNoAxeViolations(view.container);

      await user.type(field(part.code), 'pec-0101');
      expect(field(part.code)).toHaveValue('PEC-0101');
      await user.type(field(form.name), 'Correia dentada');
      await user.type(field(part.cost), '90');
      await user.type(field(part.unitPrice), '150');
      await user.tab();
      expect(field(part.unitPrice)).toHaveValue('150,00');
      expect(screen.getByTestId('part-margin')).toHaveTextContent('40%');
      await user.click(screen.getByRole('button', { name: form.save }));
      expect(await screen.findByTestId('catalog-form-error')).toHaveTextContent(
        messages.fake.duplicatePartCode,
      );
      expect(field(part.code)).toHaveAttribute('aria-invalid', 'true');

      await user.clear(field(part.code));
      await user.type(field(part.code), 'PEC-9100');
      await user.click(screen.getByRole('radio', { name: messages.catalog.partTypes.Supply }));
      await user.click(screen.getByRole('button', { name: form.save }));
      await vi.waitFor(() => expect(navigate).toHaveBeenCalled());
      const created = catalog().parts.find((item) => item.code === 'PEC-9100');
      expect(created).toMatchObject({ cost: 90, unitPrice: 150, type: 'Supply' });
    });

    it('warns and asks before saving below the cost', async () => {
      const { user, id } = await setup(
        PartFormPage,
        (data) => data.parts.find((item) => item.code === 'PEC-0412')?.id,
      );

      await user.clear(field(part.unitPrice));
      await user.type(field(part.unitPrice), '100');
      expect(screen.getByText(part.belowCost)).toBeInTheDocument();
      await user.click(screen.getByRole('button', { name: form.save }));
      const dialog = await screen.findByRole('dialog', { name: part.confirmBelowCostTitle });
      await user.click(within(dialog).getByRole('button', { name: messages.ui.dialog.cancel }));
      expect(catalog().parts.find((item) => item.id === id)?.unitPrice).toBe(189);

      await user.click(screen.getByRole('button', { name: form.save }));
      await user.click(
        within(await screen.findByRole('dialog')).getByRole('button', {
          name: part.confirmBelowCostAction,
        }),
      );
      await vi.waitFor(() =>
        expect(catalog().parts.find((item) => item.id === id)?.unitPrice).toBe(100),
      );
      expect(await screen.findByText(/R\$\s100,00/)).toBeInTheDocument();
    });

    it('shows field errors before calling the API', async () => {
      const { user } = await setup(PartFormPage, null);

      await user.clear(field(part.unit));
      await user.type(field(part.barcode), '123');
      await user.type(field(part.code), 'A B');
      await user.click(screen.getByRole('button', { name: form.save }));

      expect(screen.getByTestId('catalog-form-error')).toHaveTextContent(form.fixErrors);
      expect(screen.getByText(part.invalidBarcode)).toBeInTheDocument();
      expect(screen.getByText(part.invalidCode)).toBeInTheDocument();
      expect(screen.getAllByText(form.required).length).toBeGreaterThan(0);
    });

    it('is read-only without costs for the mechanic', async () => {
      await setup(
        PartFormPage,
        (data) => data.parts.find((item) => item.code === 'PEC-0412')?.id,
        'Mechanic',
      );

      expect(screen.getByText(form.readOnly)).toBeInTheDocument();
      expect(field(form.name)).toBeDisabled();
      expect(screen.queryByLabelText(new RegExp(part.cost.replace(/[()$]/g, '\\$&')))).toBeNull();
      expect(screen.queryByRole('button', { name: form.save })).toBeNull();
      expect(
        (await screen.findAllByText(messages.catalog.history.fields.UnitPrice)).length,
      ).toBeGreaterThan(0);
      expect(screen.queryByText(messages.catalog.history.fields.Cost)).toBeNull();
    });

    it('shows not found', async () => {
      await render(PartFormPage, {
        providers: [...provideTestApi('mock'), provideAuthData(), provideCatalogData()],
        inputs: { id: 'missing' },
        configureTestBed: () => {
          signInWithFakeApi('Admin');
        },
      });
      expect(await screen.findByText(form.notFound)).toBeInTheDocument();
    });
  });

  describe('ServiceFormPage', () => {
    const service = form.services;

    it('creates a service with usual parts', async () => {
      const { user, navigate } = await setup(ServiceFormPage, null);

      await user.type(field(form.name), 'Troca de correia');
      await user.type(field(service.basePrice), '350');
      await user.clear(field(service.minutes));
      await user.type(field(service.minutes), '120');
      await user.selectOptions(field(service.part), 'PEC-0115 · Filtro de óleo Polo TSI');
      await user.click(screen.getByRole('button', { name: service.addPart }));
      const quantity = screen.getByLabelText(service.quantityOf('Filtro de óleo Polo TSI'));
      await user.clear(quantity);
      await user.type(quantity, '2');
      await user.click(screen.getByRole('button', { name: form.save }));

      await vi.waitFor(() => expect(navigate).toHaveBeenCalled());
      const created = catalog().services.find((item) => item.name === 'Troca de correia');
      expect(created).toMatchObject({ basePrice: 350, estimatedMinutes: 120 });
      expect(created?.usualParts).toHaveLength(1);
      expect(created?.usualParts[0]?.quantity).toBe(2);
    });

    it('edits the price, removes a part and records the history', async () => {
      const { user, id } = await setup(
        ServiceFormPage,
        (data) => data.services.find((item) => item.name === 'Troca de óleo')?.id,
      );

      expect(screen.getAllByRole('listitem').length).toBeGreaterThan(1);
      await user.click(
        screen.getByRole('button', { name: service.removePart('Óleo 5W30 sintético') }),
      );
      await user.clear(field(service.basePrice));
      await user.type(field(service.basePrice), '75');
      await user.click(screen.getByRole('button', { name: form.save }));

      await vi.waitFor(() =>
        expect(catalog().services.find((item) => item.id === id)?.basePrice).toBe(75),
      );
      expect(catalog().services.find((item) => item.id === id)?.usualParts).toHaveLength(1);
      expect(await screen.findByText(/R\$\s75,00/)).toBeInTheDocument();
    });

    it('validates before saving and reports API errors', async () => {
      const { user } = await setup(ServiceFormPage, null);

      await user.clear(field(service.commission));
      await user.type(field(service.commission), '80');
      await user.click(screen.getByRole('button', { name: form.save }));
      expect(screen.getByText(form.invalidPercent(50))).toBeInTheDocument();

      await user.clear(field(service.commission));
      await user.type(field(service.commission), '10');
      await user.type(field(form.name), 'Serviço');
      await user.type(field(service.basePrice), '10');
      TestBed.inject(FakeBackend).setScenario('catalog', 'unprocessable');
      await user.click(screen.getByRole('button', { name: form.save }));
      expect(await screen.findByTestId('catalog-form-error')).toHaveTextContent(
        messages.errors.businessRule,
      );
    });
  });

  describe('PackageFormPage', () => {
    const pack = form.packages;

    it('builds a package and shows the items total and the savings', async () => {
      const { view, user, navigate } = await setup(PackageFormPage, null);
      await expectNoAxeViolations(view.container);

      await user.type(field(form.name), 'Freio completo');
      await user.type(field(pack.search), 'pastilha');
      await user.click(
        screen.getByRole('button', { name: pack.add('Pastilha de freio dianteira') }),
      );
      await user.type(field(pack.search), 'troca de pastilhas');
      await user.click(
        screen.getByRole('button', { name: pack.add('Troca de pastilhas e discos') }),
      );
      await user.type(field(pack.search), 'zzz');
      expect(screen.getByText(pack.noResults)).toBeInTheDocument();
      const quantity = screen.getByLabelText(pack.quantityOf('Pastilha de freio dianteira'));
      await user.clear(quantity);
      await user.type(quantity, '2');
      expect(screen.getByTestId('package-items-total')).toHaveTextContent('638,00');
      await user.type(field(pack.price), '600');
      expect(screen.getByTestId('package-savings')).toHaveTextContent('38,00');
      expect(screen.getByText(pack.savingsPercent('6%'))).toBeInTheDocument();
      await user.clear(field(pack.price));
      await user.type(field(pack.price), '700');
      expect(screen.getByText(pack.moreExpensive)).toBeInTheDocument();
      await user.type(field(pack.minMileage), '20000');
      await user.type(field(pack.maxMileage), '10000');
      await user.click(screen.getByRole('button', { name: form.save }));
      expect(screen.getByText(pack.invalidRange)).toBeInTheDocument();

      await user.clear(field(pack.maxMileage));
      await user.type(field(pack.maxMileage), '30000');
      await user.click(screen.getByRole('button', { name: form.save }));
      await vi.waitFor(() => expect(navigate).toHaveBeenCalled());
      expect(catalog().packages.find((item) => item.name === 'Freio completo')).toMatchObject({
        price: 700,
        suggestion: { minMileage: 20_000, maxMileage: 30_000 },
      });
    });

    it('requires items and edits an existing package', async () => {
      const { user, id } = await setup(
        PackageFormPage,
        (data) => data.packages.find((item) => item.name === 'Kit freio dianteiro')?.id,
      );

      expect(screen.getByTestId('package-savings')).toHaveTextContent('58,00');
      for (const name of [
        'Pastilha de freio dianteira',
        'Disco de freio dianteiro',
        'Troca de pastilhas e discos',
      ]) {
        await user.click(screen.getByRole('button', { name: pack.removeItem(name) }));
      }

      await user.click(screen.getByRole('button', { name: form.save }));
      expect(screen.getByText(pack.noItems)).toBeInTheDocument();
      await user.type(field(pack.search), 'disco');
      await user.click(screen.getByRole('button', { name: pack.add('Disco de freio dianteiro') }));
      await user.click(screen.getByRole('button', { name: form.save }));
      await vi.waitFor(() =>
        expect(catalog().packages.find((item) => item.id === id)?.items).toHaveLength(1),
      );
    });

    it('is read-only for the attendant', async () => {
      await setup(PackageFormPage, (data) => data.packages[0]?.id, 'Attendant');

      expect(screen.getByText(form.readOnly)).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: form.save })).toBeNull();
      expect(screen.queryByLabelText(new RegExp(pack.search))).toBeNull();
    });

    it('shows the load error and retries', async () => {
      const view = await render(PackageFormPage, {
        providers: [...provideTestApi('mock'), provideAuthData(), provideCatalogData()],
        inputs: { id: null },
        configureTestBed: () => {
          signInWithFakeApi('Admin');
          TestBed.inject(FakeBackend).setScenario('catalog', 'serverError');
        },
      });

      expect(await screen.findByText(form.loadError)).toBeInTheDocument();
      TestBed.inject(FakeBackend).setScenario('catalog', 'none');
      await userEvent.click(screen.getByRole('button', { name: form.retry }));
      expect(await screen.findByRole('heading', { level: 1, name: pack.newTitle })).toBeVisible();
      expect(view.container).toBeTruthy();
    });
  });

  it('renders the catalog tabs', async () => {
    await render(CatalogPage, {
      providers: [...provideTestApi('mock'), provideAuthData(), provideCatalogData()],
    });

    expect(screen.getByRole('navigation', { name: messages.catalog.tabs })).toBeVisible();
    expect(screen.getByRole('link', { name: messages.catalog.kinds.parts })).toHaveAttribute(
      'href',
      '/catalog/parts',
    );
  });
});
