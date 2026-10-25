import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { ToastService, UiToastRegion } from './toast';

describe('Toast', () => {
  afterEach(() => vi.useRealTimers());

  it('shows info as status and errors as alert with detail', async () => {
    const { container, fixture } = await render(UiToastRegion);
    const toasts = TestBed.inject(ToastService);

    toasts.show('Salvo', { tone: 'success', durationMs: 0 });
    toasts.show('Falhou', { tone: 'error', detail: 'traceId 123', durationMs: 0 });
    fixture.detectChanges();

    expect(screen.getByRole('status')).toHaveTextContent('Salvo');
    expect(screen.getByRole('alert')).toHaveTextContent('Falhou');
    expect(screen.getByText('traceId 123')).toBeInTheDocument();
    await expectNoAxeViolations(container);
  });

  it('dismisses with the button', async () => {
    const { fixture } = await render(UiToastRegion);
    TestBed.inject(ToastService).show('Aviso', { durationMs: 0 });
    fixture.detectChanges();

    await userEvent.click(screen.getByRole('button', { name: messages.ui.toast.dismiss }));

    expect(screen.queryByText('Aviso')).not.toBeInTheDocument();
  });

  it('dismisses automatically after the duration', () => {
    vi.useFakeTimers();
    const toasts = TestBed.inject(ToastService);

    toasts.show('Temporário');
    expect(toasts.toasts()).toHaveLength(1);
    expect(toasts.toasts()[0]?.tone).toBe('info');

    vi.advanceTimersByTime(5000);
    expect(toasts.toasts()).toHaveLength(0);
  });
});
