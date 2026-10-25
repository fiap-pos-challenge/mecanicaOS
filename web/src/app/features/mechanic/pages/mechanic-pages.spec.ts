import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { render, RenderComponentOptions, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { PwaService } from '../../../core/pwa/pwa.service';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideMechanicData } from '../data/mechanic.gateway';
import { MechanicStore } from '../state/mechanic.store';
import { MechanicOrderPage, readAsDataUrl } from './mechanic-order-page';
import { MechanicPage } from './mechanic-page';

const text = messages.mechanic;
const orderText = messages.serviceOrders;

async function show<T>(
  component: Type<T>,
  options: { mode?: TestApiMode; inputs?: RenderComponentOptions<T>['inputs'] } = {},
) {
  const view = await render(component, {
    providers: [
      ...provideTestApi(options.mode ?? 'mock', [{ path: '**', children: [] }]),
      provideAuthData(),
      provideMechanicData(),
      MechanicStore,
    ],
    ...(options.inputs ? { inputs: options.inputs } : {}),
    configureTestBed: () => {
      signInWithFakeApi('Mechanic');
    },
  });
  return { view, user: userEvent.setup({ applyAccept: false }) };
}

function order(number: number) {
  const found = TestBed.inject(FakeBackend).state.database.serviceOrders.orders.find(
    (item) => item.number === number,
  );
  if (!found) {
    throw new Error('Missing order');
  }

  return found;
}

function go(online: boolean): void {
  Object.defineProperty(navigator, 'onLine', { value: online, configurable: true });
  window.dispatchEvent(new Event(online ? 'online' : 'offline'));
}

async function openOrder(number: number) {
  const result = await show(MechanicOrderPage, { inputs: { id: '' } });
  result.view.fixture.componentRef.setInput('id', order(number).id);
  await screen.findByText(orderText.number(number));
  return result;
}

afterEach(() => go(true));

describe('mechanic helpers', () => {
  it('reads a file as a data URL', async () => {
    const url = await readAsDataUrl(new Blob(['x'], { type: 'image/png' }));
    expect(url.startsWith('data:image/png;base64,')).toBe(true);
  });
});

describe('MechanicPage', () => {
  it('lists my orders and offers installation', async () => {
    const { view, user } = await show(MechanicPage);

    expect(await screen.findByRole('link', { name: text.open(3327) })).toBeVisible();
    expect(screen.getByRole('link', { name: text.open(3318) })).toBeVisible();
    expect(screen.queryByRole('link', { name: text.open(3326) })).toBeNull();
    await expectNoAxeViolations(view.container);

    const pwa = TestBed.inject(PwaService);
    pwa.canInstall.set(true);
    const install = vi.spyOn(pwa, 'install').mockResolvedValue(true);
    view.fixture.detectChanges();
    await user.click(await screen.findByRole('button', { name: text.pwa.install }));
    expect(install).toHaveBeenCalled();
  });

  it('shows the construction notice and load errors', async () => {
    await show(MechanicPage, { mode: 'pending' });
    expect(await screen.findByText(messages.pending.serviceOrders)).toBeVisible();
  });
});

