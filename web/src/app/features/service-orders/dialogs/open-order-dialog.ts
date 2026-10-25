import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  firstValueFrom,
  of,
  switchMap,
} from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { formatPlate } from '../../../shared/util/plate';
import { Customer } from '../../customers/data/customer.models';
import { provideCustomersData } from '../../customers/data/customers.providers';
import { CustomersGateway } from '../../customers/data/customers.gateway';
import { Vehicle } from '../../customers/data/vehicle.models';
import { VehiclesGateway } from '../../customers/data/vehicles.gateway';
import { provideServiceOrderData, ServiceOrderGateway } from '../data/service-order.gateway';
import {
  PROBLEM_MAX_LENGTH,
  PROBLEM_MIN_LENGTH,
  ServiceOrderDetail,
} from '../data/service-order.models';

export const OPEN_ORDER_SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-open-order-dialog',
  imports: [ReactiveFormsModule, UiButton, UiControl, UiDialogLayout, UiField],
  providers: [provideCustomersData(), provideServiceOrderData()],
  templateUrl: './open-order-dialog.html',
  styleUrl: './open-order-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OpenOrderDialog {
  protected readonly ref = inject<DialogRef<ServiceOrderDetail>>(DialogRef);
  private readonly customers = inject(CustomersGateway);
  private readonly vehicles = inject(VehiclesGateway);
  private readonly orders = inject(ServiceOrderGateway);

  protected readonly text = messages.serviceOrders.openDialog;
  protected readonly maxLength = PROBLEM_MAX_LENGTH;
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly problem = new FormControl('', { nonNullable: true });
  protected readonly promisedAt = new FormControl('', { nonNullable: true });
  protected readonly candidates = signal<readonly Customer[]>([]);
  protected readonly searched = signal(false);
  protected readonly customer = signal<Customer | null>(null);
  protected readonly customerVehicles = signal<readonly Vehicle[]>([]);
  protected readonly vehiclesLoading = signal(false);
  protected readonly vehicle = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.search.valueChanges
      .pipe(
        debounceTime(OPEN_ORDER_SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        switchMap((term) =>
          term.trim().length < 2
            ? of({ items: [] as readonly Customer[] })
            : this.customers
                .query({
                  term,
                  documentType: null,
                  sort: 'Name',
                  direction: 'Asc',
                  page: 1,
                  pageSize: 8,
                })
                .pipe(catchError(() => of({ items: [] as readonly Customer[] }))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.candidates.set(result.items);
        this.searched.set(true);
      });
  }

  protected plate(value: string): string {
    return formatPlate(value);
  }

  protected async chooseCustomer(customer: Customer): Promise<void> {
    this.customer.set(customer);
    this.vehicle.set(null);
    this.error.set(null);
    this.vehiclesLoading.set(true);
    try {
      const vehicles = await firstValueFrom(this.vehicles.listByCustomer(customer.id));
      this.customerVehicles.set(vehicles);
      this.vehicle.set(vehicles.length === 1 ? (vehicles[0]?.id ?? null) : null);
    } catch {
      this.customerVehicles.set([]);
      this.error.set(this.text.vehiclesError);
    } finally {
      this.vehiclesLoading.set(false);
    }
  }

  protected changeCustomer(): void {
    this.customer.set(null);
    this.customerVehicles.set([]);
    this.vehicle.set(null);
  }

  protected chooseVehicle(id: string): void {
    this.vehicle.set(id);
    this.error.set(null);
  }

  protected async confirm(): Promise<void> {
    const customer = this.customer();
    const vehicleId = this.vehicle();
    const problem = this.problem.value.trim();
    if (!customer) {
      this.error.set(this.text.customerRequired);
      return;
    }

    if (!vehicleId) {
      this.error.set(this.text.vehicleRequired);
      return;
    }

    if (problem.length < PROBLEM_MIN_LENGTH) {
      this.error.set(this.text.problemRequired);
      return;
    }

    const promised = this.promisedAt.value ? new Date(this.promisedAt.value) : null;
    if (promised && promised.getTime() <= Date.now()) {
      this.error.set(this.text.promiseInPast);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      const order = await firstValueFrom(
        this.orders.open({
          customerId: customer.id,
          vehicleId,
          problemDescription: problem,
          promisedAt: promised ? promised.toISOString() : null,
          mileage: null,
        }),
      );
      this.ref.close(order);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.saving.set(false);
    }
  }
}
