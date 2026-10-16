import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  created,
  fieldError,
  FakeRouter,
  noContent,
  ok,
  paginate,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, FakePrincipal, FakeRole, isPrincipal } from '../fake-api/fake-session';
import {
  FakeCatalog,
  FakeCatalogPackage,
  FakeCatalogPart,
  FakeCatalogService,
  FakePackageItem,
  FakePriceChange,
  FakePriceField,
} from './catalog.seed';

export const CATALOG_PATHS = {
  collection: '/api/admin/catalog/:kind',
  item: '/api/admin/catalog/:kind/:id',
  deactivate: '/api/admin/catalog/:kind/:id/deactivate',
  reactivate: '/api/admin/catalog/:kind/:id/reactivate',
  history: '/api/admin/catalog/:kind/:id/price-history',
} as const;

type Kind = 'services' | 'packages' | 'parts';
type FieldErrors = ReturnType<typeof fieldError>[];

const readers: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic'];
const managers: readonly FakeRole[] = ['Admin', 'Manager'];
const KINDS: readonly Kind[] = ['services', 'packages', 'parts'];
const CODE = /^[A-Z0-9.-]{1,30}$/;
const text = messages.fake;

export function toCents(value: number): number {
  return Math.round(value * 100);
}

export function fromCents(value: number): number {
  return value / 100;
}

export function fakeLineTotal(unitPrice: number, quantity: number): number {
  const thousandths = Math.round(quantity * 1000);
  const raw = (toCents(unitPrice) * thousandths) / 1000;
  return raw < 0 ? -Math.round(-raw) : Math.round(raw);
}

export function isValidEan13(barcode: string): boolean {
  if (!/^\d{13}$/.test(barcode)) {
    return false;
  }

  const digits = [...barcode].map(Number);
  const check = digits.pop() ?? -1;
  const sum = digits.reduce((total, digit, index) => total + digit * (index % 2 === 0 ? 1 : 3), 0);
  return (10 - (sum % 10)) % 10 === check;
}

