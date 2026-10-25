import { required } from '../../../core/data/contract-error';
import { Page } from '../../../core/data/page';
import { Cents, centsFromDecimal, centsToDecimal } from '../../../shared/util/money';
import {
  CatalogKind,
  CatalogPageDto,
  PackageDto,
  PackageInputDto,
  PartDto,
  PartInputDto,
  PriceChangeDto,
  ServiceDto,
  ServiceInputDto,
} from './catalog.contracts';
import {
  CatalogInputMap,
  CatalogItemMap,
  CatalogPackage,
  CatalogPart,
  CatalogService,
  PackageInput,
  PartInput,
  PriceChange,
  ServiceInput,
} from './catalog.models';

function money(value: number | undefined, field: string): Cents {
  return centsFromDecimal(required(value, field));
}

export function toCatalogService(dto: ServiceDto): CatalogService {
  return {
    id: required(dto.id, 'id'),
    name: required(dto.name, 'name'),
    description: dto.description ?? null,
    basePrice: money(dto.basePrice, 'basePrice'),
    estimatedMinutes: required(dto.estimatedMinutes, 'estimatedMinutes'),
    commissionPercent: dto.commissionPercent ?? 0,
    warrantyDays: dto.warrantyDays ?? 0,
    usualParts: (dto.usualParts ?? []).map((part) => ({
      partId: required(part.partId, 'partId'),
      quantity: required(part.quantity, 'quantity'),
    })),
    isActive: required(dto.isActive, 'isActive'),
    createdAt: required(dto.createdAt, 'createdAt'),
    updatedAt: dto.updatedAt ?? null,
  };
}

export function toCatalogPart(dto: PartDto): CatalogPart {
  return {
    id: required(dto.id, 'id'),
    code: required(dto.code, 'code'),
    name: required(dto.name, 'name'),
    type: required(dto.type, 'type'),
    unitOfMeasure: required(dto.unitOfMeasure, 'unitOfMeasure'),
    unitPrice: money(dto.unitPrice, 'unitPrice'),
    cost: dto.cost === undefined || dto.cost === null ? null : centsFromDecimal(dto.cost),
    stockQuantity: dto.stockQuantity ?? 0,
    minimumStock: dto.minimumStock ?? 0,
    supplier: dto.supplier ?? null,
    barcode: dto.barcode ?? null,
    isActive: required(dto.isActive, 'isActive'),
    createdAt: required(dto.createdAt, 'createdAt'),
    updatedAt: dto.updatedAt ?? null,
  };
}

export function toCatalogPackage(dto: PackageDto): CatalogPackage {
  return {
    id: required(dto.id, 'id'),
    name: required(dto.name, 'name'),
    description: dto.description ?? null,
    items: (dto.items ?? []).map((item) => ({
      kind: required(item.kind, 'kind'),
      itemId: required(item.itemId, 'itemId'),
      quantity: required(item.quantity, 'quantity'),
      name: required(item.name, 'name'),
      unitPrice: money(item.unitPrice, 'unitPrice'),
    })),
    price: money(dto.price, 'price'),
    itemsTotal: money(dto.itemsTotal, 'itemsTotal'),
    savings: money(dto.savings, 'savings'),
    suggestion: dto.suggestion
      ? {
          minMileage: dto.suggestion.minMileage ?? null,
          maxMileage: dto.suggestion.maxMileage ?? null,
        }
      : null,
    isActive: required(dto.isActive, 'isActive'),
    createdAt: required(dto.createdAt, 'createdAt'),
    updatedAt: dto.updatedAt ?? null,
  };
}

export function toPriceChange(dto: PriceChangeDto): PriceChange {
  return {
    field: required(dto.field, 'field'),
    oldValue:
      dto.oldValue === undefined || dto.oldValue === null ? null : centsFromDecimal(dto.oldValue),
    newValue: money(dto.newValue, 'newValue'),
    changedBy: required(dto.changedBy, 'changedBy'),
    changedAt: required(dto.changedAt, 'changedAt'),
  };
}

export const CATALOG_MAPPERS: {
  readonly [K in CatalogKind]: (dto: never) => CatalogItemMap[K];
} = {
  services: toCatalogService,
  packages: toCatalogPackage,
  parts: toCatalogPart,
};

export function toCatalogItem<K extends CatalogKind>(kind: K, dto: unknown): CatalogItemMap[K] {
  const mapper = CATALOG_MAPPERS[kind] as (value: unknown) => CatalogItemMap[K];
  return mapper(dto);
}

export function toCatalogPage<K extends CatalogKind>(
  kind: K,
  dto: CatalogPageDto<unknown>,
): Page<CatalogItemMap[K]> {
  return {
    items: (dto.items ?? []).map((item) => toCatalogItem(kind, item)),
    page: required(dto.page, 'page'),
    pageSize: required(dto.pageSize, 'pageSize'),
    totalItems: required(dto.totalItems, 'totalItems'),
  };
}

function toServiceInputDto(input: ServiceInput): ServiceInputDto {
  return {
    ...input,
    basePrice: centsToDecimal(input.basePrice),
    usualParts: input.usualParts.map((part) => ({ ...part })),
  };
}

function toPartInputDto(input: PartInput): PartInputDto {
  return {
    ...input,
    unitPrice: centsToDecimal(input.unitPrice),
    cost: centsToDecimal(input.cost),
  };
}

function toPackageInputDto(input: PackageInput): PackageInputDto {
  return {
    name: input.name,
    description: input.description,
    items: input.items.map((item) => ({
      kind: item.kind,
      itemId: item.itemId,
      quantity: item.quantity,
    })),
    price: centsToDecimal(input.price),
    suggestion: input.suggestion ? { ...input.suggestion } : null,
  };
}

export function toCatalogInputDto<K extends CatalogKind>(
  kind: K,
  input: CatalogInputMap[K],
): ServiceInputDto | PartInputDto | PackageInputDto {
  switch (kind) {
    case 'services':
      return toServiceInputDto(input as ServiceInput);
    case 'parts':
      return toPartInputDto(input as PartInput);
    default:
      return toPackageInputDto(input as PackageInput);
  }
}
