import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { formatBrl } from '../../../shared/util/money';
import { formatDocumentDate } from '../components/document-sheet';
import { DocumentVerification } from '../data/document.models';
import { DocumentsGateway } from '../data/documents.gateway';

type VerifyLoad = 'loading' | 'ready' | 'pending';

@Component({
  selector: 'app-verify-page',
  imports: [UiPendingNotice, UiSkeleton],
  template: `
    <main class="verify">
      <h1>{{ text.title }}</h1>
      <p class="code">{{ text.code(code() ?? '') }}</p>
      @switch (state()) {
        @case ('loading') {
          <ui-skeleton height="120px" />
        }
        @case ('pending') {
          <ui-pending-notice [message]="pendingText" />
        }
        @case ('ready') {
          @if (summary(); as valid) {
            <section class="result result--ok" data-testid="verify-valid">
              <h2>{{ text.valid }}</h2>
              <p>{{ valid.description }}</p>
              <p>{{ valid.shop }}</p>
              <p>{{ valid.total }}</p>
            </section>
          } @else {
            <section class="result result--bad" data-testid="verify-invalid">
              <h2>{{ text.invalid }}</h2>
              <p>{{ text.invalidText }}</p>
            </section>
          }
        }
      }
    </main>
  `,
  styles: `
    :host {
      display: block;
      min-height: 100dvh;
      background: var(--bg);
    }
    .verify {
      display: grid;
      gap: var(--space-3);
      max-width: 480px;
      margin: 0 auto;
      padding: var(--space-6) var(--space-4);
    }
    h1 {
      margin: 0;
      font-size: var(--text-xl);
    }
    .code {
      margin: 0;
      font-family: var(--f-mono);
      color: var(--muted);
    }
    .result {
      display: grid;
      gap: var(--space-1);
      padding: var(--space-4);
      border-radius: var(--radius-lg);
      p,
      h2 {
        margin: 0;
      }
    }
    .result--ok {
      border: 1px solid var(--ok);
      background: var(--ok-soft);
    }
    .result--bad {
      border: 1px solid var(--bad);
      background: var(--bad-soft);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyPage {
  readonly code = input<string | null | undefined>(null);

  private readonly gateway = inject(DocumentsGateway);

  protected readonly text = messages.documents.verify;
  protected readonly pendingText = messages.pending.documents;
  protected readonly state = signal<VerifyLoad>('loading');
  protected readonly result = signal<DocumentVerification | null>(null);
  protected readonly summary = computed(() => {
    const result = this.result();
    if (
      !result?.valid ||
      result.kind === null ||
      result.number === null ||
      result.issuedAt === null
    ) {
      return null;
    }

    return {
      description: this.text.validText(
        messages.documents.kinds[result.kind],
        result.number,
        formatDocumentDate(result.issuedAt),
      ),
      shop: this.text.shop(result.shopName ?? ''),
      total: result.total === null ? '' : this.text.total(formatBrl(result.total)),
    };
  });

  constructor() {
    effect(() => {
      const code = this.code() ?? '';
      untracked(() => void this.load(code));
    });
  }

  private async load(code: string): Promise<void> {
    this.state.set('loading');
    try {
      this.result.set(await firstValueFrom(this.gateway.verify(code)));
      this.state.set('ready');
    } catch (error) {
      this.result.set(null);
      this.state.set(isFeaturePending(error) ? 'pending' : 'ready');
    }
  }
}
