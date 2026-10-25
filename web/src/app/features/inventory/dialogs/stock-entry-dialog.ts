import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MAX_PAGE_SIZE } from '../../../core/data/page';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { ToastService } from '../../../shared/ui/toast/toast';
import { onlyDigits } from '../../../shared/util/digits';
import { cents, formatAmount } from '../../../shared/util/money';
import { amountText, moneyOf, moneyValidator } from '../../catalog/pages/catalog-form';
import { BarcodeScanner } from '../components/barcode-scanner';
import { InventoryGateway } from '../data/inventory.gateway';
import { isValidQuantityFor } from '../data/inventory.math';
import { InvoiceDraft, StockItem } from '../data/inventory.models';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';

export type EntryMode = 'manual' | 'barcode' | 'invoice';

export const MAX_INVOICE_BYTES = 2 * 1024 * 1024;

export function isInvoiceFile(file: Pick<File, 'name' | 'size'>): boolean {
  return /\.(xml|pdf)$/i.test(file.name) && file.size > 0 && file.size <= MAX_INVOICE_BYTES;
}

export async function readAsBase64(file: Blob): Promise<string> {
  const bytes = new Uint8Array(await file.arrayBuffer());
  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return btoa(binary);
}

function draftLine(line: InvoiceDraft['lines'][number]) {
  return new FormGroup({
    description: new FormControl(line.description, { nonNullable: true }),
    partId: new FormControl(line.partId ?? '', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    quantity: new FormControl<number | null>(line.quantity, { validators: [Validators.required] }),
    unitCost: new FormControl(amountText(line.unitCost), {
      nonNullable: true,
      validators: [Validators.required, moneyValidator],
    }),
  });
}

@Component({
  selector: 'app-stock-entry-dialog',
  imports: [
    UiAiBadge,
    BarcodeScanner,
    ReactiveFormsModule,
    UiButton,
    UiControl,
    UiDialogLayout,
    UiField,
    UiPendingNotice,
    UiSegmented,
  ],
  templateUrl: './stock-entry-dialog.html',
  styleUrl: './inventory-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockEntryDialog {
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  private readonly gateway = inject(InventoryGateway);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.inventory.entry;
  protected readonly pendingText = messages.pending.inventory;
  protected readonly modeOptions: readonly SegmentedOption<EntryMode>[] = [
    { value: 'manual', label: this.text.modes.manual },
    { value: 'barcode', label: this.text.modes.barcode },
    { value: 'invoice', label: this.text.modes.invoice },
  ];

  protected readonly mode = signal<EntryMode>('manual');
  protected readonly items = signal<readonly StockItem[]>([]);
  protected readonly itemsPending = signal(false);
  protected readonly saving = signal(false);
  protected readonly reading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly found = signal<StockItem | null>(null);
  protected readonly draft = signal<InvoiceDraft | null>(null);
  protected readonly file = signal<File | null>(null);

  protected readonly barcode = new FormControl('', { nonNullable: true });
  protected readonly form = new FormGroup({
    partId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    quantity: new FormControl<number | null>(null, { validators: [Validators.required] }),
    unitCost: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, moneyValidator],
    }),
    reason: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] }),
  });
  protected readonly lines = new FormArray<ReturnType<typeof draftLine>>([]);

  private readonly partId = toSignal(this.form.controls.partId.valueChanges, { initialValue: '' });
  protected readonly selected = computed(
    () => this.items().find((item) => item.partId === this.partId()) ?? this.found(),
  );

  constructor() {
    void this.loadItems();
  }

  protected setMode(mode: EntryMode): void {
    this.mode.set(mode);
    this.error.set(null);
  }

  protected selectPart(): void {
    const item = this.selected();
    if (item && !this.form.controls.unitCost.value) {
      this.form.controls.unitCost.setValue(formatAmount(item.unitCost));
    }
  }

  protected quantityHint(): string | null {
    const item = this.selected();
    return item ? (item.type === 'Part' ? this.text.quantityPart : this.text.quantitySupply) : null;
  }

  protected errorOf(name: 'partId' | 'quantity' | 'unitCost'): string | null {
    const control = this.form.controls[name];
    const item = this.selected();
    if (
      name === 'quantity' &&
      control.touched &&
      control.value !== null &&
      item &&
      !isValidQuantityFor(item.type, Number(control.value))
    ) {
      return this.text.invalidQuantity;
    }

    if (!control.touched || control.valid) {
      return null;
    }

    if (control.hasError('required')) {
      return this.text.required;
    }

    return name === 'unitCost' ? this.text.invalidMoney : this.text.invalidQuantity;
  }

  protected async lookup(code: string = this.barcode.value): Promise<void> {
    const barcode = onlyDigits(code);
    this.barcode.setValue(barcode);
    if (!barcode) {
      return;
    }

    this.error.set(null);
    this.found.set(null);
    try {
      const item = await firstValueFrom(this.gateway.findByBarcode(barcode));
      this.found.set(item);
      this.form.controls.partId.setValue(item.partId);
      this.form.controls.unitCost.setValue(formatAmount(item.unitCost));
    } catch (error) {
      this.form.controls.partId.setValue('');
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    }
  }

  protected async save(): Promise<void> {
    const item = this.selected();
    const quantity = Number(this.form.controls.quantity.value);
    if (this.form.invalid || !item || !isValidQuantityFor(item.type, quantity)) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    try {
      await firstValueFrom(
        this.gateway.registerEntry({
          partId: item.partId,
          quantity,
          unitCost: moneyOf(value.unitCost) ?? cents(0),
          reason: value.reason.trim(),
          supplierId: null,
          invoiceNumber: null,
        }),
      );
      this.toast.show(this.text.saved, { tone: 'success' });
      this.ref.close(true);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.saving.set(false);
    }
  }

  protected chooseFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.draft.set(null);
    this.lines.clear();
    if (file && !isInvoiceFile(file)) {
      this.file.set(null);
      this.error.set(this.text.invalidFile);
      return;
    }

    this.error.set(null);
    this.file.set(file);
  }

  protected async readInvoice(): Promise<void> {
    const file = this.file();
    if (!file) {
      this.error.set(this.text.invalidFile);
      return;
    }

    this.reading.set(true);
    this.error.set(null);
    try {
      const draft = await firstValueFrom(
        this.gateway.readInvoice({
          fileName: file.name,
          contentType: file.type || 'application/octet-stream',
          content: await readAsBase64(file),
        }),
      );
      this.draft.set(draft);
      this.lines.clear();
      for (const line of draft.lines) {
        this.lines.push(draftLine(line));
      }
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.reading.set(false);
    }
  }

  protected lineInvalid(index: number): boolean {
    const line = this.lines.at(index).getRawValue();
    const item = this.items().find((candidate) => candidate.partId === line.partId);
    return !item || !isValidQuantityFor(item.type, Number(line.quantity));
  }

  protected async confirmInvoice(): Promise<void> {
    const draft = this.draft();
    const lines = this.lines.getRawValue();
    if (!draft || this.lines.invalid || lines.some((_, index) => this.lineInvalid(index))) {
      this.lines.markAllAsTouched();
      this.error.set(this.text.draftHint);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.gateway.confirmInvoice({
          supplierId: draft.supplierId,
          invoiceNumber: draft.invoiceNumber,
          lines: lines.map((line) => ({
            partId: line.partId,
            quantity: Number(line.quantity),
            unitCost: moneyOf(line.unitCost) ?? cents(0),
          })),
        }),
      );
      this.toast.show(this.text.confirmed, { tone: 'success' });
      this.ref.close(true);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.saving.set(false);
    }
  }

  private async loadItems(): Promise<void> {
    try {
      const overview = await firstValueFrom(
        this.gateway.overview({
          page: 1,
          pageSize: MAX_PAGE_SIZE,
          term: '',
          situation: null,
          location: null,
        }),
      );
      this.items.set(overview.items);
    } catch (error) {
      this.itemsPending.set(isFeaturePending(error));
    }
  }
}
