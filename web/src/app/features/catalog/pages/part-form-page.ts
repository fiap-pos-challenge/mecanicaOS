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
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { applyApiErrors, serverErrorOf } from '../../../core/forms/apply-api-errors';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { ConfirmDialogData, UiConfirmDialog } from '../../../shared/ui/dialog/confirm-dialog';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { cents, formatBrl } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { PriceHistory } from '../components/price-history';
import { CatalogGateway } from '../data/catalog.gateway';
import { isBelowCost, isValidBarcode, isValidPartCode, marginPercent } from '../data/catalog.math';
import { CatalogPart, PART_TYPES, PartType } from '../data/catalog.models';
import {
  amountText,
  formatMoneyControl,
  moneyOf,
  moneyValidator,
  numberRange,
  optionalText,
} from './catalog-form';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error' | 'pending';

const text = messages.catalog.form;

export function codeValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  return value === '' || isValidPartCode(value) ? null : { code: true };
}

export function barcodeValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').trim();
  return value === '' || isValidBarcode(value) ? null : { barcode: true };
}

@Component({
  selector: 'app-part-form-page',
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
    UiSegmented,
    UiSkeleton,
    UiStatusPill,
  ],
  templateUrl: './part-form-page.html',
  styleUrl: './catalog-form-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PartFormPage {
  readonly id = input<string | null | undefined>(null);

  private readonly gateway = inject(CatalogGateway);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);

  protected readonly text = text;
  protected readonly part = text.parts;
  protected readonly icons = { back: ArrowLeftIcon };
  protected readonly typeOptions: readonly SegmentedOption<PartType>[] = PART_TYPES.map((type) => ({
    value: type,
    label: messages.catalog.partTypes[type],
  }));

  protected readonly state = signal<LoadState>('loading');
  protected readonly item = signal<CatalogPart | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly historyVersion = signal(0);
  protected readonly optionsPending = signal(false);
  protected readonly pendingText = messages.pending.catalog;

  protected readonly editing = computed(() => Boolean(this.id()));
  protected readonly canManage = computed(() => this.auth.can('catalog.manage'));
  protected readonly canSeeCosts = computed(() => this.auth.can('catalog.viewCosts'));

  protected readonly form = new FormGroup({
    code: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, codeValidator],
    }),
    type: new FormControl<PartType>('Part', { nonNullable: true }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    }),
    unitOfMeasure: new FormControl('un', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(10), Validators.pattern(/\S/)],
    }),
    cost: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, moneyValidator],
    }),
    unitPrice: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, moneyValidator],
    }),
    minimumStock: new FormControl<number | null>(0, {
      validators: [Validators.required, numberRange(0, 1_000_000)],
    }),
    supplier: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(120)] }),
    barcode: new FormControl('', { nonNullable: true, validators: [barcodeValidator] }),
  });

  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.value });
  protected readonly margin = computed(() => {
    const value = this.values();
    const price = moneyOf(value.unitPrice);
    return price === null ? null : marginPercent(price, moneyOf(value.cost));
  });
  protected readonly belowCost = computed(() => {
    const value = this.values();
    const price = moneyOf(value.unitPrice);
    return price !== null && isBelowCost(price, moneyOf(value.cost));
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id ?? null));
    });
  }

  protected title(): string {
    return this.editing() ? this.part.editTitle : this.part.newTitle;
  }

  protected formatMoney(name: 'cost' | 'unitPrice'): void {
    formatMoneyControl(this.form.controls[name]);
  }

  protected upperCode(): void {
    const control = this.form.controls.code;
    control.setValue(control.value.toUpperCase(), { emitEvent: false });
  }

  protected errorOf(name: keyof typeof this.form.controls): string | null {
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

    const messagesByField: Partial<Record<keyof typeof this.form.controls, string>> = {
      code: this.part.invalidCode,
      name: text.invalidName,
      unitOfMeasure: this.part.invalidUnit,
      cost: text.invalidMoney,
      unitPrice: text.invalidMoney,
      minimumStock: text.invalidInteger,
      barcode: this.part.invalidBarcode,
    };
    return messagesByField[name] ?? text.invalidName;
  }

  protected marginText(): string {
    const margin = this.margin();
    return margin === null ? '—' : messages.catalog.percent(margin);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set(text.fixErrors);
      return;
    }

    const value = this.form.getRawValue();
    const price = moneyOf(value.unitPrice) ?? cents(0);
    const cost = moneyOf(value.cost) ?? this.item()?.cost ?? cents(0);
    if (isBelowCost(price, cost) && !(await this.confirmBelowCost(price, cost))) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const input = {
      code: value.code.trim().toUpperCase(),
      name: value.name.trim(),
      type: value.type,
      unitOfMeasure: value.unitOfMeasure.trim(),
      unitPrice: price,
      cost,
      minimumStock: Number(value.minimumStock),
      supplier: optionalText(value.supplier),
      barcode: optionalText(value.barcode),
    };
    try {
      const id = this.id();
      const saved = id
        ? await firstValueFrom(this.gateway.update('parts', id, input))
        : await firstValueFrom(this.gateway.create('parts', input));
      this.toast.show(text.saved, { tone: 'success' });
      if (id) {
        this.fill(saved);
        this.historyVersion.update((version) => version + 1);
      } else {
        await this.router.navigate(['/catalog', 'parts', saved.id]);
      }
    } catch (error) {
      this.handleError(error);
    } finally {
      this.saving.set(false);
    }
  }

  private async confirmBelowCost(price: number, cost: number): Promise<boolean> {
    const ref = this.dialogs.open<UiConfirmDialog, ConfirmDialogData, boolean>(UiConfirmDialog, {
      title: this.part.confirmBelowCostTitle,
      body: this.part.confirmBelowCost(formatBrl(cents(price)), formatBrl(cents(cost))),
      confirm: this.part.confirmBelowCostAction,
      danger: true,
    });
    return (await firstValueFrom(ref.closed)) === true;
  }

  private handleError(error: unknown): void {
    if (!isApiError(error)) {
      this.error.set(messages.errors.unknown);
      return;
    }

    if (error.code === 'inventory.duplicatePartCode') {
      const code = this.form.controls.code;
      code.setErrors({ server: error.detail });
      code.markAsTouched();
      this.error.set(error.detail);
      return;
    }

    const unmatched = applyApiErrors(this.form, error);
    this.error.set(
      error.fieldErrors.length > 0 && unmatched.length === 0 ? text.fixErrors : error.detail,
    );
  }

  private fill(part: CatalogPart): void {
    this.item.set(part);
    this.form.reset({
      code: part.code,
      type: part.type,
      name: part.name,
      unitOfMeasure: part.unitOfMeasure,
      cost: amountText(part.cost),
      unitPrice: amountText(part.unitPrice),
      minimumStock: part.minimumStock,
      supplier: part.supplier ?? '',
      barcode: part.barcode ?? '',
    });
  }

  private applyAccess(): void {
    if (this.canManage()) {
      this.form.enable();
    } else {
      this.form.disable();
    }
  }

  private async load(id: string | null): Promise<void> {
    this.error.set(null);
    if (!id) {
      this.item.set(null);
      this.form.reset({ minimumStock: 0 });
      this.applyAccess();
      this.state.set('ready');
      return;
    }

    this.state.set('loading');
    try {
      this.fill(await firstValueFrom(this.gateway.get('parts', id)));
      this.applyAccess();
      this.state.set('ready');
    } catch (error) {
      if (isFeaturePending(error) && !this.id()) {
        this.optionsPending.set(true);
        this.applyAccess();
        this.state.set('ready');
        return;
      }

      this.state.set(
        isFeaturePending(error)
          ? 'pending'
          : isApiError(error) && error.status === 404
            ? 'notFound'
            : 'error',
      );
    }
  }

  protected async retry(): Promise<void> {
    await this.load(this.id() ?? null);
  }
}
