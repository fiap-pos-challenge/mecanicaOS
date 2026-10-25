import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { UiButton } from '../../shared/ui/button/button';
import { UiDialogLayout } from '../../shared/ui/dialog/dialog';
import { uiCatalogTexts } from './ui-catalog-texts';

@Component({
  selector: 'app-dev-dialog-example',
  imports: [UiButton, UiDialogLayout],
  template: `
    <ui-dialog-layout [title]="text.title">
      <p>{{ text.body }}</p>
      <div uiDialogFooter>
        <button uiButton type="button" (click)="ref.close(false)">{{ cancel }}</button>
        <button uiButton variant="primary" type="button" (click)="ref.close(true)">
          {{ text.confirm }}
        </button>
      </div>
    </ui-dialog-layout>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DevDialogExample {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly text = uiCatalogTexts.dialog;
  protected readonly cancel = uiCatalogTexts.buttons.secondary;
}
