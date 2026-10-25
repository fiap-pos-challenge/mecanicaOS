import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { SavedReport } from '../data/report.models';
import { ReportsStore } from '../state/reports.store';

export interface ReportNameDialogData {
  readonly store: ReportsStore;
  readonly report: SavedReport | null;
}

export function validName(name: string): boolean {
  const trimmed = name.trim();
  return trimmed.length >= 3 && trimmed.length <= 80;
}

@Component({
  selector: 'app-report-name-dialog',
  imports: [UiButton, UiControl, UiDialogLayout, UiField],
  template: `
    <ui-dialog-layout [title]="data.report ? text.renameTitle : text.title">
      <form class="dialog-form" novalidate (submit)="submit($event)">
        @if (error(); as message) {
          <p class="alert" role="alert">{{ message }}</p>
        }
        <ui-field
          [label]="text.name"
          [required]="true"
          [error]="invalid() ? text.nameInvalid : null"
        >
          <input uiControl maxlength="80" [value]="name()" (input)="setName($event)" />
        </ui-field>
        <div class="footer">
          <button uiButton type="button" (click)="ref.close(null)">{{ text.cancel }}</button>
          <button uiButton variant="primary" type="submit" [loading]="saving()">
            {{ data.report ? text.rename : text.submit }}
          </button>
        </div>
      </form>
    </ui-dialog-layout>
  `,
  styleUrl: './report-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportNameDialog {
  protected readonly ref = inject<DialogRef<string | null>>(DialogRef);
  protected readonly data = inject<ReportNameDialogData>(DIALOG_DATA);
  protected readonly text = messages.reports.save;
  protected readonly name = signal(this.data.report?.name ?? '');
  protected readonly invalid = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected setName(event: Event): void {
    this.name.set((event.target as HTMLInputElement).value);
    this.invalid.set(false);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const name = this.name().trim();
    if (!validName(name)) {
      this.invalid.set(true);
      return;
    }

    this.saving.set(true);
    const report = this.data.report;
    const outcome = report
      ? await this.data.store.rename(report, name)
      : await this.data.store.save(name);
    this.saving.set(false);
    if (outcome.ok) {
      this.ref.close(name);
    } else {
      this.error.set(outcome.error);
    }
  }
}
