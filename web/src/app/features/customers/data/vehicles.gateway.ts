import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { normalizePlate } from '../../../shared/util/plate';
import {
  CreateVehicleData,
  ListCustomerVehiclesData,
  SearchVehicleData,
  TransferVehicleData,
  UpdateVehicleData,
  VehicleDto,
  VehicleListDto,
} from './vehicles.contracts';
import { toVehicle, toVehicles } from './vehicle.mappers';
import { Vehicle, VehicleInput, VehicleUpdate } from './vehicle.models';

export abstract class VehiclesGateway {
  abstract listByCustomer(customerId: string): Observable<readonly Vehicle[]>;
  abstract findByPlate(licensePlate: string): Observable<Vehicle>;
  abstract create(input: VehicleInput): Observable<Vehicle>;
  abstract update(id: string, input: VehicleUpdate): Observable<Vehicle>;
  abstract transfer(id: string, newCustomerId: string): Observable<Vehicle>;
  abstract remove(id: string): Observable<void>;
}

@Injectable({ providedIn: 'root' })
export class HttpVehiclesGateway extends VehiclesGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  listByCustomer(customerId: string): Observable<readonly Vehicle[]> {
    return this.api
      .send<ListCustomerVehiclesData, VehicleListDto>(
        'GET',
        '/api/admin/customers/{customerId}/vehicles',
        {
          path: { customerId },
          context: this.context(),
        },
      )
      .pipe(map(toVehicles));
  }

  findByPlate(licensePlate: string): Observable<Vehicle> {
    return this.api
      .send<SearchVehicleData, VehicleDto>('POST', '/api/admin/vehicles/search', {
        body: { licensePlate: normalizePlate(licensePlate) },
        context: this.context(),
      })
      .pipe(map(toVehicle));
  }

  create(input: VehicleInput): Observable<Vehicle> {
    return this.api
      .send<CreateVehicleData, VehicleDto>('POST', '/api/admin/vehicles', {
        body: { ...input, licensePlate: normalizePlate(input.licensePlate) },
        context: this.context(),
      })
      .pipe(map(toVehicle));
  }

  update(id: string, input: VehicleUpdate): Observable<Vehicle> {
    return this.api
      .send<UpdateVehicleData, VehicleDto>('PUT', '/api/admin/vehicles/{id}', {
        path: { id },
        body: input,
        context: this.context(),
      })
      .pipe(map(toVehicle));
  }

  transfer(id: string, newCustomerId: string): Observable<Vehicle> {
    return this.api
      .send<TransferVehicleData, VehicleDto>('POST', '/api/admin/vehicles/{id}/transfer', {
        path: { id },
        body: { newCustomerId },
        context: this.context(),
      })
      .pipe(map(toVehicle));
  }

  remove(id: string): Observable<void> {
    return this.api
      .send<UpdateVehicleData, unknown>('DELETE', '/api/admin/vehicles/{id}', {
        path: { id },
        context: this.context(),
      })
      .pipe(map(() => undefined));
  }
}

export function provideVehiclesData(): Provider {
  return provideGateway(VehiclesGateway, 'vehicles', {
    http: HttpVehiclesGateway,
  });
}
