export interface VehicleDto {
  readonly id?: string;
  readonly customerId?: string;
  readonly licensePlate?: string | null;
  readonly brand?: string | null;
  readonly model?: string | null;
  readonly year?: number;
  readonly mileage?: number;
  readonly createdAt?: string;
  readonly updatedAt?: string | null;
}

export interface VehicleListDto {
  readonly items?: readonly VehicleDto[] | null;
}

export interface CreateVehicleRequest {
  readonly customerId: string;
  readonly licensePlate: string;
  readonly brand: string;
  readonly model: string;
  readonly year: number;
  readonly mileage: number;
}

export type UpdateVehicleRequest = Omit<CreateVehicleRequest, 'customerId' | 'licensePlate'>;

export interface ListCustomerVehiclesData {
  readonly url: '/api/admin/customers/{customerId}/vehicles';
  readonly path: { readonly customerId: string };
}

export interface CreateVehicleData {
  readonly url: '/api/admin/vehicles';
  readonly body: CreateVehicleRequest;
}

export interface UpdateVehicleData {
  readonly url: '/api/admin/vehicles/{id}';
  readonly path: { readonly id: string };
  readonly body?: UpdateVehicleRequest;
}

export interface TransferVehicleData {
  readonly url: '/api/admin/vehicles/{id}/transfer';
  readonly path: { readonly id: string };
  readonly body: { readonly newCustomerId: string };
}

export interface SearchVehicleData {
  readonly url: '/api/admin/vehicles/search';
  readonly body: { readonly licensePlate: string };
}
