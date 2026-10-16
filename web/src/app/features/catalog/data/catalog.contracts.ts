export type CatalogKind = 'services' | 'packages' | 'parts';

export type PartTypeDto = 'Part' | 'Supply';

export type PackageItemKindDto = 'Service' | 'Part';

export type PriceFieldDto = 'BasePrice' | 'PackagePrice' | 'Cost' | 'UnitPrice';

export type CatalogStatusDto = 'Active' | 'Inactive';

export interface UsualPartDto {
  readonly partId?: string;
  readonly quantity?: number;
}

export interface ServiceDto {
  readonly id?: string;
  readonly name?: string;
  readonly description?: string | null;
  readonly basePrice?: number;
  readonly estimatedMinutes?: number;
  readonly commissionPercent?: number;
  readonly warrantyDays?: number;
  readonly usualParts?: readonly UsualPartDto[] | null;
  readonly isActive?: boolean;
  readonly createdAt?: string;
  readonly updatedAt?: string | null;
}

export interface PartDto {
  readonly id?: string;
  readonly code?: string;
  readonly name?: string;
  readonly type?: PartTypeDto;
  readonly unitOfMeasure?: string;
  readonly unitPrice?: number;
  readonly cost?: number | null;
  readonly stockQuantity?: number;
  readonly minimumStock?: number;
  readonly supplier?: string | null;
  readonly barcode?: string | null;
  readonly isActive?: boolean;
  readonly createdAt?: string;
  readonly updatedAt?: string | null;
}

export interface PackageItemDto {
  readonly kind?: PackageItemKindDto;
  readonly itemId?: string;
  readonly quantity?: number;
  readonly name?: string;
  readonly unitPrice?: number;
}

export interface PackageSuggestionDto {
  readonly minMileage?: number | null;
  readonly maxMileage?: number | null;
}

export interface PackageDto {
  readonly id?: string;
  readonly name?: string;
  readonly description?: string | null;
  readonly items?: readonly PackageItemDto[] | null;
  readonly price?: number;
  readonly itemsTotal?: number;
  readonly savings?: number;
  readonly suggestion?: PackageSuggestionDto | null;
  readonly isActive?: boolean;
  readonly createdAt?: string;
  readonly updatedAt?: string | null;
}

export interface CatalogPageDto<T> {
  readonly items?: readonly T[] | null;
  readonly page?: number;
  readonly pageSize?: number;
  readonly totalItems?: number;
}

export interface PriceChangeDto {
  readonly field?: PriceFieldDto;
  readonly oldValue?: number | null;
  readonly newValue?: number;
  readonly changedBy?: string;
  readonly changedAt?: string;
}

export interface PriceHistoryDto {
  readonly items?: readonly PriceChangeDto[] | null;
}

export interface ServiceInputDto {
  readonly name: string;
  readonly description: string | null;
  readonly basePrice: number;
  readonly estimatedMinutes: number;
  readonly commissionPercent: number;
  readonly warrantyDays: number;
  readonly usualParts: readonly { readonly partId: string; readonly quantity: number }[];
}

export interface PartInputDto {
  readonly code: string;
  readonly name: string;
  readonly type: PartTypeDto;
  readonly unitOfMeasure: string;
  readonly unitPrice: number;
  readonly cost: number;
  readonly minimumStock: number;
  readonly supplier: string | null;
  readonly barcode: string | null;
}

export interface PackageInputDto {
  readonly name: string;
  readonly description: string | null;
  readonly items: readonly {
    readonly kind: PackageItemKindDto;
    readonly itemId: string;
    readonly quantity: number;
  }[];
  readonly price: number;
  readonly suggestion: {
    readonly minMileage: number | null;
    readonly maxMileage: number | null;
  } | null;
}

export interface CatalogListData {
  readonly url: '/api/admin/catalog/{kind}';
  readonly path: { readonly kind: CatalogKind };
  readonly query: {
    readonly page: number;
    readonly pageSize: number;
    readonly term?: string;
    readonly status?: CatalogStatusDto;
  };
}

export interface CatalogCreateData {
  readonly url: '/api/admin/catalog/{kind}';
  readonly path: { readonly kind: CatalogKind };
  readonly body: ServiceInputDto | PartInputDto | PackageInputDto;
}

export interface CatalogItemData {
  readonly url: '/api/admin/catalog/{kind}/{id}';
  readonly path: { readonly kind: CatalogKind; readonly id: string };
  readonly body?: ServiceInputDto | PartInputDto | PackageInputDto;
}

export interface CatalogActivationData {
  readonly url: '/api/admin/catalog/{kind}/{id}/{action}';
  readonly path: {
    readonly kind: CatalogKind;
    readonly id: string;
    readonly action: 'deactivate' | 'reactivate';
  };
}

export interface PriceHistoryData {
  readonly url: '/api/admin/catalog/{kind}/{id}/price-history';
  readonly path: { readonly kind: CatalogKind; readonly id: string };
}
