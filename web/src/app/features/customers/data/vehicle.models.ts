export interface Vehicle {
  readonly id: string;
  readonly customerId: string;
  readonly licensePlate: string;
  readonly brand: string;
  readonly model: string;
  readonly year: number;
  readonly mileage: number;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface VehicleInput {
  readonly customerId: string;
  readonly licensePlate: string;
  readonly brand: string;
  readonly model: string;
  readonly year: number;
  readonly mileage: number;
}

export type VehicleUpdate = Omit<VehicleInput, 'customerId' | 'licensePlate'>;
