import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RefreshCw as RefreshIcon } from 'lucide';
import { messages } from '../../core/i18n/messages.pt-BR';
import { VersionService } from '../../core/version/version.service';
import { UiButton } from '../../shared/ui/button/button';
import { UiIcon } from '../../shared/ui/icon/icon';

@Component({
  selector: 'app-update-banner',
  imports: [UiButton, UiIcon],
  template: `
    <div class="banner" role="status">
      @if (version.outdated()) {
        <ui-icon [icon]="icon" [size]="16" />
        <span>{{ text.banner }}</span>
        <button uiButton size="sm" variant="primary" type="button" (click)="version.reload()">
          {{ text.reload }}
        </button>
      }
    </div>
  `,
  styles: `
    .banner:empty {
      display: none;
    }

    .banner {
      position: fixed;
      inset: auto var(--space-4) calc(var(--space-4) + env(safe-area-inset-bottom, 0px)) auto;
      z-index: var(--z-toast);
      display: flex;
      align-items: center;
      gap: var(--space-3);
      max-width: calc(100vw - 2 * var(--space-4));
      padding: var(--space-3) var(--space-4);
      border: 1px solid var(--accent);
      border-radius: var(--radius-lg);
      background: var(--surface);
      box-shadow: var(--shadow-overlay);
      color: var(--fg);
      font-size: var(--text-sm);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpdateBanner {
  protected readonly version = inject(VersionService);
  protected readonly text = messages.pages.update;
  protected readonly icon = RefreshIcon;
}
