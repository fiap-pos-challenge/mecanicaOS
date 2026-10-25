import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { ToastService } from '../../../shared/ui/toast/toast';
import { PartType } from '../../catalog/data/catalog.models';
import { InventoryGateway } from '../data/inventory.gateway';
import { balanceAfterExit, isValidQuantityFor } from '../data/inventory.math';
import { StockItem, StockMovement } from '../data/inventory.models';

export function quantityForValidator(type: PartType) {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: unknown = control.value;
    if (value === null || value === '' || value === undefined) {
      return null;
    }

    return isValidQuantityFor(type, Number(value)) ? null : { quantity: true };
  };
}

@Component({
  selector: 'app-stock-exit-dialog',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiDialogLayout, UiField],
  templateUrl: './stock-exit-dialog.html',
  styleUrl: './inventory-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockExitDialog {
  protected readonly ref = inject<DialogRef<StockMovement>>(DialogRef);
  protected readonly item = inject<StockItem>(DIALOG_DATA);
  private readonly gateway = inject(InventoryGateway);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.inventory.exit;
  protected readonly entry = messages.inventory.entry;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = new FormGroup({
    quantity: new FormControl<number | null>(null, {
      validators: [Validators.required, quantityForValidator(this.item.type)],
    }),
    reason: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] }),
    serviceOrderNumber: new FormControl<number | null>(null, {
      validators: [Validators.min(1)],
    }),
  });

  private readonly quantity = toSignal(this.form.controls.quantity.valueChanges, {
    initialValue: null,
  });
  protected readonly negative = computed(() => {
    const quantity = Number(this.quantity());
    return quantity > 0 && balanceAfterExit(this.item.stockQuantity, quantity) === null;
  });

  protected balance(): string {
    return this.text.balance(
      messages.inventory.quantity(this.item.stockQuantity, this.item.unitOfMeasure),
    );
  }

  protected quantityError(): string | null {
    const control = this.form.controls.quantity;
    if (this.negative()) {
      return this.text.negative;
    }

    if (!control.touched || control.valid) {
      return null;
    }

    return control.hasError('required') ? this.entry.required : this.entry.invalidQuantity;
  }

  protected async save(): Promise<void> {
    if (this.form.invalid || this.negative()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    try {
      const movement = await firstValueFrom(
        this.gateway.registerExit({
          partId: this.item.partId,
          quantity: Number(value.quantity),
          reason: value.reason.trim(),
          serviceOrderNumber: value.serviceOrderNumber ? Number(value.serviceOrderNumber) : null,
        }),
      );
      this.toast.show(this.text.saved, { tone: 'success' });
      this.ref.close(movement);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.saving.set(false);
    }
  }
}
