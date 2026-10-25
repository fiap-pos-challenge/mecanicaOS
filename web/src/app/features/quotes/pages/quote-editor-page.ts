import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { BuilderAdd, QuoteBuilder } from '../components/quote-builder';
import { QuantityChange, QuoteItems } from '../components/quote-items';
import { QuotePricingPanel } from '../components/quote-pricing';
import { CompareVersionsDialog } from '../dialogs/compare-versions-dialog';
import { QuoteDocumentDialog } from '../dialogs/quote-document-dialog';
import { RejectQuoteDialog } from '../dialogs/reject-quote-dialog';
import { SendQuoteDialog } from '../dialogs/send-quote-dialog';
import {
  DELIVERY_TONES,
  deliveryStage,
  formatDateTime,
  quoteStage,
  STAGE_TONES,
} from '../quote-display';
import { QuoteEditorStore } from '../state/quote-editor.store';

@Component({
  selector: 'app-quote-editor-page',
  imports: [
    QuoteBuilder,
    QuoteItems,
    QuotePricingPanel,
    RouterLink,
    UiButton,
    UiCard,
    UiIcon,
    UiPendingNotice,
    UiPlate,
    UiSkeleton,
    UiStatusPill,
  ],
  providers: [QuoteEditorStore],
  templateUrl: './quote-editor-page.html',
  styleUrl: './quote-editor-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuoteEditorPage {
  readonly id = input<string | null | undefined>(null);

  protected readonly store = inject(QuoteEditorStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);

  protected readonly text = messages.quotes;
  protected readonly pendingText = messages.pending.quotes;
  protected readonly icons = { back: ArrowLeftIcon };
  protected readonly stage = computed(() => {
    const quote = this.store.quote();
    if (!quote) {
      return null;
    }

    const stage = quoteStage(quote.status, quote.delivery?.status ?? null);
    return { label: this.text.statuses[stage], tone: STAGE_TONES[stage] };
  });
  protected readonly delivery = computed(() => {
    const delivery = this.store.quote()?.delivery;
    if (!delivery) {
      return null;
    }

    const stage = deliveryStage(delivery);
    const date = formatDateTime(delivery.expiresAt);
    return {
      delivery,
      stage,
      label: this.text.deliveryStatuses[stage],
      tone: DELIVERY_TONES[stage],
      channel: this.text.delivery.channel(this.text.channels[delivery.channel], delivery.attempts),
      expiry:
        stage === 'Expired' ? this.text.delivery.expired(date) : this.text.delivery.expires(date),
      canRetry: stage === 'Failed' || stage === 'Expired',
    };
  });
  protected readonly blocks = computed(() =>
    this.store
      .blocks()
      .filter((block) => block !== 'Locked')
      .map((block) => this.text.editor.blocks[block]),
  );
  protected readonly saveText = computed(() => this.text.editor.saveStates[this.store.saveState()]);
  protected readonly isPending = computed(() => this.store.quote()?.status === 'Pending');

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.store.load(id ?? ''));
    });
  }

  protected add(event: BuilderAdd): void {
    this.store.addOption(event.option, event.source);
  }

  protected setQuantity(change: QuantityChange): void {
    this.store.setQuantity(change.lineId, change.quantity);
  }

  protected async requestApproval(): Promise<void> {
    if (await this.store.requestApproval()) {
      this.toast.show(this.text.discount.approvalRequested, { tone: 'success' });
    }
  }

  protected async decide(approve: boolean): Promise<void> {
    if (await this.store.decideApproval(approve)) {
      this.toast.show(this.text.discount.decided, { tone: 'success' });
    }
  }

  protected openSend(): void {
    this.dialogs.open(SendQuoteDialog, this.store);
  }

  protected openDocument(): void {
    this.dialogs.open(QuoteDocumentDialog, this.store);
  }

  protected openVersions(): void {
    this.dialogs.open(CompareVersionsDialog, this.store);
  }

  protected async openReject(): Promise<void> {
    const done = await firstValueFrom(
      this.dialogs.open<RejectQuoteDialog, QuoteEditorStore, boolean>(RejectQuoteDialog, this.store)
        .closed,
    );
    if (done) {
      this.toast.show(this.text.reject.done, { tone: 'success' });
    }
  }

  protected async approve(): Promise<void> {
    if (await this.store.approve()) {
      this.toast.show(this.text.editor.approved, { tone: 'success' });
    }
  }

  protected async createVersion(): Promise<void> {
    const version = (this.store.quote()?.version ?? 0) + 1;
    const id = await this.store.createVersion();
    if (id) {
      await this.router.navigateByUrl(`/quotes/${id}`);
      this.toast.show(this.text.editor.versionCreated(version), { tone: 'success' });
    }
  }

  protected async retry(): Promise<void> {
    if (await this.store.retryDelivery()) {
      this.toast.show(this.text.delivery.retried, { tone: 'success' });
    }
  }

  protected async copyLink(link: string): Promise<void> {
    const clipboard = this.document.defaultView?.navigator.clipboard;
    if (clipboard) {
      await clipboard.writeText(link).catch(() => undefined);
      this.toast.show(this.text.send.copied, { tone: 'success' });
    }
  }
}