describe('MechanicOrderPage', () => {
  it('fills the checklist, attaches photos and validates the files', async () => {
    const { view, user } = await openOrder(3327);
    await expectNoAxeViolations(view.container);

    const brakes = screen.getByRole('group', { name: 'Freios' });
    await user.click(within(brakes).getByRole('radio', { name: text.states.Replace }));
    await vi.waitFor(() => expect(order(3327).checklist[1]?.state).toBe('Replace'));

    await user.click(screen.getByRole('tab', { name: text.sections.photos }));
    expect(screen.getByText(text.photos.empty)).toBeVisible();
    const input = screen.getByLabelText(text.photos.take);
    await user.upload(input, new File(['gif'], 'roda.gif', { type: 'image/gif' }));
    expect(await screen.findByText(text.photos.invalidType)).toBeVisible();
    const big = new File(['x'], 'grande.jpg', { type: 'image/jpeg' });
    Object.defineProperty(big, 'size', { value: 6 * 1024 * 1024 });
    await user.upload(input, big);
    expect(await screen.findByText(text.photos.tooLarge)).toBeVisible();
    await user.upload(input, new File(['png'], 'roda.png', { type: 'image/png' }));
    expect(await screen.findByAltText(text.photos.alt(3327, 1))).toBeVisible();
    await vi.waitFor(() => expect(order(3327).photos).toHaveLength(1));
  });

  it('describes the problem, asks the AI and sends the items to the quote', async () => {
    const { user } = await openOrder(3327);

    await user.click(screen.getByRole('tab', { name: text.sections.diagnosis }));
    const description = screen.getByRole('textbox', { name: text.diagnosis.label });
    await user.type(description, 'bieleta dianteira com folga');
    await user.click(screen.getByRole('button', { name: text.diagnosis.save }));
    await vi.waitFor(() => expect(order(3327).diagnosis).toBe('bieleta dianteira com folga'));
    await user.click(screen.getByRole('button', { name: text.diagnosis.suggest }));
    const suggestion = await screen.findByRole('checkbox', { name: /Alinhamento 3D/ });
    expect(suggestion).toBeChecked();
    await user.click(suggestion);
    await user.click(suggestion);
    await user.click(screen.getByRole('button', { name: text.actions.sendToQuote }));
    await vi.waitFor(() => expect(order(3327).sentToQuote).toBe(true));
    expect(order(3327).quoteItems.map((item) => item.name)).toEqual(['Alinhamento 3D']);

    await user.clear(description);
    await user.type(description, 'xx');
    await user.click(screen.getByRole('button', { name: text.diagnosis.suggest }));
    expect(await screen.findByRole('alert')).toBeVisible();
    await user.clear(description);
    await user.type(description, 'barulho estranho');
    await user.click(screen.getByRole('button', { name: text.diagnosis.suggest }));
    expect(await screen.findByText(text.diagnosis.noSuggestions)).toBeVisible();
  });

  it('works offline with a visible queue and syncs when the network returns', async () => {
    const { user } = await openOrder(3327);

    go(false);
    expect(await screen.findByText(text.sync.offline)).toBeVisible();
    await user.click(
      within(screen.getByRole('group', { name: 'Pneus' })).getByRole('radio', {
        name: text.states.Ok,
      }),
    );
    await user.click(screen.getByRole('tab', { name: text.sections.diagnosis }));
    expect(screen.getByRole('button', { name: text.diagnosis.suggest })).toBeDisabled();
    expect(screen.getByText(text.diagnosis.offlineSuggest)).toBeVisible();
    await user.type(screen.getByRole('textbox', { name: text.diagnosis.label }), 'coxim rachado');
    await user.click(screen.getByRole('button', { name: text.diagnosis.save }));
    const banner = screen.getByTestId('sync-banner');
    expect(banner).toHaveTextContent(text.sync.pending(2));
    expect(banner).toHaveTextContent(text.sync.item(3327, text.sync.labels.diagnosis));
    expect(order(3327).diagnosis).toBe('');

    go(true);
    await vi.waitFor(() => expect(screen.queryByTestId('sync-banner')).toBeNull());
    expect(order(3327).diagnosis).toBe('coxim rachado');
    expect(order(3327).checklist[3]?.state).toBe('Ok');
  });

  it('starts diagnosis, runs and finishes the service and shows sync failures', async () => {
    const { user, view } = await openOrder(3326);
    await user.click(screen.getByRole('button', { name: text.actions.startDiagnosis }));
    await vi.waitFor(() => expect(order(3326).status).toBe('InDiagnosis'));

    view.fixture.componentRef.setInput('id', order(3318).id);
    await screen.findByText(orderText.number(3318));
    await user.click(screen.getByRole('button', { name: text.actions.start }));
    expect(await screen.findByText(text.actions.timer)).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.actions.finish }));
    await vi.waitFor(() => expect(order(3318).status).toBe('Completed'));

    view.fixture.componentRef.setInput('id', order(3315).id);
    await screen.findByText(orderText.number(3315));
    order(3315).status = 'Completed';
    await user.click(screen.getByRole('button', { name: text.actions.finish }));
    expect(await screen.findByText(/fim do serviço/)).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.sync.dismiss }));
    expect(screen.queryByText(/fim do serviço/)).toBeNull();
  });

  it('shows load errors and the construction notice', async () => {
    const { view, user } = await show(MechanicOrderPage, { inputs: { id: 'missing' } });
    expect(await screen.findByText(text.loadError)).toBeVisible();
    view.fixture.componentRef.setInput('id', order(3327).id);
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByText(orderText.number(3327))).toBeVisible();
  });

  it('shows the construction notice', async () => {
    await show(MechanicOrderPage, { mode: 'pending', inputs: { id: 'x' } });
    expect(await screen.findByText(messages.pending.serviceOrders)).toBeVisible();
  });
});
