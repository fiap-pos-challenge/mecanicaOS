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
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon, X as XIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { applyApiErrors, serverErrorOf } from '../../../core/forms/apply-api-errors';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { MAX_PAGE_SIZE } from '../../../core/data/page';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiIconButton } from '../../../shared/ui/icon-button/icon-button';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { cents } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { PriceHistory } from '../components/price-history';
import { CatalogGateway } from '../data/catalog.gateway';
import { CatalogPart, CatalogService, UsualPart } from '../data/catalog.models';
import {
  amountText,
  formatMoneyControl,
  moneyOf,
  moneyValidator,
  numberRange,
  optionalText,
  quantityValidator,
} from './catalog-form';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error' | 'pending';

const text = messages.catalog.form;

function usualPartGroup(part: UsualPart) {
  return new FormGroup({
    partId: new FormControl(part.partId, { nonNullable: true }),
    quantity: new FormControl<number | null>(part.quantity, {
      validators: [Validators.required, quantityValidator],
    }),
  });
}

@Component({
  selector: 'app-service-form-page',
  imports: [
    PriceHistory,
    ReactiveFormsModule,
    RouterLink,
    UiButton,
    UiCard,
    UiControl,
    UiField,
    UiIcon,
    UiPendingNotice,
    UiIconButton,
    UiSkeleton,
    UiStatusPill,
  ],
  templateUrl: './service-form-page.html',
  styleUrls: ['./catalog-form-page.scss', './catalog-lines.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceFormPage {
  readonly id = input<string | null | undefined>(null);

  private readonly gateway = inject(CatalogGateway);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly text = text;
  protected readonly service = text.services;
  protected readonly icons = { back: ArrowLeftIcon, remove: XIcon };

  protected readonly state = signal<LoadState>('loading');
  protected readonly item = signal<CatalogService | null>(null);
  protected readonly parts = signal<readonly CatalogPart[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly historyVersion = signal(0);
  protected readonly optionsPending = signal(false);
  protected readonly pendingText = messages.pending.catalog;
  protected readonly editing = computed(() => Boolean(this.id()));
  protected readonly canManage = computed(() => this.auth.can('catalog.manage'));

  protected readonly partChoice = new FormControl('', { nonNullable: true });
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(500)],
    }),
    basePrice: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, moneyValidator],
    }),
    estimatedMinutes: new FormControl<number | null>(30, {
      validators: [Validators.required, numberRange(1, 10_000, true)],
    }),
    commissionPercent: new FormControl<number | null>(10, {
      validators: [Validators.required, numberRange(0, 50)],
    }),
    warrantyDays: new FormControl<number | null>(90, {
      validators: [Validators.required, numberRange(0, 3650, true)],
    }),
    usualParts: new FormArray<ReturnType<typeof usualPartGroup>>([]),
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id ?? null));
    });
  }

  protected title(): string {
    return this.editing() ? this.service.editTitle : this.service.newTitle;
  }

  protected partName(partId: string): string {
    return this.parts().find((part) => part.id === partId)?.name ?? partId;
  }

  protected availableParts(): readonly CatalogPart[] {
    const chosen = new Set(this.form.controls.usualParts.getRawValue().map((part) => part.partId));
    return this.parts().filter((part) => part.isActive && !chosen.has(part.id));
  }

  protected addPart(): void {
    const partId = this.partChoice.value;
    if (!partId) {
      return;
    }

    this.form.controls.usualParts.push(usualPartGroup({ partId, quantity: 1 }));
    this.form.markAsDirty();
    this.partChoice.setValue('');
  }

  protected removePart(index: number): void {
    this.form.controls.usualParts.removeAt(index);
    this.form.markAsDirty();
  }

  protected formatPrice(): void {
    formatMoneyControl(this.form.controls.basePrice);
  }

  protected quantityInvalid(index: number): boolean {
    const control = this.form.controls.usualParts.at(index).controls.quantity;
    return control.touched && control.invalid;
  }

  protected errorOf(
    name: 'name' | 'basePrice' | 'estimatedMinutes' | 'commissionPercent' | 'warrantyDays',
  ): string | null {
    const control = this.form.controls[name];
    if (!control.touched || control.valid) {
      return null;
    }

    const server = serverErrorOf(control);
    if (server) {
      return server;
    }

    if (control.hasError('required')) {
      return text.required;
    }

    return {
      name: text.invalidName,
      basePrice: text.invalidMoney,
      estimatedMinutes: text.invalidInteger,
      commissionPercent: text.invalidPercent(50),
      warrantyDays: text.invalidInteger,
    }[name];
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set(text.fixErrors);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    const input = {
      name: value.name.trim(),
      description: optionalText(value.description),
      basePrice: moneyOf(value.basePrice) ?? cents(0),
      estimatedMinutes: Number(value.estimatedMinutes),
      commissionPercent: Number(value.commissionPercent),
      warrantyDays: Number(value.warrantyDays),
      usualParts: value.usualParts.map((part) => ({
        partId: part.partId,
        quantity: Number(part.quantity),
      })),
    };
    try {
      const id = this.id();
      const saved = id
        ? await firstValueFrom(this.gateway.update('services', id, input))
        : await firstValueFrom(this.gateway.create('services', input));
      this.toast.show(text.saved, { tone: 'success' });
      if (id) {
        this.fill(saved);
        this.historyVersion.update((version) => version + 1);
      } else {
        await this.router.navigate(['/catalog', 'services', saved.id]);
      }
    } catch (error) {
      if (isApiError(error)) {
        const unmatched = applyApiErrors(this.form, error);
        this.error.set(
          error.fieldErrors.length > 0 && unmatched.length === 0 ? text.fixErrors : error.detail,
        );
      } else {
        this.error.set(messages.errors.unknown);
      }
    } finally {
      this.saving.set(false);
    }
  }

  protected async retry(): Promise<void> {
    await this.load(this.id() ?? null);
  }

  private fill(service: CatalogService): void {
    this.item.set(service);
    this.form.controls.usualParts.clear();
    for (const part of service.usualParts) {
      this.form.controls.usualParts.push(usualPartGroup(part));
    }

    this.form.reset({
      name: service.name,
      description: service.description ?? '',
      basePrice: amountText(service.basePrice),
      estimatedMinutes: service.estimatedMinutes,
      commissionPercent: service.commissionPercent,
      warrantyDays: service.warrantyDays,
      usualParts: service.usualParts.map((part) => ({ ...part })),
    });
  }

  private applyAccess(): void {
    if (this.canManage()) {
      this.form.enable();
      this.partChoice.enable();
    } else {
      this.form.disable();
      this.partChoice.disable();
    }
  }

  private async load(id: string | null): Promise<void> {
    this.state.set('loading');
    this.error.set(null);
    try {
      const [parts, service] = await Promise.all([
        this.loadParts(),
        id ? firstValueFrom(this.gateway.get('services', id)) : Promise.resolve(null),
      ]);
      this.optionsPending.set(parts === null);
      this.parts.set(parts ?? []);
      if (service) {
        this.fill(service);
      } else {
        this.item.set(null);
        this.form.controls.usualParts.clear();
        this.form.reset({ estimatedMinutes: 30, commissionPercent: 10, warrantyDays: 90 });
      }

      this.applyAccess();
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

  private async loadParts(): Promise<readonly CatalogPart[] | null> {
    try {
      const parts = await firstValueFrom(
        this.gateway.list('parts', { page: 1, pageSize: MAX_PAGE_SIZE, term: '', status: null }),
      );
      return parts.items;
    } catch (error) {
      if (isFeaturePending(error)) {
        return null;
      }

      throw error;
    }
  }
}
