import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { isValidEmail } from '../../../shared/util/email';
import {
  ExportFormat,
  MAX_RECIPIENTS,
  parseRecipients,
  REPORT_WEEKDAYS,
  ReportSchedule,
  ReportWeekday,
  SavedReport,
} from '../data/report.models';
import { ReportsStore } from '../state/reports.store';

export interface ReportScheduleDialogData {
  readonly store: ReportsStore;
  readonly report: SavedReport;
}

export type ScheduleField = 'time' | 'recipients';

export function scheduleErrors(time: string, recipients: readonly string[]): ScheduleField[] {
  const errors: ScheduleField[] = [];
  if (!/^([01]\d|2[0-3]):[0-5]\d$/.test(time)) {
    errors.push('time');
  }

  if (
    recipients.length === 0 ||
    recipients.length > MAX_RECIPIENTS ||
    !recipients.every(isValidEmail)
  ) {
    errors.push('recipients');
  }

  return errors;
}

@Component({
  selector: 'app-report-schedule-dialog',
  imports: [UiButton, UiControl, UiDialogLayout, UiField],
  template: `
    <ui-dialog-layout [title]="text.title(data.report.name)">
      <form class="dialog-form" novalidate (submit)="submit($event)">
        @if (error(); as message) {
          <p class="alert" role="alert">{{ message }}</p>
        }
        <div class="row">
          <ui-field [label]="text.weekday" [required]="true">
            <select uiControl [value]="weekday()" (change)="setWeekday($event)">
              @for (day of weekdays; track day) {
                <option [value]="day" [selected]="day === weekday()">{{ weekdayText[day] }}</option>
              }
            </select>
          </ui-field>
          <ui-field
            [label]="text.time"
            [required]="true"
            [error]="errors().includes('time') ? text.timeInvalid : null"
          >
            <input uiControl type="time" [value]="time()" (input)="setTime($event)" />
          </ui-field>
          <ui-field [label]="text.format" [required]="true">
            <select uiControl [value]="format()" (change)="setFormat($event)">
              @for (option of formats; track option) {
                <option [value]="option" [selected]="option === format()">
                  {{ formatText[option] }}
                </option>
              }
            </select>
          </ui-field>
        </div>
        <ui-field
          [label]="text.recipients"
          [hint]="text.recipientsHint"
          [required]="true"
          [error]="errors().includes('recipients') ? text.recipientsInvalid : null"
        >
          <textarea
            uiControl
            rows="2"
            [value]="recipients()"
            (input)="setRecipients($event)"
          ></textarea>
        </ui-field>
        <div class="footer">
          @if (data.report.schedule) {
            <button uiButton variant="danger" type="button" [loading]="saving()" (click)="clear()">
              {{ text.remove }}
            </button>
          }
          <button uiButton type="button" (click)="ref.close(null)">{{ text.cancel }}</button>
          <button uiButton variant="primary" type="submit" [loading]="saving()">
            {{ text.submit }}
          </button>
        </div>
      </form>
    </ui-dialog-layout>
  `,
  styleUrl: './report-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportScheduleDialog {
  protected readonly ref = inject<DialogRef<'saved' | 'removed' | null>>(DialogRef);
  protected readonly data = inject<ReportScheduleDialogData>(DIALOG_DATA);
  protected readonly text = messages.reports.schedule;
  protected readonly weekdayText = messages.reports.weekdays;
  protected readonly formatText = messages.reports.formats;
  protected readonly weekdays = REPORT_WEEKDAYS;
  protected readonly formats: readonly ExportFormat[] = ['pdf', 'xlsx'];
  private readonly current = this.data.report.schedule;
  protected readonly weekday = signal<ReportWeekday>(this.current?.weekday ?? 'Monday');
  protected readonly time = signal(this.current?.time ?? '08:00');
  protected readonly format = signal<ExportFormat>(this.current?.format ?? 'pdf');
  protected readonly recipients = signal(this.current?.recipients.join(', ') ?? '');
  protected readonly errors = signal<readonly ScheduleField[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected setWeekday(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.weekday.set(REPORT_WEEKDAYS.find((day) => day === value) ?? 'Monday');
  }

  protected setTime(event: Event): void {
    this.time.set((event.target as HTMLInputElement).value);
  }

  protected setFormat(event: Event): void {
    this.format.set((event.target as HTMLSelectElement).value === 'xlsx' ? 'xlsx' : 'pdf');
  }

  protected setRecipients(event: Event): void {
    this.recipients.set((event.target as HTMLTextAreaElement).value);
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    const recipients = parseRecipients(this.recipients());
    const errors = scheduleErrors(this.time(), recipients);
    this.errors.set(errors);
    if (errors.length > 0) {
      return;
    }

    const schedule: ReportSchedule = {
      weekday: this.weekday(),
      time: this.time(),
      recipients,
      format: this.format(),
    };
    await this.persist(schedule, 'saved');
  }

  protected async clear(): Promise<void> {
    await this.persist(null, 'removed');
  }

  private async persist(schedule: ReportSchedule | null, result: 'saved' | 'removed') {
    this.saving.set(true);
    const outcome = await this.data.store.schedule(this.data.report, schedule);
    this.saving.set(false);
    if (outcome.ok) {
      this.ref.close(result);
    } else {
      this.error.set(outcome.error);
    }
  }
}
