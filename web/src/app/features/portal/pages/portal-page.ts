import { DOCUMENT } from '@angular/common';
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
import { Lock as LockIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { PhotoSource } from '../../../shared/ui/photo/photo-source';
import { UiPhoto } from '../../../shared/ui/photo/photo';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { ToastService } from '../../../shared/ui/toast/toast';
import { APP_ENVIRONMENT } from '../../../core/config/app-environment';
import { formatBrl } from '../../../shared/util/money';
import { DocumentDialog } from '../../documents/components/document-dialog';
import { formatDocumentDate } from '../../documents/components/document-sheet';
import { providePortalData } from '../data/portal.gateway';
import { PortalPhotoSource } from '../state/portal-photo-source';
import { PortalStore } from '../state/portal.store';

const timeFormatter = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
});

@Component({
  selector: 'app-portal-page',
  imports: [UiButton, UiControl, UiField, UiIcon, UiPendingNotice, UiPhoto, UiPlate, UiSkeleton],
  providers: [
    providePortalData(),
    PortalStore,
    { provide: PhotoSource, useClass: PortalPhotoSource },
  ],
  templateUrl: './portal-page.html',
  styleUrl: './portal-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalPage {
  readonly token = input<string | null | undefined>(null);
  readonly payment = input<string | null | undefined>(null);

  protected readonly store = inject(PortalStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);
  private readonly page = inject(DOCUMENT);
  private readonly providers = inject(APP_ENVIRONMENT).paymentProviderOrigins;

  protected readonly text = messages.portal;
  protected readonly documentText = messages.documents;
  protected readonly pendingText = messages.pending.portal;
  protected readonly icons = { lock: LockIcon };
  protected readonly digits = signal('');
  protected readonly rejecting = signal(false);
  protected readonly reason = signal('');
  protected readonly reasonInvalid = signal(false);
  protected readonly steps = computed(() =>
    this.store.steps().map((step) => ({
      ...step,
      label: this.text.steps[step.status],
      at: step.at ? timeFormatter.format(new Date(step.at)) : null,
    })),
  );
  protected readonly items = computed(() =>
    (this.store.order()?.quote?.items ?? []).map((item) => ({
      ...item,
      totalText: formatBrl(item.total),
      checked: this.store.selected().has(item.id),
    })),
  );
  protected readonly totals = computed(() => {
    const totals = this.store.totals();
    return totals
      ? { discount: `− ${formatBrl(totals.discount)}`, total: formatBrl(totals.total) }
      : null;
  });
  protected readonly paymentView = computed(() => {
    const payment = this.store.order()?.payment;
    return payment
      ? {
          ...payment,
          statusText: this.text.paymentStatus[payment.status],
          due: this.text.amountDue(formatBrl(payment.amountDue)),
          open: payment.status === 'Pending' && payment.amountDue > 0,
        }
      : null;
  });
  protected readonly documents = computed(() =>
    (this.store.order()?.documents ?? []).map((document) => ({
      ...document,
      label: this.documentText.kinds[document.kind],
      date: formatDocumentDate(document.issuedAt),
    })),
  );

  constructor() {
    effect(() => {
      const token = this.token() ?? '';
      untracked(() => void this.store.start(token));
    });
    effect(() => {
      const result = this.payment() ?? null;
      untracked(() => this.store.cardReturned(result));
    });
  }

  protected setDigits(event: Event): void {
    this.digits.set((event.target as HTMLInputElement).value.replace(/\D/g, '').slice(0, 3));
  }

  protected submitDigits(event: Event): void {
    event.preventDefault();
    void this.store.confirm(this.digits());
  }

  protected toggle(id: string, event: Event): void {
    this.store.toggle(id, (event.target as HTMLInputElement).checked);
  }

  protected setReason(event: Event): void {
    this.reason.set((event.target as HTMLTextAreaElement).value);
    this.reasonInvalid.set(false);
  }

  protected async submitReject(event: Event): Promise<void> {
    event.preventDefault();
    if (this.reason().trim().length < 5) {
      this.reasonInvalid.set(true);
      return;
    }

    if (await this.store.reject(this.reason())) {
      this.rejecting.set(false);
    }
  }

  protected async copyPix(code: string): Promise<void> {
    await this.page.defaultView?.navigator.clipboard?.writeText(code).catch(() => undefined);
    this.toast.show(this.text.pixCopied, { tone: 'success' });
  }

  protected async payCard(): Promise<void> {
    const url = await this.store.payCard();
    if (!url) {
      return;
    }

    if (isSafeRedirect(url, this.providers)) {
      this.page.defaultView?.location.assign(url);
    } else {
      this.toast.show(this.text.providerBlocked, { tone: 'error' });
    }
  }

  protected openDocument(code: string): void {
    this.dialogs.open(DocumentDialog, { code, portalSession: this.store.session() ?? undefined });
  }
}

export function isSafeRedirect(url: string, allowed: readonly string[]): boolean {
  try {
    const target = new URL(url);
    return target.protocol === 'https:' && allowed.includes(target.origin);
  } catch {
    return false;
  }
}
