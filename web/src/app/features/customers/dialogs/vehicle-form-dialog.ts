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
import { appValidators } from '../../../shared/util/validators';
import { Vehicle } from '../data/vehicle.models';
import { VehiclesGateway } from '../data/vehicles.gateway';

export interface VehicleFormData {
  readonly customerId: string;
  readonly vehicle: Vehicle | null;
}

export const MIN_YEAR = 1950;

export function yearValidator(control: AbstractControl): ValidationErrors | null {
  const value = Number(control.value);
  const max = new Date().getFullYear() + 1;
  return Number.isInteger(value) && value >= MIN_YEAR && value <= max ? null : { year: true };
}

export function mileageValidator(control: AbstractControl): ValidationErrors | null {
  const value = Number(control.value);
  return Number.isInteger(value) && value >= 0 ? null : { mileage: true };
}

@Component({
  selector: 'app-vehicle-form-dialog',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiDialogLayout, UiField],
  templateUrl: './vehicle-form-dialog.html',
  styleUrl: './dialog-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleFormDialog {
  protected readonly ref = inject<DialogRef<Vehicle>>(DialogRef);
  protected readonly data = inject<VehicleFormData>(DIALOG_DATA);
  private readonly vehicles = inject(VehiclesGateway);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.customers.vehicle;
  protected readonly editing = this.data.vehicle !== null;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = new FormGroup({
    licensePlate: new FormControl(this.data.vehicle?.licensePlate ?? '', {
      nonNullable: true,
      validators: [Validators.required, appValidators.plate],
    }),
    brand: new FormControl(this.data.vehicle?.brand ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(60)],
    }),
    model: new FormControl(this.data.vehicle?.model ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(60)],
    }),
    year: new FormControl<number | null>(this.data.vehicle?.year ?? null, {
      validators: [Validators.required, yearValidator],
    }),
    mileage: new FormControl<number | null>(this.data.vehicle?.mileage ?? null, {
      validators: [Validators.required, mileageValidator],
    }),
  });

  constructor() {
    if (this.editing) {
      this.form.controls.licensePlate.disable();
    }
  }

  protected formatPlateInput(): void {
    const control = this.form.controls.licensePlate;
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

    return {
      licensePlate: control.hasError('required')
        ? messages.customers.form.required
        : this.text.invalidPlate,
      brand: messages.customers.form.required,
      model: messages.customers.form.required,
      year: this.text.invalidYear,
      mileage: this.text.invalidMileage,
    }[name];
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    const details = {
      brand: value.brand.trim(),
      model: value.model.trim(),
      year: Number(value.year),
      mileage: Number(value.mileage),
    };
    try {
      const saved = this.data.vehicle
        ? await firstValueFrom(this.vehicles.update(this.data.vehicle.id, details))
        : await firstValueFrom(
            this.vehicles.create({
              ...details,
              customerId: this.data.customerId,
              licensePlate: value.licensePlate,
            }),
          );
      this.toast.show(this.editing ? this.text.updated : this.text.created, { tone: 'success' });
      this.ref.close(saved);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.saving.set(false);
    }
  }

  private handleError(error: unknown): void {
    if (!isApiError(error)) {
      this.error.set(messages.errors.unknown);
      return;
    }

    const field =
      error.code === 'vehicle.duplicatedLicensePlate'
        ? this.form.controls.licensePlate
        : error.code === 'vehicle.mileageDecrease'
          ? this.form.controls.mileage
          : null;
    if (field) {
      field.setErrors({ server: error.detail });
      field.markAsTouched();
      return;
    }

    const unmatched = applyApiErrors(this.form, error);
    this.error.set(error.fieldErrors.length > 0 && unmatched.length === 0 ? null : error.detail);
  }
}
