import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import {
  debounceTime,
  distinctUntilChanged,
  firstValueFrom,
  switchMap,
  catchError,
  of,
} from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatPlate } from '../../../shared/util/plate';
import { Customer } from '../data/customer.models';
import { CustomersGateway } from '../data/customers.gateway';
import { Vehicle } from '../data/vehicle.models';
import { VehiclesGateway } from '../data/vehicles.gateway';

export const TRANSFER_SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-transfer-vehicle-dialog',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiDialogLayout, UiField],
  templateUrl: './transfer-vehicle-dialog.html',
  styleUrl: './dialog-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransferVehicleDialog {
  protected readonly ref = inject<DialogRef<Vehicle>>(DialogRef);
  protected readonly vehicle = inject<Vehicle>(DIALOG_DATA);
  private readonly customers = inject(CustomersGateway);
  private readonly vehicles = inject(VehiclesGateway);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.customers.vehicle;
  protected readonly plate = formatPlate(this.vehicle.licensePlate);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly candidates = signal<readonly Customer[]>([]);
  protected readonly selected = signal<string | null>(null);
  protected readonly searched = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.search.valueChanges
      .pipe(
        debounceTime(TRANSFER_SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        switchMap((term) =>
          term.trim().length < 2
            ? of([])
            : this.customers
                .query({
                  term,
                  documentType: null,
                  sort: 'Name',
                  direction: 'Asc',
                  page: 1,
                  pageSize: 8,
                })
                .pipe(catchError(() => of({ items: [] as Customer[] }))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        const items = Array.isArray(result) ? result : result.items;
        this.candidates.set(items.filter((customer) => customer.id !== this.vehicle.customerId));
        this.searched.set(true);
      });
  }

  protected choose(id: string): void {
    this.selected.set(id);
    this.error.set(null);
  }

  protected async confirm(): Promise<void> {
    const target = this.selected();
    if (!target) {
      this.error.set(this.text.transferRequired);
      return;
    }

    this.saving.set(true);
    try {
      const vehicle = await firstValueFrom(this.vehicles.transfer(this.vehicle.id, target));
      this.toast.show(this.text.transferred, { tone: 'success' });
      this.ref.close(vehicle);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.saving.set(false);
    }
  }
}
