import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { TabItem, UiTabs } from '../../../shared/ui/tabs/tabs';
import { DiscountsTab } from '../components/discounts-tab';
import { FiscalTab } from '../components/fiscal-tab';
import { MessagesTab } from '../components/messages-tab';
import { PaymentsTab } from '../components/payments-tab';
import { SecurityTab } from '../components/security-tab';
import { WorkshopTab } from '../components/workshop-tab';
import { SettingsStore } from '../state/settings.store';

type SettingsTabId = keyof typeof messages.settings.tabs;

@Component({
  selector: 'app-settings-page',
  imports: [
    DiscountsTab,
    FiscalTab,
    MessagesTab,
    PaymentsTab,
    SecurityTab,
    UiPendingNotice,
    UiTabs,
    WorkshopTab,
  ],
  template: `
    <header class="head">
      <h1 class="page-title">{{ screen }}</h1>
      @if (store.workshopName(); as name) {
        <p class="sub">{{ text.subtitle(name, store.planName() ?? '') }}</p>
      }
    </header>
    @if (store.pending()) {
      <ui-pending-notice class="block" [message]="pendingText" />
    }
    @if (!store.canManage) {
      <p class="notice block" role="note">{{ text.readOnly }}</p>
    }
    <ui-tabs [tabs]="tabs()" [label]="text.tabsLabel" [(selected)]="selected">
      @switch (selected()) {
        @case ('workshop') {
          <app-workshop-tab />
        }
        @case ('discounts') {
          <app-discounts-tab />
        }
        @case ('payments') {
          <app-payments-tab />
        }
        @case ('messages') {
          <app-messages-tab />
        }
        @case ('security') {
          <app-security-tab />
        }
        @case ('fiscal') {
          <app-fiscal-tab />
        }
      }
    </ui-tabs>
  `,
  styles: `
    :host {
      display: block;
      padding-top: var(--space-2);
    }
    .head {
      margin-bottom: var(--space-4);
    }
    .page-title {
      margin: 0;
    }
    .sub {
      margin: var(--space-1) 0 0;
      color: var(--muted);
      font-size: var(--text-sm);
    }
    .block {
      display: block;
      margin-bottom: var(--space-4);
    }
    .notice {
      padding: var(--space-3);
      border: 1px solid var(--amber);
      border-radius: var(--radius-md);
      background: var(--amber-soft);
      font-size: var(--text-sm);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsPage {
  protected readonly store = inject(SettingsStore);
  protected readonly text = messages.settings;
  protected readonly screen = messages.screens.settings;
  protected readonly pendingText = messages.pending.settings;
  protected readonly selected = signal<string>('workshop');
  protected readonly tabs = computed<readonly TabItem[]>(() =>
    (Object.keys(this.text.tabs) as SettingsTabId[])
      .filter((id) => id !== 'security' || this.store.canManage)
      .map((id) => ({ id, label: this.text.tabs[id] })),
  );
}
