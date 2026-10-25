import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { FiscalIntegration } from '../data/settings.models';
import { SectionLoad, SettingsStore } from '../state/settings.store';

@Component({
  selector: 'app-fiscal-tab',
  imports: [UiButton, UiSkeleton, UiStatusPill],
  template: `
    <section class="section" [attr.aria-label]="text.title">
      <h2 class="title">{{ text.title }}</h2>
      <p class="hint">{{ text.hint }}</p>
      @switch (state()) {
        @case ('loading') {
          <ui-skeleton height="80px" />
        }
        @case ('error') {
          <div class="alert" role="alert">
            <span>{{ common.loadError }}</span>
            <button uiButton size="sm" type="button" (click)="load()">{{ common.retry }}</button>
          </div>
        }
      }
      <ul class="list">
        @for (item of integrations(); track item.kind) {
          <li class="row">
            <strong>{{ text.kinds[item.kind] }}</strong>
            <ui-status-pill [tone]="item.status === 'Active' ? 'ok' : 'neutral'">
              {{ text.statuses[item.status] }}
            </ui-status-pill>
            <span class="hint">{{ text.provider(item.provider) }}</span>
          </li>
        }
      </ul>
    </section>
  `,
  styleUrl: './settings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FiscalTab {
  private readonly store = inject(SettingsStore);
  protected readonly text = messages.settings.fiscal;
  protected readonly common = messages.settings;
  protected readonly state = signal<SectionLoad>('loading');
  protected readonly integrations = signal<readonly FiscalIntegration[]>([]);

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.integrations.set((await this.store.load(this.store.gateway.fiscal(), this.state)) ?? []);
  }
}
