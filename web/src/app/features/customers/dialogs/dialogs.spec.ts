import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { FormControl } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { firstValueFrom, lastValueFrom, of } from 'rxjs';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UI_DIALOG_TITLE_ID } from '../../../shared/ui/dialog/dialog';
import { UiConfirmDialog } from '../../../shared/ui/dialog/confirm-dialog';
import { provideAuthData } from '../../auth/data/auth.providers';
import { Job } from '../data/customer-jobs.gateway';
import { provideCustomersData } from '../data/customers.providers';
import { downloadText, pollJob } from '../data/job-polling';
import {
  ImportCustomersDialog,
  IMPORT_TEMPLATE,
  MAX_IMPORT_BYTES,
} from './import-customers-dialog';
import { mileageValidator, VehicleFormDialog, yearValidator } from './vehicle-form-dialog';

const dialogProviders = (data: unknown, close = vi.fn()) => [
  ...provideTestApi('mock'),
  provideAuthData(),
  provideCustomersData(),
  { provide: DialogRef, useValue: { close } },
  { provide: DIALOG_DATA, useValue: data },
  { provide: UI_DIALOG_TITLE_ID, useValue: 'dialog-title' },
];

describe('vehicle validators', () => {
  it('accepts plausible years and mileage only', () => {
    const next = new Date().getFullYear() + 1;

    expect(yearValidator(new FormControl(2020))).toBeNull();
    expect(yearValidator(new FormControl(next + 1))).toEqual({ year: true });
    expect(yearValidator(new FormControl(1949))).toEqual({ year: true });
    expect(mileageValidator(new FormControl(0))).toBeNull();
    expect(mileageValidator(new FormControl(1.5))).toEqual({ mileage: true });
  });
});

describe('job polling', () => {
  afterEach(() => vi.useRealTimers());

  it('polls until the job finishes', async () => {
    const statuses: Job['status'][] = ['Processing', 'Processing', 'Completed'];
    const job = (status: Job['status']): Job => ({
      id: 'j',
      kind: 'CustomerImport',
      status,
      fileName: null,
      content: null,
      totalLines: null,
      validLines: null,
      errors: [],
    });
    const load = vi.fn(() => of(job(statuses.shift() ?? 'Completed')));

    const last = await lastValueFrom(pollJob(load, 'j', 1));

    expect(last.status).toBe('Completed');
    expect(load).toHaveBeenCalledTimes(3);
  });

  it('downloads text through a temporary link', () => {
    const click = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => undefined);
    Object.assign(URL, { createObjectURL: vi.fn(() => 'blob:x'), revokeObjectURL: vi.fn() });

    downloadText(document, 'a.json', '{}', 'application/json');

    expect(click).toHaveBeenCalled();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:x');
    expect(document.querySelector('a[download]')).toBeNull();
  });
});

describe('UiConfirmDialog', () => {
  it('confirms or cancels', async () => {
    const close = vi.fn();
    const view = await render(UiConfirmDialog, {
      providers: [
        { provide: DialogRef, useValue: { close } },
        {
          provide: DIALOG_DATA,
          useValue: { title: 'Excluir', body: 'Certeza?', confirm: 'Sim', danger: true },
        },
        { provide: UI_DIALOG_TITLE_ID, useValue: 't' },
      ],
    });

    await userEvent.click(screen.getByRole('button', { name: 'Sim' }));
    await userEvent.click(screen.getByRole('button', { name: messages.ui.dialog.cancel }));

    expect(close.mock.calls).toEqual([[true], [false]]);
    await expectNoAxeViolations(view.container);
  });
});

