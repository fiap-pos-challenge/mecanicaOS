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
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon, Plus as PlusIcon, X as XIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { MAX_PAGE_SIZE } from '../../../core/data/page';
import { applyApiErrors, serverErrorOf } from '../../../core/forms/apply-api-errors';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
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
import { cents, Cents, formatBrl } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { PriceHistory } from '../components/price-history';
import { CatalogGateway } from '../data/catalog.gateway';
import {
  isValidQuantity,
  itemsTotal,
  lineTotal,
  savingsOf,
  savingsPercent,
} from '../data/catalog.math';
import { CatalogPackage, PackageItem, PackageItemKind } from '../data/catalog.models';
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

export interface PackageOption {
  readonly kind: PackageItemKind;
  readonly itemId: string;
  readonly name: string;
  readonly unitPrice: Cents;
}

const text = messages.catalog.form;
const MAX_RESULTS = 8;

export function mileageRangeValidator(group: AbstractControl): ValidationErrors | null {
  const min = group.get('minMileage')?.value as number | null | undefined;
  const max = group.get('maxMileage')?.value as number | null | undefined;
  return typeof min === 'number' && typeof max === 'number' && min > max
    ? { mileageRange: true }
    : null;
}

export function normalizeSearch(value: string): string {
  return value.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
}

function itemGroup(item: PackageItem) {
  return new FormGroup({
    kind: new FormControl<PackageItemKind>(item.kind, { nonNullable: true }),
    itemId: new FormControl(item.itemId, { nonNullable: true }),
    name: new FormControl(item.name, { nonNullable: true }),
    unitPrice: new FormControl<number>(item.unitPrice, { nonNullable: true }),
    quantity: new FormControl<number | null>(item.quantity, {
      validators: [Validators.required, quantityValidator],
    }),
  });
}

