import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiChart } from '../../../shared/ui/chart/chart';
import { ChartData, ValueFormat } from '../../../shared/ui/chart/chart.models';
import { ConfirmDialogData, UiConfirmDialog } from '../../../shared/ui/dialog/confirm-dialog';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { ToastService } from '../../../shared/ui/toast/toast';
import { cents, formatBrl } from '../../../shared/util/money';
import {
  ExportFormat,
  REPORT_CHARTS,
  REPORT_GROUPINGS,
  REPORT_MEASURES,
  REPORT_PERIODS,
  ReportGrouping,
  ReportUnit,
  SavedReport,
} from '../data/report.models';
import { AssistantPanel } from '../components/assistant-panel';
import { ReportNameDialog, ReportNameDialogData } from '../dialogs/report-name-dialog';
import { ReportScheduleDialog, ReportScheduleDialogData } from '../dialogs/report-schedule-dialog';
import { ReportsStore } from '../state/reports.store';

const text = messages.reports;

export function formatterFor(unit: ReportUnit): ValueFormat {
  switch (unit) {
    case 'money':
      return (value) => formatBrl(cents(value));
    case 'percent':
      return (value) => text.percent(value);
    case 'count':
      return (value) => value.toLocaleString('pt-BR');
  }
}

export function displayDate(date: string): string {
  const [year, month, day] = date.split('-');
  return `${day}/${month}/${year}`;
}

@Component({
  selector: 'app-reports-page',
  imports: [
    AssistantPanel,
    UiAiBadge,
    UiButton,
    UiCard,
    UiChart,
    UiControl,
    UiField,
    UiPendingNotice,
    UiSkeleton,
  ],
  templateUrl: './reports-page.html',
  styleUrl: './reports-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsPage {
  protected readonly store = inject(ReportsStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);

  protected readonly text = text;
  protected readonly screen = messages.screens.reports;
  protected readonly pendingText = messages.pending.reports;
  protected readonly measures = REPORT_MEASURES;
  protected readonly groupings = REPORT_GROUPINGS;
  protected readonly periods = REPORT_PERIODS;
  protected readonly charts = REPORT_CHARTS;
  protected readonly formats: readonly ExportFormat[] = ['xlsx', 'pdf'];
  protected readonly title = computed(() => {
    const definition = this.store.definition();
    return text.title(text.measures[definition.measure], text.groupings[definition.groupBy]);
  });
  protected readonly format = computed(() => formatterFor(this.store.result()?.unit ?? 'count'));
  protected readonly chartData = computed<ChartData>(() => {
    const rows = this.store.result()?.rows ?? [];
    return {
      categories: rows.map((row) => row.label),
      series: [
        {
          name: text.measures[this.store.definition().measure],
          tone: 'accent',
          values: rows.map((row) => row.value),
        },
      ],
    };
  });
  protected readonly range = computed(() => {
    const result = this.store.result();
    return result ? text.range(displayDate(result.from), displayDate(result.to)) : null;
  });
  protected readonly total = computed(() => {
    const total = this.store.result()?.total;
    return total === null || total === undefined ? null : text.total(this.format()(total));
  });
  protected readonly pending = computed(
    () => this.store.state() === 'pending' || this.store.savedState() === 'pending',
  );

  constructor() {
    void this.store.load();
  }

  protected allowed(grouping: ReportGrouping): boolean {
    return this.store.groupings().includes(grouping);
  }

  protected setMeasure(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const measure = REPORT_MEASURES.find((item) => item === value);
    if (measure) {
      this.store.setMeasure(measure);
    }
  }

  protected setGrouping(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const groupBy = REPORT_GROUPINGS.find((item) => item === value);
    if (groupBy) {
      this.store.update({ groupBy });
    }
  }

  protected setPeriod(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const preset = REPORT_PERIODS.find((item) => item === value);
    if (preset) {
      const current = this.store.definition().period;
      this.store.update({
        period:
          preset === 'custom'
            ? { preset, from: current.from, to: current.to }
            : { preset, from: null, to: null },
      });
    }
  }

  protected setDate(key: 'from' | 'to', event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.store.update({
      period: { ...this.store.definition().period, [key]: value === '' ? null : value },
    });
  }

  protected setChart(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const chart = REPORT_CHARTS.find((item) => item === value);
    if (chart) {
      this.store.update({ chart });
    }
  }

  protected async saveReport(): Promise<void> {
    const name = await this.openName(null);
    if (name) {
      this.toast.show(text.save.saved(name), { tone: 'success' });
    }
  }

  protected async rename(report: SavedReport): Promise<void> {
    const name = await this.openName(report);
    if (name) {
      this.toast.show(text.save.renamed(name), { tone: 'success' });
    }
  }

  protected open(report: SavedReport): void {
    this.store.apply(report.definition, { id: report.id });
  }

  protected async remove(report: SavedReport): Promise<void> {
    const ref = this.dialogs.open<UiConfirmDialog, ConfirmDialogData, boolean>(UiConfirmDialog, {
      title: text.saved.removeTitle,
      body: text.saved.removeBody(report.name),
      confirm: text.saved.removeConfirm,
      danger: true,
    });
    if ((await firstValueFrom(ref.closed)) !== true) {
      return;
    }

    const outcome = await this.store.remove(report);
    this.toast.show(outcome.ok ? text.saved.removed(report.name) : outcome.error, {
      tone: outcome.ok ? 'success' : 'error',
    });
  }

  protected async schedule(report: SavedReport): Promise<void> {
    const ref = this.dialogs.open<
      ReportScheduleDialog,
      ReportScheduleDialogData,
      'saved' | 'removed' | null
    >(ReportScheduleDialog, { store: this.store, report });
    const result = await firstValueFrom(ref.closed);
    if (result) {
      this.toast.show(
        result === 'saved' ? text.schedule.saved(report.name) : text.schedule.removed(report.name),
        { tone: 'success' },
      );
    }
  }

  protected scheduleText(report: SavedReport): string {
    const schedule = report.schedule;
    return schedule
      ? text.saved.scheduled(text.weekdays[schedule.weekday], schedule.time)
      : text.saved.notScheduled;
  }

  private async openName(report: SavedReport | null): Promise<string | null> {
    const ref = this.dialogs.open<ReportNameDialog, ReportNameDialogData, string | null>(
      ReportNameDialog,
      { store: this.store, report },
    );
    return (await firstValueFrom(ref.closed)) ?? null;
  }
}
