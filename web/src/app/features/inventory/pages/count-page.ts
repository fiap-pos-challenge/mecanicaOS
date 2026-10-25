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
import { toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDateTime } from '../../../shared/util/date';
import { AuthStore } from '../../auth/state/auth.store';
import { InventoryGateway } from '../data/inventory.gateway';
import { countDifference, countSummary, isValidQuantityFor } from '../data/inventory.math';
import { CountLine, StockCount } from '../data/inventory.models';
import { COUNT_TONES } from './inventory-display';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error' | 'pending';

const text = messages.inventory;

export function parseCounted(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const number = Number(String(value).replace(',', '.'));
  return Number.isFinite(number) ? number : Number.NaN;
}

export function isValidCounted(line: Pick<CountLine, 'type'>, counted: number | null): boolean {
  return counted === null || counted === 0 || isValidQuantityFor(line.type, counted);
}

@Component({
  selector: 'app-count-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    UiButton,
    UiControl,
    UiField,
    UiIcon,
    UiPendingNotice,
    UiSkeleton,
    UiStatusPill,
  ],
  templateUrl: './count-page.html',
  styleUrl: './count-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountPage {
  readonly id = input.required<string>();

  private readonly gateway = inject(InventoryGateway);
  private readonly auth = inject(AuthStore);
  private readonly toast = inject(ToastService);
  private request = 0;

  protected readonly text = text.count;
  protected readonly statuses = text.counts.statuses;
  protected readonly tones = COUNT_TONES;
  protected readonly pendingText = messages.pending.inventory;
  protected readonly icons = { back: ArrowLeftIcon };

  protected readonly state = signal<LoadState>('loading');
  protected readonly count = signal<StockCount | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly rejecting = signal(false);
  protected readonly counted = new FormArray<FormControl<string>>([]);
  protected readonly reason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(300)],
  });

  private readonly values = toSignal(this.counted.valueChanges, { initialValue: [] });
  protected readonly canApprove = computed(() => this.auth.can('inventory.approve'));
  protected readonly editable = computed(() => this.count()?.status === 'Open');
  protected readonly lines = computed<readonly CountLine[]>(() => {
    const values = this.values();
    return (this.count()?.lines ?? []).map((line, index) => {
      const counted = parseCounted(values[index] ?? this.counted.at(index)?.value);
      return {
        ...line,
        countedQuantity: counted === null || Number.isNaN(counted) ? null : counted,
      };
    });
  });
  protected readonly summary = computed(() => countSummary(this.lines()));

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
  }

  protected title(count: StockCount): string {
    return this.text.title(count.location);
  }

  protected opened(count: StockCount): string {
    return this.text.openedBy(count.openedBy, formatDateTime(count.openedAt));
  }

  protected decided(count: StockCount): string | null {
    return count.decidedBy && count.decidedAt
      ? this.text.decided(
          this.statuses[count.status],
          count.decidedBy,
          formatDateTime(count.decidedAt),
        )
      : null;
  }

  protected amount(value: number, line: CountLine): string {
    return text.quantity(value, line.unitOfMeasure);
  }

  protected difference(index: number): number | null {
    const line = this.lines()[index];
    return line ? countDifference(line) : null;
  }

  protected differenceText(index: number): string {
    const difference = this.difference(index);
    const line = this.lines()[index];
    if (difference === null || !line) {
      return this.text.notCounted;
    }

    if (difference === 0) {
      return this.text.noDifference;
    }

    return `${difference > 0 ? '+' : '−'}${this.amount(Math.abs(difference), line)}`;
  }

  protected invalid(index: number): boolean {
    const line = this.count()?.lines[index];
    const value = parseCounted(this.counted.at(index)?.value);
    return !!line && (Number.isNaN(value) || !isValidCounted(line, value));
  }

  protected async save(): Promise<void> {
    await this.run(() => this.persist(), this.text.saved);
  }

  protected async submit(): Promise<void> {
    if (!this.summary().complete) {
      this.error.set(this.text.incomplete);
      return;
    }

    await this.run(async () => {
      await this.persist();
      return firstValueFrom(this.gateway.submitCount(this.id()));
    }, this.text.submitted);
  }

  protected async approve(): Promise<void> {
    await this.run(() => firstValueFrom(this.gateway.approveCount(this.id())), this.text.approved);
  }

  protected async reject(): Promise<void> {
    if (this.reason.invalid) {
      this.reason.markAsTouched();
      return;
    }

    await this.run(
      () => firstValueFrom(this.gateway.rejectCount(this.id(), this.reason.value.trim())),
      this.text.rejected,
    );
    this.rejecting.set(false);
  }

  private async persist(): Promise<StockCount> {
    const lines = this.count()?.lines ?? [];
    if (lines.some((_, index) => this.invalid(index))) {
      throw new Error(this.text.invalid);
    }

    const payload = this.lines().map((line) => ({
      partId: line.partId,
      countedQuantity: line.countedQuantity,
    }));
    return firstValueFrom(this.gateway.saveCount(this.id(), payload));
  }

  private async run(action: () => Promise<StockCount>, success: string): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      this.fill(await action());
      this.toast.show(success, { tone: 'success' });
    } catch (error) {
      this.error.set(
        isApiError(error)
          ? error.detail
          : error instanceof Error && error.message
            ? error.message
            : messages.errors.unknown,
      );
    } finally {
      this.busy.set(false);
    }
  }

  private fill(count: StockCount): void {
    this.count.set(count);
    this.counted.clear({ emitEvent: false });
    for (const line of count.lines) {
      this.counted.push(
        new FormControl(line.countedQuantity === null ? '' : String(line.countedQuantity), {
          nonNullable: true,
        }),
        { emitEvent: false },
      );
    }

    if (count.status === 'Open') {
      this.counted.enable({ emitEvent: false });
    } else {
      this.counted.disable({ emitEvent: false });
    }

    this.counted.updateValueAndValidity();
  }

  private async load(id: string): Promise<void> {
    const current = ++this.request;
    this.state.set('loading');
    try {
      const count = await firstValueFrom(this.gateway.count(id));
      if (current === this.request) {
        this.fill(count);
        this.state.set('ready');
      }
    } catch (error) {
      if (current === this.request) {
        this.state.set(
          isFeaturePending(error)
            ? 'pending'
            : isApiError(error) && error.status === 404
              ? 'notFound'
              : 'error',
        );
      }
    }
  }
}
