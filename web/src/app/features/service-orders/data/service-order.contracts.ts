import { DocumentKind, ServiceOrderStatus, ServiceOrderTag } from './service-order.models';

export interface ServiceOrderCardDto {
  readonly id?: string;
  readonly number?: number;
  readonly status?: ServiceOrderStatus;
  readonly version?: number;
  readonly customerName?: string;
  readonly licensePlate?: string;
  readonly vehicle?: string;
  readonly summary?: string;
  readonly mechanicName?: string | null;
  readonly promisedAt?: string | null;
  readonly total?: number;
  readonly totalAmount?: number;
  readonly hasQuoteItems?: boolean;
  readonly tags?: readonly ServiceOrderTag[];
  readonly updatedAt?: string;
}

export interface ServiceOrderDetailDto extends ServiceOrderCardDto {
  readonly problemDescription?: string;
  readonly mileage?: number;
  readonly statusHistory?: readonly {
    readonly previousStatus?: ServiceOrderStatus | null;
    readonly newStatus?: ServiceOrderStatus;
    readonly changedBy?: string | null;
    readonly note?: string | null;
    readonly changedAt?: string;
  }[];
  readonly quote?: {
    readonly id?: string;
    readonly number?: number;
    readonly version?: number;
    readonly status?: 'Pending' | 'Approved' | 'Rejected';
    readonly total?: number;
    readonly items?: readonly {
      readonly name?: string;
      readonly quantity?: number;
      readonly total?: number;
    }[];
  } | null;
  readonly parts?: readonly {
    readonly name?: string;
    readonly quantity?: number;
    readonly unit?: string;
    readonly reserved?: boolean;
    readonly leadTimeDays?: number | null;
  }[];
  readonly photos?: readonly {
    readonly id?: string;
    readonly url?: string;
    readonly caption?: string;
    readonly takenAt?: string;
  }[];
  readonly timeEntries?: readonly {
    readonly id?: string;
    readonly mechanicName?: string;
    readonly startedAt?: string;
    readonly endedAt?: string | null;
    readonly minutes?: number;
  }[];
  readonly documents?: readonly {
    readonly kind?: DocumentKind;
    readonly code?: string;
    readonly issuedAt?: string;
  }[];
}

export interface ServiceOrderListData {
  readonly url: '/api/admin/service-orders';
  readonly query: { readonly page: number; readonly pageSize: number };
}

export interface ServiceOrderOpenData {
  readonly url: '/api/admin/service-orders';
  readonly body: {
    readonly customerId: string;
    readonly vehicleId: string;
    readonly problemDescription: string;
    readonly promisedAt: string | null;
    readonly mileage: number | null;
  };
}

export interface ServiceOrderItemData {
  readonly url:
    | '/api/admin/service-orders/{id}'
    | '/api/admin/service-orders/{id}/time-entries/start'
    | '/api/admin/service-orders/{id}/time-entries/stop';
  readonly path: { readonly id: string };
}

export interface ServiceOrderTransitionData {
  readonly url: '/api/admin/service-orders/{id}/transitions';
  readonly path: { readonly id: string };
  readonly body: {
    readonly to: ServiceOrderStatus;
    readonly version: number;
    readonly reason: string | null;
  };
}
