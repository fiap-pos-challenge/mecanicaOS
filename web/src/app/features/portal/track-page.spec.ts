import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { messages } from '../../core/i18n/messages.pt-BR';
import { toTrackedOrder } from './data/tracking.gateway';
import { TrackPage } from './pages/track-page';

const text = messages.track;

async function show(mode: TestApiMode = 'mock') {
  const view = await render(TrackPage, {
    providers: [...provideTestApi(mode, [{ path: '**', children: [] }])],
  });
  return { view, user: userEvent.setup() };
}

async function lookup(user: ReturnType<typeof userEvent.setup>, number: string, document: string) {
  await user.clear(screen.getByLabelText(text.number));
  await user.type(screen.getByLabelText(text.number), number);
  await user.clear(screen.getByLabelText(text.document));
  await user.type(screen.getByLabelText(text.document), document);
  await user.click(screen.getByRole('button', { name: text.submit }));
}

describe('TrackPage', () => {
  it('finds the order with the number and the document of the customer', async () => {
    const { view, user } = await show();

    expect(
      screen.getByRole('heading', { level: 1, name: messages.screens.portal }),
    ).toBeInTheDocument();
    await lookup(user, '3310', '529.982.247-25');

    expect(
      await screen.findByRole('heading', { level: 2, name: text.result(3310) }),
    ).toBeInTheDocument();
    expect(screen.getByText(messages.serviceOrders.statuses.Completed)).toBeInTheDocument();
    expect(screen.getByText(text.quoteStatuses.Approved)).toBeInTheDocument();
    await expectNoAxeViolations(view.container);
  });

  it('validates the fields and explains when nothing is found', async () => {
    const { user } = await show();

    await lookup(user, '0', '123');
    expect(screen.getByRole('alert')).toHaveTextContent(text.invalid);

    await lookup(user, '3310', '111.444.777-35');
    expect(await screen.findByRole('alert')).toHaveTextContent(text.notFound);

    TestBed.inject(FakeBackend).setScenario('portal', 'serverError');
    await lookup(user, '3310', '529.982.247-25');
    expect(await screen.findByRole('alert')).toHaveTextContent(text.error);
  });

  it('shows the pending notice when the API is not published', async () => {
    const { user } = await show('pending');

    await lookup(user, '3310', '529.982.247-25');
    expect(await screen.findByText(messages.pending.portal)).toBeInTheDocument();
  });

  it('maps the delivered order with its vehicle', () => {
    const order = toTrackedOrder({
      number: 1,
      status: 'Delivered',
      openedAt: '2026-10-01T10:00:00Z',
      deliveredAt: '2026-10-02T10:00:00Z',
      totalAmount: 150.5,
      vehicle: { licensePlate: 'BRA2E19', brand: 'Fiat', model: 'Uno', year: 2020 },
    });
    expect(order.vehicle).toBe('Fiat Uno 2020');
    expect(order.total).toBe(15_050);
    expect(order.quoteStatus).toBeNull();
  });
});
