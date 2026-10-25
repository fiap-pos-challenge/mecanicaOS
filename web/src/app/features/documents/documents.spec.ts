import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { SAMPLE_PDF_URL } from '../../../testing/fake-api/portal/portal.fake-api';
import { messages } from '../../core/i18n/messages.pt-BR';
import { UiDialogService } from '../../shared/ui/dialog/dialog';
import { cents } from '../../shared/util/money';
import { provideAuthData } from '../auth/data/auth.providers';
import { DocumentDialog } from './components/document-dialog';
import { DocumentViewer } from './components/document-viewer';
import { formatDocumentDate, verificationUrl } from './components/document-sheet';
import { DocumentModel } from './data/document.models';
import { provideDocumentsData } from './data/documents.gateway';
import { VerifyPage } from './pages/verify-page';

const text = messages.documents;

@Component({ selector: 'app-host', template: '' })
class Host {}

const receipt: DocumentModel = {
  kind: 'Receipt',
  code: 'REC1-3310-M2XT',
  number: 3310,
  version: null,
  issuedAt: '2026-10-10T12:00:00Z',
  shopName: 'Oficina Exemplo',
  customerName: 'Joana Prado',
  vehicle: 'FJK4E21 · VW Polo',
  lines: [{ name: 'Troca de óleo', quantity: 1, unitPrice: cents(6_000), total: cents(6_000) }],
  services: cents(6_000),
  parts: cents(0),
  discount: cents(0),
  total: cents(6_000),
  paidAt: '2026-10-10T13:00:00Z',
  paymentMethods: ['Pix'],
};

function providers(mode: TestApiMode = 'mock') {
  return [
    ...provideTestApi(mode, [{ path: '**', children: [] }]),
    provideAuthData(),
    provideDocumentsData(),
  ];
}

describe('document helpers', () => {
  it('builds the verification URL and formats dates', () => {
    expect(verificationUrl('https://oficina.example', 'A B')).toBe(
      'https://oficina.example/v/A%20B',
    );
    expect(formatDocumentDate('2026-10-10T12:00:00Z')).toMatch(/10\/10\/2026/);
  });
});

describe('DocumentViewer', () => {
  it('shows the document with its verification code and acts on it', async () => {
    const view = await render(DocumentViewer, {
      providers: providers(),
      inputs: { document: receipt, allowSend: true },
      configureTestBed: () => signInWithFakeApi('Attendant'),
    });
    const user = userEvent.setup();
    const print = vi.spyOn(window, 'print').mockReturnValue(undefined);
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockReturnValue(undefined);
    const writeText = vi.spyOn(navigator.clipboard, 'writeText').mockResolvedValue(undefined);

    expect(screen.getByText(text.title(text.kinds.Receipt, 3310, null))).toBeVisible();
    expect(screen.getByTestId('verification-code')).toHaveTextContent(
      text.verification('REC1-3310-M2XT'),
    );
    expect(screen.getByText(text.noFiscal)).toBeVisible();
    expect(screen.getByText(text.paidAt(formatDocumentDate(receipt.paidAt ?? '')))).toBeVisible();
    await expectNoAxeViolations(view.container);

    await user.click(screen.getByRole('button', { name: text.print }));
    expect(print).toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: text.download }));
    await vi.waitFor(() => expect(click).toHaveBeenCalled());
    const anchor = click.mock.contexts[0] as HTMLAnchorElement;
    expect(anchor.getAttribute('href')).toBe(SAMPLE_PDF_URL);
    expect(anchor.download).toBe('REC1-3310-M2XT.pdf');
    await user.click(screen.getByRole('button', { name: text.copyLink }));
    expect(writeText).toHaveBeenCalledWith(expect.stringContaining('/v/REC1-3310-M2XT'));
    await user.click(screen.getByRole('button', { name: text.sendWhatsApp }));
    await user.click(screen.getByRole('button', { name: text.sendEmail }));
    expect(screen.queryByRole('alert')).toBeNull();

    view.fixture.componentRef.setInput('document', { ...receipt, code: 'missing' });
    await user.click(screen.getByRole('button', { name: text.download }));
    expect(await screen.findByRole('alert')).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.sendEmail }));
    expect(screen.getByRole('alert')).toBeVisible();
    print.mockRestore();
    click.mockRestore();
  });

  it('shows a draft without code and without sending', async () => {
    await render(DocumentViewer, {
      providers: providers(),
      inputs: {
        document: {
          ...receipt,
          kind: 'Quote',
          code: null,
          version: 2,
          paidAt: null,
          paymentMethods: [],
        },
      },
    });

    expect(screen.getByText(text.draft)).toBeVisible();
    expect(screen.getByText(text.title(text.kinds.Quote, 3310, 2))).toBeVisible();
    expect(screen.queryByRole('button', { name: text.download })).toBeNull();
    expect(screen.queryByText(text.noFiscal)).toBeNull();
  });
});

describe('DocumentDialog', () => {
  async function openDocument(data: object) {
    await render(Host, { providers: providers() });
    signInWithFakeApi('Attendant');
    TestBed.inject(UiDialogService).open(DocumentDialog, data);
    return screen.findByRole('dialog', { name: text.preview });
  }

  it('loads a document by code', async () => {
    const dialog = await openDocument({ code: 'ORC1-3324-A7KD', allowSend: true });
    expect(await within(dialog).findByTestId('verification-code')).toHaveTextContent(
      'ORC1-3324-A7KD',
    );
    expect(within(dialog).getByRole('button', { name: text.sendWhatsApp })).toBeVisible();
  });

  it('shows the given document and load errors', async () => {
    const dialog = await openDocument({ document: receipt });
    expect(within(dialog).getByText(text.title(text.kinds.Receipt, 3310, null))).toBeVisible();

    TestBed.inject(UiDialogService).open(DocumentDialog, { code: 'missing' });
    expect(await screen.findByText(messages.fake.documentNotFound)).toBeVisible();
  });
});

describe('VerifyPage', () => {
  it('confirms an authentic document and rejects an unknown code', async () => {
    const view = await render(VerifyPage, {
      providers: providers(),
      inputs: { code: 'REC1-3310-M2XT' },
    });

    expect(await screen.findByTestId('verify-valid')).toHaveTextContent(text.verify.valid);
    expect(screen.getByText(new RegExp(`^${text.kinds.Receipt}`))).toBeVisible();
    await expectNoAxeViolations(view.container);

    view.fixture.componentRef.setInput('code', 'FAKE-0000');
    expect(await screen.findByTestId('verify-invalid')).toHaveTextContent(text.verify.invalid);

    TestBed.inject(FakeBackend).setScenario('portal', 'serverError');
    view.fixture.componentRef.setInput('code', 'OTHER');
    await vi.waitFor(() => expect(screen.getByTestId('verify-invalid')).toBeVisible());
  });

  it('shows the construction notice', async () => {
    await render(VerifyPage, { providers: providers('pending'), inputs: { code: 'X' } });
    expect(await screen.findByText(messages.pending.documents)).toBeVisible();
  });
});
