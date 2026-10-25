import { TestBed } from '@angular/core/testing';
import { render, screen, waitFor, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { messages } from '../../core/i18n/messages.pt-BR';
import { provideAuthData } from '../auth/data/auth.providers';
import { DocumentsPage } from './pages/documents-page';

const text = messages.documents.list;

async function show(mode: TestApiMode = 'mock', failing = false) {
  const view = await render(DocumentsPage, {
    providers: [...provideTestApi(mode, [{ path: '**', children: [] }]), provideAuthData()],
    configureTestBed: () => {
      signInWithFakeApi('Attendant');
      if (failing) {
        TestBed.inject(FakeBackend).setScenario('portal', 'serverError');
      }
    },
  });
  return { view, user: userEvent.setup() };
}

describe('DocumentsPage', () => {
  it('lists, filters and opens the issued documents', async () => {
    const { view, user } = await show();

    const table = await screen.findByRole('table', { name: text.caption });
    expect(await within(table).findByText('REC1-3310-M2XT')).toBeInTheDocument();
    expect(within(table).getByText('ORC1-3324-A7KD')).toBeInTheDocument();
    await expectNoAxeViolations(view.container);

    await user.click(screen.getByRole('radio', { name: messages.documents.kinds.Receipt }));
    await waitFor(() =>
      expect(within(table).queryByText('ORC1-3324-A7KD')).not.toBeInTheDocument(),
    );
    expect(within(table).getByText('REC1-3310-M2XT')).toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: text.all }));
    await user.type(screen.getByLabelText(text.search), 'qw8p');
    await waitFor(() =>
      expect(within(table).queryByText('REC1-3310-M2XT')).not.toBeInTheDocument(),
    );
    const row = within(table).getByText('OS01-3310-QW8P').closest('tr') as HTMLElement;
    await user.click(within(row).getByRole('button', { name: text.open }));
    expect(
      await screen.findByRole('dialog', { name: messages.documents.preview }),
    ).toBeInTheDocument();
  });

  it('reports load errors and retries', async () => {
    const { user } = await show('mock', true);

    expect(await screen.findByRole('alert')).toHaveTextContent(text.loadError);
    TestBed.inject(FakeBackend).setScenario('portal', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByText('REC1-3310-M2XT')).toBeInTheDocument();
  });

  it('shows the pending notice when the API is not published', async () => {
    await show('pending');

    expect(await screen.findByText(messages.pending.documents)).toBeInTheDocument();
  });
});
