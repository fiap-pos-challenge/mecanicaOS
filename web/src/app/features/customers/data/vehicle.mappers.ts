import { required } from '../../../core/data/contract-error';
import { VehicleDto, VehicleListDto } from './vehicles.contracts';
import { Vehicle } from './vehicle.models';

export function toVehicle(dto: VehicleDto): Vehicle {
  return {
    id: required(dto.id, 'id'),
    customerId: required(dto.customerId, 'customerId'),
    licensePlate: required(dto.licensePlate, 'licensePlate'),
    brand: required(dto.brand, 'brand'),
    model: required(dto.model, 'model'),
    year: required(dto.year, 'year'),
    mileage: required(dto.mileage, 'mileage'),
    createdAt: required(dto.createdAt, 'createdAt'),
    updatedAt: dto.updatedAt ?? null,
  };
}

export function toVehicles(dto: VehicleListDto): readonly Vehicle[] {
  return (dto.items ?? []).map(toVehicle);
}
