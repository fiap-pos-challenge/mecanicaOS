import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { applyApiErrors, serverErrorOf } from '../../../core/forms/apply-api-errors';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { ToastService } from '../../../shared/ui/toast/toast';
import { onlyDigits } from '../../../shared/util/digits';
import { documentKindOf, formatDocument } from '../../../shared/util/document';
import { formatPhone } from '../../../shared/util/phone';
import { InventoryGateway } from '../data/inventory.gateway';
import { Supplier } from '../data/inventory.models';

export function cnpjValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  return value.trim() === '' || documentKindOf(value) === 'Cnpj' ? null : { cnpj: true };
}

@Component({
  selector: 'app-supplier-dialog',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiDialogLayout, UiField],
  templateUrl: './supplier-dialog.html',
  styleUrl: './inventory-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierDialog {
  protected readonly ref = inject<DialogRef<Supplier>>(DialogRef);
  protected readonly supplier = inject<Supplier | null>(DIALOG_DATA);
  private readonly gateway = inject(InventoryGateway);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.inventory.suppliers;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = new FormGroup({
    name: new FormControl(this.supplier?.name ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    }),
    document: new FormControl(
      this.supplier?.document ? formatDocument(this.supplier.document) : '',
      { nonNullable: true, validators: [cnpjValidator] },
    ),
    email: new FormControl(this.supplier?.email ?? '', {
      nonNullable: true,
      validators: [Validators.email],
    }),
    phone: new FormControl(this.supplier?.phone ? formatPhone(this.supplier.phone) : '', {
      nonNullable: true,
    }),
    leadTimeDays: new FormControl<number | null>(this.supplier?.leadTimeDays ?? 3, {
      validators: [Validators.required, Validators.min(0), Validators.max(365)],
    }),
  });

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
      return this.text.required;
    }

    return {
      name: this.text.invalidName,
      document: this.text.invalidDocument,
      email: this.text.invalidEmail,
      phone: this.text.required,
      leadTimeDays: this.text.invalidLeadTime,
    }[name];
  }

  protected maskDocument(): void {
    const control = this.form.controls.document;
    control.setValue(formatDocument(onlyDigits(control.value)), { emitEvent: false });
  }

  protected maskPhone(): void {
    const control = this.form.controls.phone;
    control.setValue(formatPhone(control.value), { emitEvent: false });
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    const input = {
      name: value.name.trim(),
      document: onlyDigits(value.document) || null,
      email: value.email.trim() || null,
      phone: onlyDigits(value.phone) || null,
      leadTimeDays: Number(value.leadTimeDays),
    };
    try {
      const saved = this.supplier
        ? await firstValueFrom(this.gateway.updateSupplier(this.supplier.id, input))
        : await firstValueFrom(this.gateway.createSupplier(input));
      this.toast.show(this.text.saved, { tone: 'success' });
      this.ref.close(saved);
    } catch (error) {
      if (isApiError(error)) {
        const unmatched = applyApiErrors(this.form, error);
        this.error.set(
          error.fieldErrors.length > 0 && unmatched.length === 0 ? null : error.detail,
        );
      } else {
        this.error.set(messages.errors.unknown);
      }
    } finally {
      this.saving.set(false);
    }
  }
}
