import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen, waitFor, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { RealtimeGateway } from '../../../core/realtime/realtime.gateway';
import { PendingRealtimeGateway } from '../../../core/realtime/pending-realtime.gateway';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideServiceOrderData } from '../data/service-order.gateway';
import { ServiceOrdersPage } from '../pages/service-orders-page';
import { SERVICE_ORDER_POLL_MS } from '../state/board.store';

const text = messages.serviceOrders;
const dialogText = text.openDialog;

async function show(role: BackendRole = 'Attendant') {
  await render(ServiceOrdersPage, {
    providers: [
      ...provideTestApi('mock', [{ path: '**', children: [] }]),
      provideAuthData(),
      provideServiceOrderData(),
      { provide: RealtimeGateway, useClass: PendingRealtimeGateway },
      { provide: SERVICE_ORDER_POLL_MS, useValue: 60_000 },
    ],
    configureTestBed: () => signInWithFakeApi(role),
  });
  await screen.findByRole('region', { name: text.board.label }).catch(() => undefined);
  return userEvent.setup();
}

async function openDialog(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByRole('button', { name: text.openOrder }));
  return screen.findByRole('dialog', { name: dialogText.title });
}

describe('OpenOrderDialog', () => {
  it('opens a service order for the chosen customer and vehicle', async () => {
    const user = await show();
    const before = TestBed.inject(FakeBackend).state.database.serviceOrders.orders.length;
    const dialog = await openDialog(user);

    await user.click(within(dialog).getByRole('button', { name: dialogText.confirm }));
    expect(await within(dialog).findByRole('alert')).toHaveTextContent(dialogText.customerRequired);

    await user.type(within(dialog).getByLabelText(dialogText.search), 'Joana');
    await user.click(await within(dialog).findByRole('button', { name: /Joana Prado/ }));
    expect(await within(dialog).findByRole('radio', { name: /Polo/ })).toBeChecked();

    await user.type(within(dialog).getByLabelText(dialogText.problem, { exact: false }), 'x');
    await user.click(within(dialog).getByRole('button', { name: dialogText.confirm }));
    expect(within(dialog).getByRole('alert')).toHaveTextContent(dialogText.problemRequired);

    await user.type(
      within(dialog).getByLabelText(dialogText.problem, { exact: false }),
      'Barulho na suspensão',
    );
    await user.click(within(dialog).getByRole('button', { name: dialogText.confirm }));

    await waitFor(() =>
      expect(TestBed.inject(FakeBackend).state.database.serviceOrders.orders.length).toBe(
        before + 1,
      ),
    );
    const created = TestBed.inject(FakeBackend).state.database.serviceOrders.orders.at(-1);
    expect(created?.complaint).toBe('xBarulho na suspensão');
    await waitFor(() => expect(TestBed.inject(Router).url).toBe(`/service-orders/${created?.id}`));
  });

  it('asks for a vehicle when the customer has several and rejects past promises', async () => {
    const user = await show('Admin');
    const dialog = await openDialog(user);

    await user.type(within(dialog).getByLabelText(dialogText.search), 'Rota');
    await user.click(await within(dialog).findByRole('button', { name: /Rota Sul/ }));
    expect(await within(dialog).findAllByRole('radio')).toHaveLength(3);
    await user.type(
      within(dialog).getByLabelText(dialogText.problem, { exact: false }),
      'Revisão da frota',
    );
    await user.click(within(dialog).getByRole('button', { name: dialogText.confirm }));
    expect(within(dialog).getByRole('alert')).toHaveTextContent(dialogText.vehicleRequired);

    await user.click(within(dialog).getByRole('radio', { name: /Master/ }));
    const promise = within(dialog).getByLabelText(dialogText.promisedAt);
    await user.type(promise, '2020-01-01T10:00');
    await user.click(within(dialog).getByRole('button', { name: dialogText.confirm }));
    expect(within(dialog).getByRole('alert')).toHaveTextContent(dialogText.promiseInPast);

    await user.click(within(dialog).getByRole('button', { name: dialogText.changeCustomer }));
    expect(within(dialog).getByLabelText(dialogText.search)).toBeInTheDocument();
    await user.type(within(dialog).getByLabelText(dialogText.search), 'Ninguém');
    expect(await within(dialog).findByText(dialogText.noResults)).toBeInTheDocument();
    await user.click(within(dialog).getByRole('button', { name: dialogText.cancel }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('hides the action from mechanics', async () => {
    await show('Mechanic');
    await screen.findByRole('heading', { level: 1 });
    expect(screen.queryByRole('button', { name: text.openOrder })).not.toBeInTheDocument();
  });
});
