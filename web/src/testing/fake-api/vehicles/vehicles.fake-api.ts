import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { formatPlate, isValidPlate, normalizePlate } from '../../../app/shared/util/plate';
import { FakeVehicleRecord } from '../fake-api/fake-database';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  created,
  FakeFieldError,
  fieldError,
  FakeRouter,
  noContent,
  ok,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, isPrincipal } from '../fake-api/fake-session';

export const VEHICLE_PATHS = {
  byCustomer: '/api/admin/customers/:customerId/vehicles',
  collection: '/api/admin/vehicles',
  item: '/api/admin/vehicles/:id',
  transfer: '/api/admin/vehicles/:id/transfer',
  search: '/api/admin/vehicles/search',
} as const;

export const MIN_VEHICLE_YEAR = 1950;
export const VEHICLES_WITH_OPEN_ORDER = new Set<string>();

const text = messages.fake;
const staff = ['Admin', 'Manager', 'Attendant'] as const;

export function toVehicleResponse(vehicle: FakeVehicleRecord): Record<string, unknown> {
  return { ...vehicle, licensePlate: formatPlate(vehicle.licensePlate) };
}

function readNumber(body: Readonly<Record<string, unknown>>, key: string): number | null {
  const value = body[key];
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

function detailErrors(body: Readonly<Record<string, unknown>>): FakeFieldError[] {
  const year = readNumber(body, 'year');
  const mileage = readNumber(body, 'mileage');
  const maxYear = new Date().getUTCFullYear() + 1;
  return [
    ...(readText(body, 'brand') ? [] : [fieldError('brand', 'common.required', text.required)]),
    ...(readText(body, 'model') ? [] : [fieldError('model', 'common.required', text.required)]),
    ...(year !== null && Number.isInteger(year) && year >= MIN_VEHICLE_YEAR && year <= maxYear
      ? []
      : [fieldError('year', 'vehicle.invalidYear', text.invalidVehicleYear)]),
    ...(mileage !== null && Number.isInteger(mileage) && mileage >= 0
      ? []
      : [fieldError('mileage', 'vehicle.invalidMileage', text.invalidField)]),
  ];
}

export function createVehiclesFakeApi(state: FakeBackendState): FakeFeatureApi {
  const vehicles = (): FakeVehicleRecord[] => state.database.vehicles;
  const users = (): FakeBackendState['database']['users'] => state.database.users;
  const customerExists = (id: string): boolean => /^[0-9a-f-]{36}$/i.test(id);
  const find = (id: string): FakeVehicleRecord | undefined =>
    vehicles().find((vehicle) => vehicle.id === id);
  const notFound = (): ReturnType<typeof problem> =>
    problem(404, 'vehicle.notFound', text.vehicleNotFound);
  const plateInUse = (plate: string, ignoreId?: string): boolean =>
    vehicles().some((vehicle) => vehicle.licensePlate === plate && vehicle.id !== ignoreId);

  const router = new FakeRouter()
    .on('GET', VEHICLE_PATHS.byCustomer, (request, params) => {
      const principal = authorize(request, users(), ['Admin', 'Manager', 'Attendant', 'Mechanic']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const items = vehicles()
        .filter((vehicle) => vehicle.customerId === params['customerId'])
        .sort((a, b) => a.licensePlate.localeCompare(b.licensePlate))
        .map(toVehicleResponse);
      return ok({ items });
    })
    .on('POST', VEHICLE_PATHS.search, (request) => {
      const principal = authorize(request, users(), ['Admin', 'Manager', 'Attendant', 'Mechanic']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const plate = normalizePlate(readText(readBody(request), 'licensePlate'));
      const vehicle = vehicles().find((candidate) => candidate.licensePlate === plate);
      return vehicle ? ok(toVehicleResponse(vehicle)) : notFound();
    })
    .on('POST', VEHICLE_PATHS.collection, (request) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const customerId = readText(body, 'customerId');
      const plate = normalizePlate(readText(body, 'licensePlate'));
      const errors = [
        ...(customerExists(customerId)
          ? []
          : [fieldError('customerId', 'common.required', text.required)]),
        ...(isValidPlate(plate)
          ? []
          : [fieldError('licensePlate', 'vehicle.invalidLicensePlate', text.invalidPlate)]),
        ...detailErrors(body),
      ];
      if (errors.length > 0) {
        return validation(errors);
      }

      if (plateInUse(plate)) {
        return problem(409, 'vehicle.duplicatedLicensePlate', text.plateInUse);
      }

      const vehicle: FakeVehicleRecord = {
        id: fakeId(),
        customerId,
        licensePlate: plate,
        brand: readText(body, 'brand'),
        model: readText(body, 'model'),
        year: readNumber(body, 'year') ?? MIN_VEHICLE_YEAR,
        mileage: readNumber(body, 'mileage') ?? 0,
        createdAt: fakeNow(),
        updatedAt: null,
      };
      vehicles().push(vehicle);
      return created(toVehicleResponse(vehicle));
    })
    .on('PUT', VEHICLE_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const vehicle = find(params['id'] ?? '');
      if (!vehicle) {
        return notFound();
      }

      const body = readBody(request);
      const errors = detailErrors(body);
      if (errors.length > 0) {
        return validation(errors);
      }

      const mileage = readNumber(body, 'mileage') ?? vehicle.mileage;
      if (mileage < vehicle.mileage) {
        return problem(400, 'vehicle.mileageDecrease', text.mileageDecrease);
      }

      Object.assign(vehicle, {
        brand: readText(body, 'brand'),
        model: readText(body, 'model'),
        year: readNumber(body, 'year') ?? vehicle.year,
        mileage,
        updatedAt: fakeNow(),
      });
      return ok(toVehicleResponse(vehicle));
    })
    .on('POST', VEHICLE_PATHS.transfer, (request, params) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const vehicle = find(params['id'] ?? '');
      if (!vehicle) {
        return notFound();
      }

      const newCustomerId = readText(readBody(request), 'newCustomerId');
      if (!customerExists(newCustomerId)) {
        return validation([fieldError('newCustomerId', 'common.required', text.required)]);
      }

      if (newCustomerId === vehicle.customerId) {
        return problem(422, 'vehicle.sameOwner', text.sameOwner);
      }

      Object.assign(vehicle, { customerId: newCustomerId, updatedAt: fakeNow() });
      return ok(toVehicleResponse(vehicle));
    })
    .on('DELETE', VEHICLE_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const vehicle = find(params['id'] ?? '');
      if (!vehicle) {
        return notFound();
      }

      if (VEHICLES_WITH_OPEN_ORDER.has(vehicle.licensePlate)) {
        return problem(409, 'vehicle.hasOpenServiceOrder', text.vehicleHasOpenOrder);
      }

      vehicles().splice(vehicles().indexOf(vehicle), 1);
      return noContent();
    });

  return new FakeFeatureApi('vehicles', router, state.scenarios);
}
