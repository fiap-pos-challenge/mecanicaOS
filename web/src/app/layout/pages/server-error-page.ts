import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ErrorState } from '../../core/errors/error-state';
import { messages } from '../../core/i18n/messages.pt-BR';
import { AuthStore } from '../../features/auth/state/auth.store';
import { UiButton } from '../../shared/ui/button/button';

@Component({
  selector: 'app-server-error-page',
  imports: [RouterLink, UiButton],
  template: `
    <main class="status" id="main-content">
      <p class="brand" aria-hidden="true">mecanica<b>OS</b></p>
      <h1>{{ text.title }}</h1>
      <p class="description">{{ detail() }}</p>
      <dl class="trace">
        <dt>{{ text.traceLabel }}</dt>
        <dd>{{ traceId() ?? text.noTrace }}</dd>
      </dl>
      <div class="actions">
        <button uiButton variant="primary" type="button" (click)="retry()">{{ text.retry }}</button>
        <a uiButton [routerLink]="home()">{{ homeLabel }}</a>
      </div>
    </main>
  `,
  styleUrl: './status-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServerErrorPage {
  private readonly errors = inject(ErrorState);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthStore);

  protected readonly text = messages.pages.serverError;
  protected readonly homeLabel = messages.shell.home;
  protected readonly traceId = computed(() => this.errors.last()?.traceId ?? null);
  protected readonly detail = computed(() => this.text.description);
  protected readonly home = computed(() => this.auth.home());

  protected retry(): void {
    void this.router.navigateByUrl(this.errors.failedUrl() ?? this.auth.home());
  }
}
