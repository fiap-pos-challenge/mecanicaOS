import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { MechanicStore } from '../state/mechanic.store';

@Component({
  selector: 'app-sync-banner',
  imports: [UiButton],
  template: `
    @if (!store.online() || store.queue().length > 0 || store.syncing()) {
      <div
        class="banner"
        [class.banner--offline]="!store.online()"
        role="status"
        data-testid="sync-banner"
      >
        <strong>{{ headline() }}</strong>
        @if (store.queue().length > 0) {
          <span>{{ text.pending(store.queue().length) }}</span>
          <ul>
            @for (item of store.queue(); track item.id) {
              <li>{{ store.label(item) }}</li>
            }
          </ul>
          @if (store.online() && !store.syncing()) {
            <button uiButton size="sm" type="button" (click)="store.flush()">
              {{ text.syncNow }}
            </button>
          }
        }
      </div>
    }
    @for (failure of store.failures(); track failure.id) {
      <div class="banner banner--failed" role="alert">
        <span>{{ failure.text }}</span>
        <button uiButton size="sm" type="button" (click)="store.dismissFailure(failure.id)">
          {{ text.dismiss }}
        </button>
      </div>
    }
  `,
  styles: `
    :host {
      display: grid;
      gap: var(--space-2);
    }
    .banner {
      display: grid;
      gap: var(--space-1);
      justify-items: start;
      padding: var(--space-3);
      border: 1px solid var(--accent);
      border-radius: var(--radius-md);
      background: var(--accent-soft);
      font-size: var(--text-sm);
    }
    .banner--offline {
      border-color: var(--amber);
      background: var(--amber-soft);
    }
    .banner--failed {
      border-color: var(--bad);
      background: var(--bad-soft);
    }
    ul {
      margin: 0;
      padding-left: var(--space-4);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SyncBanner {
  protected readonly store = inject(MechanicStore);
  protected readonly text = messages.mechanic.sync;
  protected readonly headline = computed(() =>
    !this.store.online()
      ? this.text.offline
      : this.store.syncing()
        ? this.text.syncing
        : this.text.online,
  );
}
