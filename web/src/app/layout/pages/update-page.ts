import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { messages } from '../../core/i18n/messages.pt-BR';
import { VersionService } from '../../core/version/version.service';
import { UiButton } from '../../shared/ui/button/button';

@Component({
  selector: 'app-update-page',
  imports: [UiButton],
  template: `
    <main class="status" id="main-content">
      <p class="brand" aria-hidden="true">mecanica<b>OS</b></p>
      <h1>{{ text.title }}</h1>
      <p class="description">{{ text.description }}</p>
      <div class="actions">
        <button uiButton variant="primary" type="button" (click)="version.reload()">
          {{ text.reload }}
        </button>
      </div>
    </main>
  `,
  styleUrl: './status-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpdatePage {
  protected readonly version = inject(VersionService);
  protected readonly text = messages.pages.update;
}
