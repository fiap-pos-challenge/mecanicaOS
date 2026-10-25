import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { messages } from './core/i18n/messages.pt-BR';
import { ThemeService } from './core/theme/theme.service';
import { DEMO_MODE } from './demo/demo-mode';
import { VersionService } from './core/version/version.service';
import { UpdateBanner } from './layout/update-banner/update-banner';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, UpdateBanner],
  template: `
    <router-outlet />
    <app-update-banner />
    @if (demo) {
      <p class="demo-banner" role="note" data-testid="demo-banner">{{ demoText }}</p>
    }
  `,
  styles: `
    .demo-banner {
      position: fixed;
      bottom: var(--space-2);
      left: 50%;
      z-index: var(--z-toast);
      margin: 0;
      padding: var(--space-1) var(--space-3);
      border: 1px solid var(--amber);
      border-radius: 999px;
      background: var(--amber-soft);
      color: var(--fg);
      font-size: var(--text-sm);
      font-weight: 600;
      white-space: nowrap;
      transform: translateX(-50%);
      pointer-events: none;
    }
    @media (max-width: 760px) {
      .demo-banner {
        bottom: calc(var(--bottom-nav-height) + var(--space-2));
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly theme = inject(ThemeService);
  protected readonly demo = DEMO_MODE;
  protected readonly demoText = messages.demo.banner;

  constructor() {
    inject(VersionService).watch();
  }
}