function normalize(value: string): string {
  return value.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

function readKind(value: string | undefined): Kind | null {
  return KINDS.find((kind) => kind === value) ?? null;
}

function readNumber(body: Readonly<Record<string, unknown>>, key: string): number | null {
  const value = body[key];
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

function readNullableText(body: Readonly<Record<string, unknown>>, key: string): string | null {
  const value = readText(body, key).trim();
  return value === '' ? null : value;
}

function isQuantity(value: unknown): value is number {
  return (
    typeof value === 'number' &&
    Number.isFinite(value) &&
    value > 0 &&
    Math.abs(value * 1000 - Math.round(value * 1000)) < 1e-6
  );
}

function money(body: Readonly<Record<string, unknown>>, key: string, errors: FieldErrors): number {
  const value = readNumber(body, key);
  if (value === null || value < 0 || Math.abs(value * 100 - Math.round(value * 100)) > 1e-6) {
    errors.push(fieldError(key, 'catalog.invalidPrice', text.invalidPrice));
    return 0;
  }

  return value;
}

function nameOf(body: Readonly<Record<string, unknown>>, errors: FieldErrors): string {
  const name = readText(body, 'name').trim();
  if (name.length < 2 || name.length > 120) {
    errors.push(fieldError('name', 'catalog.invalidName', text.invalidCatalogName));
  }

  return name;
}

export function createCatalogFakeApi(state: FakeBackendState): FakeFeatureApi {
  const catalog = (): FakeCatalog => state.database.catalog;
  const users = (): FakeBackendState['database']['users'] => state.database.users;
  const canSeeCosts = (principal: FakePrincipal): boolean => managers.includes(principal.role);
  const nameOfPrincipal = (principal: FakePrincipal): string =>
    users().find((user) => user.id === principal.id)?.name ?? messages.roles[principal.role];

  const unitPriceOf = (item: FakePackageItem): number =>
    item.kind === 'Service'
      ? (catalog().services.find((service) => service.id === item.itemId)?.basePrice ?? 0)
      : (catalog().parts.find((part) => part.id === item.itemId)?.unitPrice ?? 0);

  const itemNameOf = (item: FakePackageItem): string =>
    item.kind === 'Service'
      ? (catalog().services.find((service) => service.id === item.itemId)?.name ?? '')
      : (catalog().parts.find((part) => part.id === item.itemId)?.name ?? '');

  const packageDto = (pack: FakeCatalogPackage): Record<string, unknown> => {
    const total = pack.items.reduce(
      (sum, item) => sum + fakeLineTotal(unitPriceOf(item), item.quantity),
      0,
    );
    return {
      ...pack,
      items: pack.items.map((item) => ({
        ...item,
        name: itemNameOf(item),
        unitPrice: unitPriceOf(item),
      })),
      itemsTotal: fromCents(total),
      savings: fromCents(total - toCents(pack.price)),
    };
  };

  const partDto = (part: FakeCatalogPart, principal: FakePrincipal): Record<string, unknown> => {
    if (canSeeCosts(principal)) {
      return { ...part };
    }

    const visible: Partial<FakeCatalogPart> = { ...part };
    delete visible.cost;
    return visible;
  };

  const toDto = (kind: Kind, item: unknown, principal: FakePrincipal): unknown =>
    kind === 'packages'
      ? packageDto(item as FakeCatalogPackage)
      : kind === 'parts'
        ? partDto(item as FakeCatalogPart, principal)
        : item;

  const listOf = (kind: Kind): (FakeCatalogService | FakeCatalogPart | FakeCatalogPackage)[] =>
    catalog()[kind];

  const find = (kind: Kind, id: string) => listOf(kind).find((item) => item.id === id);

  const record = (
    kind: Kind,
    itemId: string,
    field: FakePriceField,
    oldValue: number | null,
    newValue: number,
    principal: FakePrincipal,
  ): void => {
    if (oldValue === newValue) {
      return;
    }

    catalog().priceHistory.push({
      kind,
      itemId,
      field,
      oldValue,
      newValue,
      changedBy: nameOfPrincipal(principal),
      changedAt: fakeNow(),
    });
  };

  const activePackagesUsing = (kind: Kind, id: string): FakeCatalogPackage[] =>
    kind === 'packages'
      ? []
      : catalog().packages.filter(
          (pack) =>
            pack.isActive &&
            pack.items.some(
              (item) =>
                item.itemId === id && item.kind === (kind === 'services' ? 'Service' : 'Part'),
            ),
        );

  const readService = (
    body: Readonly<Record<string, unknown>>,
  ): Omit<FakeCatalogService, 'id' | 'isActive' | 'createdAt' | 'updatedAt'> | FieldErrors => {
    const errors: FieldErrors = [];
    const name = nameOf(body, errors);
    const basePrice = money(body, 'basePrice', errors);
    const minutes = readNumber(body, 'estimatedMinutes');
    const commission = readNumber(body, 'commissionPercent');
    const warranty = readNumber(body, 'warrantyDays');
    const usual = Array.isArray(body['usualParts']) ? (body['usualParts'] as unknown[]) : null;
    if (minutes === null || !Number.isInteger(minutes) || minutes <= 0) {
      errors.push(fieldError('estimatedMinutes', 'catalog.invalidMinutes', text.invalidMinutes));
    }

    if (commission === null || commission < 0 || commission > 50) {
      errors.push(
        fieldError('commissionPercent', 'catalog.invalidCommission', text.invalidCommission),
      );
    }

    if (warranty === null || !Number.isInteger(warranty) || warranty < 0 || warranty > 3650) {
      errors.push(fieldError('warrantyDays', 'catalog.invalidWarranty', text.invalidWarranty));
    }

    const usualParts = (usual ?? []).map((item) => {
      const entry =
        typeof item === 'object' && item !== null ? (item as Record<string, unknown>) : {};
      return { partId: String(entry['partId'] ?? ''), quantity: entry['quantity'] };
    });
    if (
      usual === null ||
      usualParts.some(
        (item) =>
          !isQuantity(item.quantity) || !catalog().parts.some((part) => part.id === item.partId),
      )
    ) {
      errors.push(fieldError('usualParts', 'catalog.invalidItems', text.invalidCatalogItems));
    }

    return errors.length > 0 || minutes === null || commission === null || warranty === null
      ? errors
      : {
          name,
          description: readNullableText(body, 'description'),
          basePrice,
          estimatedMinutes: minutes,
          commissionPercent: commission,
          warrantyDays: warranty,
          usualParts: usualParts.map((item) => ({
            partId: item.partId,
            quantity: Number(item.quantity),
          })),
        };
  };

  const readPart = (
    body: Readonly<Record<string, unknown>>,
  ):
    | Omit<FakeCatalogPart, 'id' | 'isActive' | 'createdAt' | 'updatedAt' | 'stockQuantity'>
    | FieldErrors => {
    const errors: FieldErrors = [];
    const code = readText(body, 'code').trim().toUpperCase();
    const name = nameOf(body, errors);
    const type = body['type'] === 'Part' || body['type'] === 'Supply' ? body['type'] : null;
    const unit = readText(body, 'unitOfMeasure').trim();
    const unitPrice = money(body, 'unitPrice', errors);
    const cost = money(body, 'cost', errors);
    const minimum = readNumber(body, 'minimumStock');
    const supplier = readNullableText(body, 'supplier');
    const barcode = readNullableText(body, 'barcode');
    if (!CODE.test(code)) {
      errors.push(fieldError('code', 'inventory.invalidPartCode', text.invalidPartCode));
    }

    if (!type) {
      errors.push(fieldError('type', 'inventory.invalidPartType', text.invalidField));
    }

    if (unit.length < 1 || unit.length > 10) {
      errors.push(fieldError('unitOfMeasure', 'inventory.invalidUnit', text.invalidUnit));
    }

    if (minimum === null || minimum < 0) {
      errors.push(fieldError('minimumStock', 'inventory.invalidMinimumStock', text.invalidField));
    }

    if (supplier && supplier.length > 120) {
      errors.push(fieldError('supplier', 'catalog.invalidSupplier', text.invalidField));
    }

    if (barcode && !isValidEan13(barcode)) {
      errors.push(fieldError('barcode', 'catalog.invalidBarcode', text.invalidBarcode));
    }

    return errors.length > 0 || !type || minimum === null
      ? errors
      : {
          code,
          name,
          type,
          unitOfMeasure: unit,
          unitPrice,
          cost,
          minimumStock: minimum,
          supplier,
          barcode,
        };
  };

  const readPackage = (
    body: Readonly<Record<string, unknown>>,
  ): Omit<FakeCatalogPackage, 'id' | 'isActive' | 'createdAt' | 'updatedAt'> | FieldErrors => {
    const errors: FieldErrors = [];
    const name = nameOf(body, errors);
    const price = money(body, 'price', errors);
    const raw = Array.isArray(body['items']) ? (body['items'] as unknown[]) : [];
    const items = raw.map((item): FakePackageItem | null => {
      const entry =
        typeof item === 'object' && item !== null ? (item as Record<string, unknown>) : {};
      const kind = entry['kind'] === 'Service' || entry['kind'] === 'Part' ? entry['kind'] : null;
      const itemId = String(entry['itemId'] ?? '');
      const quantity = entry['quantity'];
      const target =
        kind === 'Service'
          ? catalog().services.find((service) => service.id === itemId)
          : catalog().parts.find((part) => part.id === itemId);
      return kind && isQuantity(quantity) && target?.isActive ? { kind, itemId, quantity } : null;
    });
    const valid = items.filter((item): item is FakePackageItem => item !== null);
    if (raw.length === 0 || valid.length !== raw.length) {
      errors.push(fieldError('items', 'catalog.invalidItems', text.invalidPackageItems));
    }

    const suggestionBody =
      typeof body['suggestion'] === 'object' && body['suggestion'] !== null
        ? (body['suggestion'] as Record<string, unknown>)
        : null;
    const minMileage = suggestionBody ? readNumber(suggestionBody, 'minMileage') : null;
    const maxMileage = suggestionBody ? readNumber(suggestionBody, 'maxMileage') : null;
    if (
      (minMileage !== null && minMileage < 0) ||
      (maxMileage !== null && maxMileage < 0) ||
      (minMileage !== null && maxMileage !== null && minMileage > maxMileage)
    ) {
      errors.push(
        fieldError('suggestion', 'catalog.invalidMileageRange', text.invalidMileageRange),
      );
    }

    return errors.length > 0
      ? errors
      : {
          name,
          description: readNullableText(body, 'description'),
          items: valid,
          price,
          suggestion:
            minMileage === null && maxMileage === null ? null : { minMileage, maxMileage },
        };
  };

  const router = new FakeRouter()
    .on('GET', CATALOG_PATHS.collection, (request, params) => {
      const principal = authorize(request, users(), readers);
      const kind = readKind(params['kind']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      if (!kind) {
        return problem(404, 'common.notFound', messages.errors.notFound);
      }

      const term = normalize(request.query.get('term') ?? '');
      const status = request.query.get('status');
      if (status && status !== 'Active' && status !== 'Inactive') {
        return validation([fieldError('status', 'common.validationFailed', text.invalidField)]);
      }

      const items = listOf(kind)
        .filter(
          (item) =>
            !term ||
            normalize(item.name).includes(term) ||
            ('code' in item && normalize(item.code).includes(term)),
        )
        .filter((item) => !status || item.isActive === (status === 'Active'))
        .sort((left, right) => left.name.localeCompare(right.name, 'pt-BR'))
        .map((item) => toDto(kind, item, principal));
      return paginate(items, request.query);
    })
    .on('GET', CATALOG_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const kind = readKind(params['kind']);
      const item = kind ? find(kind, params['id'] ?? '') : undefined;
      return kind && item
        ? ok(toDto(kind, item, principal))
        : problem(404, 'catalog.notFound', text.catalogItemNotFound);
    })
    .on('POST', CATALOG_PATHS.collection, (request, params) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const kind = readKind(params['kind']);
      if (!kind) {
        return problem(404, 'common.notFound', messages.errors.notFound);
      }

      const body = readBody(request);
      const now = fakeNow();
      const base = { id: fakeId(), isActive: true, createdAt: now, updatedAt: null };
      if (kind === 'services') {
        const input = readService(body);
        if (Array.isArray(input)) {
          return validation(input);
        }

        const item: FakeCatalogService = { ...base, ...input };
        catalog().services.push(item);
        record(kind, item.id, 'BasePrice', null, item.basePrice, principal);
        return created(item);
      }

      if (kind === 'parts') {
        const input = readPart(body);
        if (Array.isArray(input)) {
          return validation(input);
        }

        if (catalog().parts.some((part) => part.code === input.code)) {
          return problem(409, 'inventory.duplicatePartCode', text.duplicatePartCode, [
            fieldError('code', 'inventory.duplicatePartCode', text.duplicatePartCode),
          ]);
        }

        const item: FakeCatalogPart = { ...base, ...input, stockQuantity: 0 };
        catalog().parts.push(item);
        record(kind, item.id, 'Cost', null, item.cost, principal);
        record(kind, item.id, 'UnitPrice', null, item.unitPrice, principal);
        return created(partDto(item, principal));
      }

      const input = readPackage(body);
      if (Array.isArray(input)) {
        return validation(input);
      }

      const item: FakeCatalogPackage = { ...base, ...input };
      catalog().packages.push(item);
      record(kind, item.id, 'PackagePrice', null, item.price, principal);
      return created(packageDto(item));
    })
    .on('PUT', CATALOG_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const kind = readKind(params['kind']);
      const id = params['id'] ?? '';
      const body = readBody(request);
      const updatedAt = fakeNow();
      if (kind === 'services') {
        const item = catalog().services.find((service) => service.id === id);
        const input = item ? readService(body) : null;
        if (!item || !input) {
          return problem(404, 'catalog.notFound', text.catalogItemNotFound);
        }

        if (Array.isArray(input)) {
          return validation(input);
        }

        record(kind, id, 'BasePrice', item.basePrice, input.basePrice, principal);
        Object.assign(item, input, { updatedAt });
        return ok(item);
      }

      if (kind === 'parts') {
        const item = catalog().parts.find((part) => part.id === id);
        const input = item ? readPart(body) : null;
        if (!item || !input) {
          return problem(404, 'catalog.notFound', text.catalogItemNotFound);
        }

        if (Array.isArray(input)) {
          return validation(input);
        }

        if (catalog().parts.some((part) => part.id !== id && part.code === input.code)) {
          return problem(409, 'inventory.duplicatePartCode', text.duplicatePartCode, [
            fieldError('code', 'inventory.duplicatePartCode', text.duplicatePartCode),
          ]);
        }

        record(kind, id, 'Cost', item.cost, input.cost, principal);
        record(kind, id, 'UnitPrice', item.unitPrice, input.unitPrice, principal);
        Object.assign(item, input, { updatedAt });
        return ok(partDto(item, principal));
      }

      const item =
        kind === 'packages' ? catalog().packages.find((pack) => pack.id === id) : undefined;
      const input = item ? readPackage(body) : null;
      if (!item || !input) {
        return problem(404, 'catalog.notFound', text.catalogItemNotFound);
      }

      if (Array.isArray(input)) {
        return validation(input);
      }

      record('packages', id, 'PackagePrice', item.price, input.price, principal);
      Object.assign(item, input, { updatedAt });
      return ok(packageDto(item));
    })
    .on('POST', CATALOG_PATHS.deactivate, (request, params) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const kind = readKind(params['kind']);
      const item = kind ? find(kind, params['id'] ?? '') : undefined;
      if (!kind || !item) {
        return problem(404, 'catalog.notFound', text.catalogItemNotFound);
      }

      if (!item.isActive) {
        return problem(409, 'catalog.alreadyInactive', text.catalogAlreadyInactive);
      }

      const blocking = activePackagesUsing(kind, item.id);
      if (blocking.length > 0) {
        const names = blocking.map((pack) => pack.name);
        return problem(
          409,
          'catalog.usedByActivePackage',
          text.usedByActivePackage(names.join(', ')),
          [],
          undefined,
          { packages: names },
        );
      }

      Object.assign(item, { isActive: false, updatedAt: fakeNow() });
      return noContent();
    })
    .on('POST', CATALOG_PATHS.reactivate, (request, params) => {
      const principal = authorize(request, users(), managers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const kind = readKind(params['kind']);
      const item = kind ? find(kind, params['id'] ?? '') : undefined;
      if (!kind || !item) {
        return problem(404, 'catalog.notFound', text.catalogItemNotFound);
      }

      if (item.isActive) {
        return problem(409, 'catalog.alreadyActive', text.catalogAlreadyActive);
      }

      Object.assign(item, { isActive: true, updatedAt: fakeNow() });
      return noContent();
    })
    .on('GET', CATALOG_PATHS.history, (request, params) => {
      const principal = authorize(request, users(), readers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const kind = readKind(params['kind']);
      const id = params['id'] ?? '';
      if (!kind || !find(kind, id)) {
        return problem(404, 'catalog.notFound', text.catalogItemNotFound);
      }

      const items: FakePriceChange[] = catalog()
        .priceHistory.filter((entry) => entry.kind === kind && entry.itemId === id)
        .filter((entry) => entry.field !== 'Cost' || canSeeCosts(principal))
        .reverse()
        .sort((left, right) => right.changedAt.localeCompare(left.changedAt));
      return ok({
        items: items.map(({ field, oldValue, newValue, changedBy, changedAt }) => ({
          field,
          oldValue,
          newValue,
          changedBy,
          changedAt,
        })),
      });
    });

  return new FakeFeatureApi('catalog', router, state.scenarios);
}