@Component({
  selector: 'app-package-form-page',
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
  templateUrl: './package-form-page.html',
  styleUrls: ['./catalog-form-page.scss', './catalog-lines.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageFormPage {
  readonly id = input<string | null | undefined>(null);

  private readonly gateway = inject(CatalogGateway);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly text = text;
  protected readonly pack = text.packages;
  protected readonly kinds = messages.catalog.itemKinds;
  protected readonly icons = { back: ArrowLeftIcon, add: PlusIcon, remove: XIcon };

  protected readonly state = signal<LoadState>('loading');
  protected readonly item = signal<CatalogPackage | null>(null);
  protected readonly options = signal<readonly PackageOption[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly historyVersion = signal(0);
  protected readonly optionsPending = signal(false);
  protected readonly pendingText = messages.pending.catalog;
  protected readonly editing = computed(() => Boolean(this.id()));
  protected readonly canManage = computed(() => this.auth.can('catalog.manage'));

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(500)],
    }),
    items: new FormArray<ReturnType<typeof itemGroup>>([], { validators: [Validators.required] }),
    price: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, moneyValidator],
    }),
    suggestion: new FormGroup(
      {
        minMileage: new FormControl<number | null>(null, {
          validators: [numberRange(0, 2_000_000, true)],
        }),
        maxMileage: new FormControl<number | null>(null, {
          validators: [numberRange(0, 2_000_000, true)],
        }),
      },
      { validators: [mileageRangeValidator] },
    ),
  });

  private readonly term = toSignal(this.search.valueChanges, { initialValue: '' });
  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  protected readonly results = computed(() => {
    const term = normalizeSearch(this.term());
    this.values();
    if (!term) {
      return [];
    }

    const chosen = new Set(this.form.controls.items.getRawValue().map((line) => line.itemId));
    return this.options()
      .filter((option) => !chosen.has(option.itemId) && normalizeSearch(option.name).includes(term))
      .slice(0, MAX_RESULTS);
  });

  protected readonly summary = computed(() => {
    this.values();
    const lines = this.form.controls.items
      .getRawValue()
      .filter((line) => isValidQuantity(Number(line.quantity)))
      .map((line) => ({ unitPrice: cents(line.unitPrice), quantity: Number(line.quantity) }));
    const total = itemsTotal(lines);
    const price = moneyOf(this.form.controls.price.value);
    const savings = price === null ? null : savingsOf(total, price);
    const percent = price === null ? null : savingsPercent(total, price);
    return { total, price, savings, percent };
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id ?? null));
    });
  }

  protected title(): string {
    return this.editing() ? this.pack.editTitle : this.pack.newTitle;
  }

  protected money(value: Cents): string {
    return formatBrl(value);
  }

  protected lineTotalText(index: number): string {
    const line = this.form.controls.items.at(index).getRawValue();
    const quantity = Number(line.quantity);
    return isValidQuantity(quantity)
      ? formatBrl(lineTotal({ unitPrice: cents(line.unitPrice), quantity }))
      : '—';
  }

  protected unitPriceText(index: number): string {
    return formatBrl(cents(this.form.controls.items.at(index).controls.unitPrice.value));
  }

  protected percentText(value: number): string {
    return messages.catalog.percent(value);
  }

  protected add(option: PackageOption): void {
    this.form.controls.items.push(itemGroup({ ...option, quantity: 1 }));
    this.form.markAsDirty();
    this.search.setValue('');
  }

  protected remove(index: number): void {
    this.form.controls.items.removeAt(index);
    this.form.markAsDirty();
  }

  protected formatPrice(): void {
    formatMoneyControl(this.form.controls.price);
  }

  protected quantityInvalid(index: number): boolean {
    const control = this.form.controls.items.at(index).controls.quantity;
    return control.touched && control.invalid;
  }

  protected itemsError(): string | null {
    const items = this.form.controls.items;
    if (!items.touched && !items.dirty) {
      return null;
    }

    return serverErrorOf(items) ?? (items.length === 0 ? this.pack.noItems : null);
  }

  protected errorOf(name: 'name' | 'price'): string | null {
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

    return name === 'name' ? text.invalidName : text.invalidMoney;
  }

  protected rangeError(): string | null {
    const suggestion = this.form.controls.suggestion;
    if (!suggestion.touched && !suggestion.dirty) {
      return null;
    }

    if (serverErrorOf(suggestion)) {
      return serverErrorOf(suggestion);
    }

    if (suggestion.hasError('mileageRange')) {
      return this.pack.invalidRange;
    }

    return suggestion.controls.minMileage.invalid || suggestion.controls.maxMileage.invalid
      ? text.invalidInteger
      : null;
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.form.controls.items.markAsDirty();
      this.error.set(text.fixErrors);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    const minMileage = value.suggestion.minMileage;
    const maxMileage = value.suggestion.maxMileage;
    const input = {
      name: value.name.trim(),
      description: optionalText(value.description),
      items: value.items.map((line) => ({
        kind: line.kind,
        itemId: line.itemId,
        quantity: Number(line.quantity),
      })),
      price: moneyOf(value.price) ?? cents(0),
      suggestion:
        minMileage === null && maxMileage === null
          ? null
          : {
              minMileage: minMileage === null ? null : Number(minMileage),
              maxMileage: maxMileage === null ? null : Number(maxMileage),
            },
    };
    try {
      const id = this.id();
      const saved = id
        ? await firstValueFrom(this.gateway.update('packages', id, input))
        : await firstValueFrom(this.gateway.create('packages', input));
      this.toast.show(text.saved, { tone: 'success' });
      if (id) {
        this.fill(saved);
        this.historyVersion.update((version) => version + 1);
      } else {
        await this.router.navigate(['/catalog', 'packages', saved.id]);
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

  private fill(pack: CatalogPackage): void {
    this.item.set(pack);
    this.form.controls.items.clear();
    for (const line of pack.items) {
      this.form.controls.items.push(itemGroup(line));
    }

    this.form.reset({
      name: pack.name,
      description: pack.description ?? '',
      items: pack.items.map((line) => ({ ...line })),
      price: amountText(pack.price),
      suggestion: {
        minMileage: pack.suggestion?.minMileage ?? null,
        maxMileage: pack.suggestion?.maxMileage ?? null,
      },
    });
  }

  private applyAccess(): void {
    if (this.canManage()) {
      this.form.enable();
      this.search.enable();
    } else {
      this.form.disable();
      this.search.disable();
    }
  }

  private async load(id: string | null): Promise<void> {
    this.state.set('loading');
    this.error.set(null);
    const all = { page: 1, pageSize: MAX_PAGE_SIZE, term: '', status: 'Active' as const };
    try {
      const [services, parts, pack] = await Promise.all([
        firstValueFrom(this.gateway.list('services', all)),
        firstValueFrom(this.gateway.list('parts', all)),
        id ? firstValueFrom(this.gateway.get('packages', id)) : Promise.resolve(null),
      ]);
      this.options.set([
        ...services.items.map((service) => ({
          kind: 'Service' as const,
          itemId: service.id,
          name: service.name,
          unitPrice: service.basePrice,
        })),
        ...parts.items.map((part) => ({
          kind: 'Part' as const,
          itemId: part.id,
          name: part.name,
          unitPrice: part.unitPrice,
        })),
      ]);
      if (pack) {
        this.fill(pack);
      } else {
        this.item.set(null);
        this.form.controls.items.clear();
        this.form.reset();
      }

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
}
