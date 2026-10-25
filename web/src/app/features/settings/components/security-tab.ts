import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { Page } from '../../../core/data/page';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { TableColumn, UiTable } from '../../../shared/ui/table/table';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDateTime } from '../../../shared/util/date';
import {
  ActiveSession,
  AUDIT_AREAS,
  AuditArea,
  AuditEntry,
  AuditFilter,
  SecuritySettings,
  validateSecurity,
} from '../data/settings.models';
import { SectionLoad, SettingsStore } from '../state/settings.store';

@Component({
  selector: 'app-security-tab',
  imports: [UiButton, UiControl, UiField, UiSkeleton, UiTable],
  templateUrl: './security-tab.html',
  styleUrl: './settings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SecurityTab {
  protected readonly store = inject(SettingsStore);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.settings.security;
  protected readonly common = messages.settings;
  protected readonly areas = AUDIT_AREAS;
  protected readonly state = signal<SectionLoad>('loading');
  protected readonly sessionState = signal<SectionLoad>('loading');
  protected readonly auditState = signal<SectionLoad>('loading');
  protected readonly draft = signal<SecuritySettings | null>(null);
  protected readonly sessions = signal<readonly ActiveSession[]>([]);
  protected readonly audit = signal<Page<AuditEntry> | null>(null);
  protected readonly filter = signal<AuditFilter>({
    area: 'all',
    from: null,
    to: null,
    actor: '',
    page: 1,
  });
  protected readonly submitted = signal(false);
  protected readonly problems = computed(() => {
    const draft = this.draft();
    return draft ? validateSecurity(draft) : [];
  });
  protected readonly columns: readonly TableColumn<AuditEntry>[] = [
    { key: 'when', header: this.text.columns.when, value: (row) => formatDateTime(row.occurredAt) },
    { key: 'who', header: this.text.columns.who, value: (row) => row.actorName },
    { key: 'action', header: this.text.columns.action, value: (row) => row.action },
    { key: 'target', header: this.text.columns.target, value: (row) => row.target },
    { key: 'detail', header: this.text.columns.detail, value: (row) => row.detail },
  ];
  protected readonly trackEntry = (row: AuditEntry): string => row.id;

  constructor() {
    void this.load();
    void this.loadSessions();
    void this.search();
  }

  async load(): Promise<void> {
    const settings = await this.store.load(this.store.gateway.security(), this.state);
    if (settings) {
      this.draft.set(settings);
      this.store.applyIdle(settings.idleMinutes);
    }
  }

  async loadSessions(): Promise<void> {
    this.sessions.set(
      (await this.store.load(this.store.gateway.sessions(), this.sessionState)) ?? [],
    );
  }

  async search(): Promise<void> {
    this.audit.set(await this.store.load(this.store.gateway.audit(this.filter()), this.auditState));
  }

  protected when(date: string): string {
    return formatDateTime(date);
  }

  protected setNumber(
    key: 'idleMinutes' | 'lockoutAttempts' | 'lockoutMinutes',
    event: Event,
  ): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.draft.update((draft) => (draft ? { ...draft, [key]: value } : draft));
  }

  protected toggleTwoFactor(): void {
    this.draft.update((draft) =>
      draft ? { ...draft, requireTwoFactorForAll: !draft.requireTwoFactorForAll } : draft,
    );
  }

  protected setFilter(key: 'area' | 'from' | 'to' | 'actor', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.filter.update((filter) => {
      if (key === 'area') {
        return { ...filter, area: AUDIT_AREAS.find((area) => area === value) ?? 'all' };
      }

      if (key === 'actor') {
        return { ...filter, actor: value };
      }

      return { ...filter, [key]: value === '' ? null : value };
    });
  }

  protected async applyFilter(event: Event): Promise<void> {
    event.preventDefault();
    this.filter.update((filter) => ({ ...filter, page: 1 }));
    await this.search();
  }

  protected areaLabel(area: AuditArea): string {
    return this.text.areas[area];
  }

  protected async save(): Promise<void> {
    const draft = this.draft();
    this.submitted.set(true);
    if (!draft || this.problems().length > 0) {
      return;
    }

    const saved = await this.store.change(this.text.change, (token) =>
      this.store.gateway.saveSecurity(draft, token),
    );
    if (saved) {
      this.draft.set(saved);
      this.store.applyIdle(saved.idleMinutes);
      this.toast.show(this.common.saved, { tone: 'success' });
      await this.search();
    }
  }

  protected async end(session: ActiveSession): Promise<void> {
    const done = await this.store.change(this.text.endChange(session.userName), (token) =>
      this.store.gateway.endSession(session.id, token),
    );
    if (done !== null) {
      this.sessions.update((items) => items.filter((item) => item.id !== session.id));
      this.toast.show(this.text.ended, { tone: 'success' });
      await this.search();
    }
  }
}
