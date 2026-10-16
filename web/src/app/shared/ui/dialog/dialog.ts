import { Dialog, DialogRef } from '@angular/cdk/dialog';
import { ComponentType } from '@angular/cdk/portal';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  Injectable,
  InjectionToken,
  input,
} from '@angular/core';
import { X as XIcon } from 'lucide';
import { UiIcon } from '../icon/icon';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiIconButton } from '../icon-button/icon-button';

export const UI_DIALOG_TITLE_ID = new InjectionToken<string>('UI_DIALOG_TITLE_ID');

let nextDialogId = 0;

@Injectable({ providedIn: 'root' })
export class UiDialogService {
  private readonly dialog = inject(Dialog);

  open<TComponent, TData = unknown, TResult = unknown>(
    component: ComponentType<TComponent>,
    data?: TData,
  ): DialogRef<TResult, TComponent> {
    const titleId = `ui-dialog-title-${++nextDialogId}`;
    return this.dialog.open<TResult, TData, TComponent>(component, {
      data: data ?? null,
      ariaLabelledBy: titleId,
      ariaModal: true,
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      panelClass: 'ui-dialog-panel',
      backdropClass: 'ui-dialog-backdrop',
      providers: [{ provide: UI_DIALOG_TITLE_ID, useValue: titleId }],
    });
  }
}

@Component({
  selector: 'ui-dialog-layout',
  imports: [UiIcon, UiIconButton],
  templateUrl: './dialog.html',
  styleUrl: './dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiDialogLayout {
  readonly title = input.required<string>();

  protected readonly titleId = inject(UI_DIALOG_TITLE_ID);
  protected readonly closeLabel = messages.ui.dialog.close;
  protected readonly closeIcon = XIcon;
  private readonly dialogRef = inject(DialogRef);

  protected close(): void {
    this.dialogRef.close();
  }
}
