import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { fireEvent, screen, waitFor } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../button/button';
import { UiDialogLayout, UiDialogService } from './dialog';

@Component({
  selector: 'app-test-dialog',
  imports: [UiDialogLayout, UiButton],
  template: `
    <ui-dialog-layout title="Confirmar">
      <p>{{ data }}</p>
      <div uiDialogFooter>
        <button uiButton type="button" (click)="ref.close('ok')">Confirmar envio</button>
      </div>
    </ui-dialog-layout>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class TestDialog {
  protected readonly ref = inject<DialogRef<string>>(DialogRef);
  protected readonly data = inject<string>(DIALOG_DATA);
}

describe('UiDialogService', () => {
  function open() {
    const ref = TestBed.inject(UiDialogService).open<TestDialog, string, string>(
      TestDialog,
      'Enviar o orçamento?',
    );
    TestBed.tick();
    return ref;
  }

  it('opens a labelled modal dialog with the data and moves focus inside', async () => {
    open();

    const dialog = await screen.findByRole('dialog', { name: 'Confirmar' });
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(screen.getByText('Enviar o orçamento?')).toBeInTheDocument();
    await waitFor(() => expect(dialog.contains(document.activeElement)).toBe(true));
    await expectNoAxeViolations(dialog);
  });

  it('closes with the result from the footer action', async () => {
    const ref = open();
    const closed = vi.fn();
    ref.closed.subscribe(closed);

    await userEvent.click(await screen.findByRole('button', { name: 'Confirmar envio' }));

    expect(closed).toHaveBeenCalledWith('ok');
  });

  it('closes with the close button', async () => {
    const ref = open();
    const closed = vi.fn();
    ref.closed.subscribe(closed);

    await userEvent.click(await screen.findByRole('button', { name: messages.ui.dialog.close }));

    expect(closed).toHaveBeenCalled();
  });

  it('closes with Escape', async () => {
    const ref = open();
    const closed = vi.fn();
    ref.closed.subscribe(closed);
    const dialog = await screen.findByRole('dialog');
    await waitFor(() => expect(dialog.contains(document.activeElement)).toBe(true));

    fireEvent.keyDown(document.activeElement ?? dialog, { key: 'Escape', keyCode: 27 });

    await waitFor(() => expect(closed).toHaveBeenCalled());
  });
});