describe('VehicleFormDialog', () => {
  it('requires the fields and normalizes the plate', async () => {
    const close = vi.fn();
    await render(VehicleFormDialog, {
      providers: dialogProviders({ customerId: 'c', vehicle: null }, close),
      configureTestBed: () => {
        signInWithFakeApi('Attendant');
      },
    });
    const user = userEvent.setup();

    await user.type(
      screen.getByRole('textbox', { name: new RegExp(messages.customers.vehicle.plate) }),
      'ab1',
    );
    await user.click(screen.getByRole('button', { name: messages.customers.vehicle.save }));

    expect(
      screen.getByRole('textbox', { name: new RegExp(messages.customers.vehicle.plate) }),
    ).toHaveValue('AB1');
    expect(screen.getByText(messages.customers.vehicle.invalidPlate)).toBeInTheDocument();
    expect(screen.getByText(messages.customers.vehicle.invalidYear)).toBeInTheDocument();
    expect(close).not.toHaveBeenCalled();
  });

  it('marks a duplicated plate returned by the API', async () => {
    const close = vi.fn();
    await render(VehicleFormDialog, {
      providers: dialogProviders(
        { customerId: '00000000-0000-4000-8000-0000000000aa', vehicle: null },
        close,
      ),
      configureTestBed: () => {
        signInWithFakeApi('Attendant');
      },
    });
    const user = userEvent.setup();
    const vehicle = messages.customers.vehicle;

    await user.type(screen.getByRole('textbox', { name: new RegExp(vehicle.plate) }), 'FJK4E21');
    await user.type(screen.getByRole('textbox', { name: new RegExp(vehicle.brand) }), 'VW');
    await user.type(screen.getByRole('textbox', { name: new RegExp(vehicle.model) }), 'Polo');
    await user.type(screen.getByRole('spinbutton', { name: new RegExp(vehicle.year) }), '2021');
    await user.type(screen.getByRole('spinbutton', { name: new RegExp(vehicle.mileage) }), '10');
    await user.click(screen.getByRole('button', { name: vehicle.save }));

    expect(await screen.findByText(messages.fake.plateInUse)).toBeInTheDocument();
    expect(close).not.toHaveBeenCalled();
  });
});

describe('ImportCustomersDialog', () => {
  afterEach(() => vi.useRealTimers());

  async function setup() {
    const view = await render(ImportCustomersDialog, {
      providers: dialogProviders(null),
      configureTestBed: () => {
        signInWithFakeApi('Attendant');
      },
    });
    return { view, user: userEvent.setup() };
  }

  const fileInput = (): HTMLInputElement => screen.getByLabelText(messages.customers.import.file);

  it('rejects large and non CSV files', async () => {
    const { user } = await setup();
    const big = new File(['x'], 'big.csv', { type: 'text/csv' });
    Object.defineProperty(big, 'size', { value: MAX_IMPORT_BYTES + 1 });

    await user.upload(fileInput(), big);
    expect(screen.getByRole('alert')).toHaveTextContent(messages.customers.import.tooLarge);

    await user.click(screen.getByRole('button', { name: messages.customers.import.submit }));
    expect(screen.getByRole('alert')).toHaveTextContent(messages.customers.import.chooseFile);
  });

  it('sends the file to the queue and shows the report by line', async () => {
    const { view, user } = await setup();
    const csv = `${IMPORT_TEMPLATE}Sem Doc;123;1;x\n`;

    await user.upload(fileInput(), new File([csv], 'clientes.csv', { type: 'text/csv' }));
    await user.click(screen.getByRole('button', { name: messages.customers.import.submit }));

    const report = await screen.findByRole(
      'region',
      { name: messages.customers.import.reportTitle },
      { timeout: 6000 },
    );
    expect(within(report).getAllByRole('row')).toHaveLength(4);
    expect(within(report).getAllByText('3')).toHaveLength(3);
    await expectNoAxeViolations(view.container);
  });

  it('offers the template for download', async () => {
    const { user } = await setup();
    const click = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => undefined);
    Object.assign(URL, { createObjectURL: vi.fn(() => 'blob:x'), revokeObjectURL: vi.fn() });

    await user.click(screen.getByRole('button', { name: messages.customers.import.template }));

    expect(click).toHaveBeenCalled();
    expect(await firstValueFrom(of(TestBed.inject(DialogRef)))).toBeTruthy();
  });
});
