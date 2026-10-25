import { computed, DestroyRef, inject, Injectable, InjectionToken, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { ReportGateway } from '../data/report.gateway';
import {
  allowedGroupings,
  defaultDefinition,
  ExportFormat,
  ExportJob,
  ReportDefinition,
  ReportGrouping,
  ReportMeasure,
  ReportResult,
  ReportSchedule,
  SavedReport,
  validateDefinition,
} from '../data/report.models';

export const REPORT_EXPORT_POLL_MS = new InjectionToken<number>('REPORT_EXPORT_POLL_MS', {
  providedIn: 'root',
  factory: () => 1500,
});

export const REPORT_EXPORT_MAX_POLLS = 20;

export type ReportLoad = 'idle' | 'loading' | 'ready' | 'pending' | 'error';

export type ActionOutcome = { readonly ok: true } | { readonly ok: false; readonly error: string };

const text = messages.reports;

function failure(error: unknown): ActionOutcome {
  return { ok: false, error: isApiError(error) ? error.detail : messages.errors.unknown };
}

@Injectable()
export class ReportsStore {
  private readonly gateway = inject(ReportGateway);
  private readonly pollMs = inject(REPORT_EXPORT_POLL_MS);
  private timer: ReturnType<typeof setTimeout> | null = null;
  private run = 0;

  readonly definition = signal<ReportDefinition>(defaultDefinition());
  readonly result = signal<ReportResult | null>(null);
  readonly state = signal<ReportLoad>('idle');
  readonly error = signal<string | null>(null);
  readonly saved = signal<readonly SavedReport[]>([]);
  readonly savedState = signal<ReportLoad>('idle');
  readonly openId = signal<string | null>(null);
  readonly exportJob = signal<ExportJob | null>(null);
  readonly exportError = signal<string | null>(null);
  readonly exporting = signal(false);
  readonly fromAssistant = signal(false);
  readonly problems = computed(() => validateDefinition(this.definition()));
  readonly valid = computed(() => this.problems().length === 0);
  readonly groupings = computed(() => allowedGroupings(this.definition().measure));

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stopPolling());
  }

  async load(): Promise<void> {
    await Promise.all([this.refresh(), this.loadSaved()]);
  }

  update(change: Partial<ReportDefinition>): void {
    this.definition.update((current) => ({ ...current, ...change }));
    this.openId.set(null);
    this.fromAssistant.set(false);
    void this.refresh();
  }

  setMeasure(measure: ReportMeasure): void {
    const groupBy: ReportGrouping = allowedGroupings(measure).includes(this.definition().groupBy)
      ? this.definition().groupBy
      : 'month';
    this.update({ measure, groupBy });
  }

  apply(definition: ReportDefinition, options: { fromAssistant?: boolean; id?: string } = {}) {
    this.definition.set(definition);
    this.openId.set(options.id ?? null);
    this.fromAssistant.set(options.fromAssistant ?? false);
    void this.refresh();
  }

  async refresh(): Promise<void> {
    const ticket = ++this.run;
    if (!this.valid()) {
      this.result.set(null);
      this.state.set('idle');
      return;
    }

    this.state.set('loading');
    this.error.set(null);
    try {
      const result = await firstValueFrom(this.gateway.run(this.definition()));
      if (ticket === this.run) {
        this.result.set(result);
        this.state.set('ready');
      }
    } catch (error) {
      if (ticket === this.run) {
        this.result.set(null);
        this.state.set(isFeaturePending(error) ? 'pending' : 'error');
        this.error.set(isApiError(error) ? error.detail : text.loadError);
      }
    }
  }

  async loadSaved(): Promise<void> {
    this.savedState.set('loading');
    try {
      this.saved.set(await firstValueFrom(this.gateway.saved()));
      this.savedState.set('ready');
    } catch (error) {
      this.savedState.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }

  async save(name: string): Promise<ActionOutcome> {
    try {
      const saved = await firstValueFrom(this.gateway.save(name, this.definition()));
      this.saved.update((items) => [saved, ...items]);
      this.openId.set(saved.id);
      return { ok: true };
    } catch (error) {
      return failure(error);
    }
  }

  async rename(report: SavedReport, name: string): Promise<ActionOutcome> {
    try {
      this.put(await firstValueFrom(this.gateway.update(report.id, name, report.definition)));
      return { ok: true };
    } catch (error) {
      return failure(error);
    }
  }

  async remove(report: SavedReport): Promise<ActionOutcome> {
    try {
      await firstValueFrom(this.gateway.remove(report.id));
      this.saved.update((items) => items.filter((item) => item.id !== report.id));
      if (this.openId() === report.id) {
        this.openId.set(null);
      }

      return { ok: true };
    } catch (error) {
      return failure(error);
    }
  }

  async schedule(report: SavedReport, schedule: ReportSchedule | null): Promise<ActionOutcome> {
    try {
      this.put(await firstValueFrom(this.gateway.schedule(report.id, schedule)));
      return { ok: true };
    } catch (error) {
      return failure(error);
    }
  }

  async export(format: ExportFormat): Promise<void> {
    this.stopPolling();
    this.exporting.set(true);
    this.exportError.set(null);
    this.exportJob.set(null);
    try {
      const job = await firstValueFrom(this.gateway.export(this.definition(), format));
      this.exportJob.set(job);
      if (!job.large) {
        this.poll(job.id, 0);
      }
    } catch (error) {
      this.exportError.set(isApiError(error) ? error.detail : text.export.failed);
    } finally {
      this.exporting.set(false);
    }
  }

  private poll(id: string, attempt: number): void {
    if (attempt >= REPORT_EXPORT_MAX_POLLS) {
      return;
    }

    this.timer = setTimeout(async () => {
      try {
        const job = await firstValueFrom(this.gateway.exportStatus(id));
        this.exportJob.set(job);
        if (job.status === 'Queued' || job.status === 'Processing') {
          this.poll(id, attempt + 1);
        }
      } catch (error) {
        this.exportError.set(isApiError(error) ? error.detail : text.export.failed);
      }
    }, this.pollMs);
  }

  private stopPolling(): void {
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }

  private put(report: SavedReport): void {
    this.saved.update((items) => items.map((item) => (item.id === report.id ? report : item)));
  }
}
