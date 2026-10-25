import { PageRequest } from '../../../core/data/page';
import { Cents } from '../../../shared/util/money';
import { CatalogKind, PackageItemKindDto, PartTypeDto, PriceFieldDto } from './catalog.contracts';

export type { CatalogKind } from './catalog.contracts';

export type PartType = PartTypeDto;
export type PackageItemKind = PackageItemKindDto;
export type PriceField = PriceFieldDto;
export type CatalogStatus = 'Active' | 'Inactive';

export const CATALOG_KINDS: readonly CatalogKind[] = ['services', 'packages', 'parts'];
export const PART_TYPES: readonly PartType[] = ['Part', 'Supply'];

export interface CatalogQuery extends PageRequest {
  readonly term: string;
  readonly status: CatalogStatus | null;
}

export interface UsualPart {
  readonly partId: string;
  readonly quantity: number;
}

interface CatalogRecord {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface CatalogService extends CatalogRecord {
  readonly description: string | null;
  readonly basePrice: Cents;
  readonly estimatedMinutes: number;
  readonly commissionPercent: number;
  readonly warrantyDays: number;
  readonly usualParts: readonly UsualPart[];
}

export interface CatalogPart extends CatalogRecord {
  readonly code: string;
  readonly type: PartType;
  readonly unitOfMeasure: string;
  readonly unitPrice: Cents;
  readonly cost: Cents | null;
  readonly stockQuantity: number;
  readonly minimumStock: number;
  readonly supplier: string | null;
  readonly barcode: string | null;
}

export interface PackageItem {
  readonly kind: PackageItemKind;
  readonly itemId: string;
  readonly quantity: number;
  readonly name: string;
  readonly unitPrice: Cents;
}

export interface MileageRange {
  readonly minMileage: number | null;
  readonly maxMileage: number | null;
}

export interface CatalogPackage extends CatalogRecord {
  readonly description: string | null;
  readonly items: readonly PackageItem[];
  readonly price: Cents;
  readonly itemsTotal: Cents;
  readonly savings: Cents;
  readonly suggestion: MileageRange | null;
}

export interface CatalogItemMap {
  readonly services: CatalogService;
  readonly packages: CatalogPackage;
  readonly parts: CatalogPart;
}

export interface ServiceInput {
  readonly name: string;
  readonly description: string | null;
  readonly basePrice: Cents;
  readonly estimatedMinutes: number;
  readonly commissionPercent: number;
  readonly warrantyDays: number;
  readonly usualParts: readonly UsualPart[];
}

export interface PartInput {
  readonly code: string;
  readonly name: string;
  readonly type: PartType;
  readonly unitOfMeasure: string;
  readonly unitPrice: Cents;
  readonly cost: Cents;
  readonly minimumStock: number;
  readonly supplier: string | null;
  readonly barcode: string | null;
}

export interface PackageInput {
  readonly name: string;
  readonly description: string | null;
  readonly items: readonly Pick<PackageItem, 'kind' | 'itemId' | 'quantity'>[];
  readonly price: Cents;
  readonly suggestion: MileageRange | null;
}

export interface CatalogInputMap {
  readonly services: ServiceInput;
  readonly packages: PackageInput;
  readonly parts: PartInput;
}

export interface PriceChange {
  readonly field: PriceField;
  readonly oldValue: Cents | null;
  readonly newValue: Cents;
  readonly changedBy: string;
  readonly changedAt: string;
}
