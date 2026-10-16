import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { RealtimeGateway } from '../../../core/realtime/realtime.gateway';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatBrl } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { provideServiceOrderData, ServiceOrderGateway } from '../data/service-order.gateway';
import { ServiceOrderDetail, ServiceOrderStatus } from '../data/service-order.models';
import {
  ChangedField,
  changedFields,
  checkTransition,
  nextStatuses,
  tagsOf,
} from '../data/service-order.rules';
import { DocumentDialog } from '../../documents/components/document-dialog';
import { ReasonDialog } from '../dialogs/reason-dialog';
import { fieldValue, formatWhen, STATUS_TONES, TAG_TONES } from '../service-order-display';
import { errorText, VERSION_CONFLICT_CODE } from '../state/board.store';

type DetailLoad = 'loading' | 'ready' | 'pending' | 'notFound' | 'error';

@Component({
  selector: 'app-service-order-detail-page',
  imports: [
    RouterLink,
    UiButton,
    UiCard,
    UiIcon,
    UiPendingNotice,
    UiPlate,
    UiSkeleton,
    UiStatusPill,
  ],
  providers: [provideServiceOrderData()],
  templateUrl: './service-order-detail-page.html',
  styleUrl: './service-order-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceOrderDetailPage {
  readonly id = input<string | null | undefined>(null);

  private readonly gateway = inject(ServiceOrderGateway);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthStore);

  protected readonly text = messages.serviceOrders;
  protected readonly detailText = messages.serviceOrders.detail;
  protected readonly pendingText = messages.pending.serviceOrders;
  protected readonly icons = { back: ArrowLeftIcon };
  protected readonly state = signal<DetailLoad>('loading');
  protected readonly order = signal<ServiceOrderDetail | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly change = signal<{
    readonly conflict: boolean;
    readonly lines: readonly string[];
  } | null>(null);
  protected readonly canManage = computed(() => this.auth.can('serviceOrders.manage'));
  protected readonly canClock = computed(() => this.auth.can('mechanic.view'));
  protected readonly status = computed(() => {
    const order = this.order();
    return order
      ? { label: this.text.statuses[order.status], tone: STATUS_TONES[order.status] }
      : null;
  });
  protected readonly tags = computed(() => {
    const order = this.order();
    return order
      ? tagsOf(order, Date.now()).map((tag) => ({
          tag,
          label: this.text.tags[tag],
          tone: TAG_TONES[tag],
        }))
      : [];
  });
  protected readonly actions = computed(() => {
    const order = this.order();
    return order
      ? nextStatuses(order.status).map((status) => {
          const check = checkTransition(order, status);
          return {
            status,
            label: this.text.board.moveTo(this.text.statuses[status]),
            allowed: check.allowed,
            reason: check.allowed ? null : this.text.board.blocked[check.block],
          };
        })
      : [];
  });
  protected readonly history = computed(() =>
    [...(this.order()?.history ?? [])].reverse().map((entry) => ({
      ...entry,
      label: this.text.statuses[entry.to],
      when: this.detailText.timelineEntry(entry.changedBy, formatWhen(entry.changedAt)),
    })),
  );
  protected readonly quote = computed(() => {
    const quote = this.order()?.quote;
    return quote
      ? {
          ...quote,
          status: this.detailText.quoteStatus[quote.status],
          totalText: formatBrl(quote.total),
          items: quote.items.map((item) => ({ ...item, totalText: formatBrl(item.total) })),
        }
      : null;
  });
  protected readonly time = computed(() => {
    const entries = this.order()?.timeEntries ?? [];
    return {
      entries: entries.map((entry) => ({
        ...entry,
        when: `${formatWhen(entry.startedAt)} – ${entry.endedAt ? formatWhen(entry.endedAt) : this.detailText.running}`,
      })),
      total: entries.reduce((sum, entry) => sum + entry.minutes, 0),
      running: entries.some((entry) => entry.endedAt === null),
    };
  });
  protected readonly promised = computed(() => {
    const promisedAt = this.order()?.promisedAt ?? null;
    return promisedAt === null ? this.text.noPromise : this.text.promised(formatWhen(promisedAt));
  });
  protected readonly mileage = computed(() =>
    this.detailText.mileage(new Intl.NumberFormat('pt-BR').format(this.order()?.mileage ?? 0)),
  );

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id ?? ''));
    });
    const realtime = inject(RealtimeGateway);
    const subscription = realtime.events$.subscribe((event) => {
      if (event.type === 'serviceOrderStatusChanged' && event.serviceOrderId === this.order()?.id) {
        void this.reload(false);
      }
    });
    inject(DestroyRef).onDestroy(() => subscription.unsubscribe());
  }

  protected async load(id: string): Promise<void> {
    this.state.set('loading');
    this.change.set(null);
    try {
      this.order.set(await firstValueFrom(this.gateway.get(id)));
      this.state.set('ready');
    } catch (error) {
      this.state.set(
        isFeaturePending(error)
          ? 'pending'
          : isApiError(error) && error.status === 404
            ? 'notFound'
            : 'error',
      );
    }
  }

  private async reload(conflict: boolean): Promise<void> {
    const before = this.order();
    if (!before) {
      return;
    }

    const after = await firstValueFrom(this.gateway.get(before.id));
    const fields = changedFields(before, after);
    this.order.set(after);
    if (conflict || fields.length > 0) {
      this.change.set({ conflict, lines: this.describe(before, after, fields) });
    }
  }

  private describe(
    before: ServiceOrderDetail,
    after: ServiceOrderDetail,
    fields: readonly ChangedField[],
  ): readonly string[] {
    return fields.map((field) =>
      this.text.changes.change(
        this.text.changes.fields[field],
        fieldValue(before, field),
        fieldValue(after, field),
      ),
    );
  }

  protected async move(to: ServiceOrderStatus): Promise<void> {
    const order = this.order();
    if (!order) {
      return;
    }

    let reason: string | null = null;
    if (
      checkTransition(order, to).allowed &&
      order.status === 'WaitingApproval' &&
      to === 'InDiagnosis'
    ) {
      reason =
        (await firstValueFrom(
          this.dialogs.open<ReasonDialog, null, string>(ReasonDialog).closed,
        )) ?? null;
      if (reason === null) {
        return;
      }
    }

    await this.run(async () => {
      this.order.set(
        await firstValueFrom(
          this.gateway.transition(order.id, { to, version: order.version, reason }),
        ),
      );
      this.toast.show(this.text.board.moved(order.number, this.text.statuses[to]), {
        tone: 'success',
      });
    });
  }

  protected openDocument(code: string): void {
    this.dialogs.open(DocumentDialog, { code, allowSend: this.canManage() });
  }

  protected async toggleTime(): Promise<void> {
    const order = this.order();
    if (!order) {
      return;
    }

    await this.run(async () => {
      this.order.set(
        await firstValueFrom(
          this.time().running ? this.gateway.stopTime(order.id) : this.gateway.startTime(order.id),
        ),
      );
    });
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await action();
    } catch (error) {
      if (isApiError(error) && error.code === VERSION_CONFLICT_CODE) {
        await this.reload(true);
      } else {
        this.error.set(errorText(error));
      }
    } finally {
      this.busy.set(false);
    }
  }
}
