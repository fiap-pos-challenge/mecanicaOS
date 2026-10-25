import {
  ApprovalStatus,
  DeliveryChannel,
  DeliveryStatus,
  DiscountCondition,
  DiscountMode,
  PaymentMethod,
  QuoteItemKind,
  QuoteItemSource,
  QuoteStatus,
} from './quote.models';

export type DiscountTypeDto = 'Percent' | 'Amount';

export interface DiscountDto {
  readonly type?: DiscountTypeDto;
  readonly value?: number;
}

export interface ManualDiscountDto extends DiscountDto {
  readonly reason?: string;
}

export interface QuoteLineDto {
  readonly id?: string;
  readonly kind?: QuoteItemKind;
  readonly itemId?: string;
  readonly name?: string;
  readonly unitPrice?: number;
  readonly quantity?: number;
  readonly source?: QuoteItemSource;
  readonly fractional?: boolean;
}

export interface QuoteCustomerDto {
  readonly id?: string;
  readonly name?: string;
  readonly maskedPhone?: string | null;
  readonly maskedEmail?: string | null;
  readonly consentWhatsApp?: boolean;
  readonly consentEmail?: boolean;
  readonly loyal?: boolean;
  readonly firstService?: boolean;
  readonly fleet?: boolean;
}

export interface QuoteVehicleDto {
  readonly licensePlate?: string;
  readonly description?: string;
  readonly mileage?: number;
}

export interface DiscountApprovalDto {
  readonly id?: string;
  readonly status?: ApprovalStatus;
  readonly discount?: DiscountDto;
  readonly reason?: string;
  readonly requestedBy?: string;
  readonly requestedAt?: string;
  readonly decidedBy?: string | null;
}

export interface QuoteDeliveryDto {
  readonly id?: string;
  readonly channel?: DeliveryChannel;
  readonly status?: DeliveryStatus;
  readonly link?: string;
  readonly expiresAt?: string;
  readonly sentAt?: string | null;
  readonly openedAt?: string | null;
  readonly failureReason?: string | null;
  readonly attempts?: number;
}

export interface PaymentPlanDto {
  readonly methods?: readonly PaymentMethod[];
  readonly maxInstallments?: number;
}

export interface QuoteDto {
  readonly id?: string;
  readonly number?: number;
  readonly serviceOrderId?: string;
  readonly serviceOrderNumber?: number;
  readonly version?: number;
  readonly status?: QuoteStatus;
  readonly customer?: QuoteCustomerDto;
  readonly vehicle?: QuoteVehicleDto;
  readonly diagnosis?: string | null;
  readonly items?: readonly QuoteLineDto[];
  readonly discountMode?: DiscountMode;
  readonly manualDiscount?: ManualDiscountDto | null;
  readonly payment?: PaymentPlanDto;
  readonly approval?: DiscountApprovalDto | null;
  readonly delivery?: QuoteDeliveryDto | null;
  readonly rejectionReason?: string | null;
  readonly servicesTotal?: number;
  readonly partsTotal?: number;
  readonly discountTotal?: number;
  readonly total?: number;
  readonly updatedAt?: string;
}

export interface QuoteSummaryDto {
  readonly id?: string;
  readonly number?: number;
  readonly version?: number;
  readonly status?: QuoteStatus;
  readonly customerName?: string;
  readonly licensePlate?: string;
  readonly total?: number;
  readonly deliveryStatus?: DeliveryStatus | null;
  readonly approvalPending?: boolean;
  readonly updatedAt?: string;
}

export interface QuotePageDto {
  readonly items?: readonly QuoteSummaryDto[];
  readonly page?: number;
  readonly pageSize?: number;
  readonly totalItems?: number;
}

export interface QuoteVersionDto {
  readonly id?: string;
  readonly version?: number;
  readonly status?: QuoteStatus;
  readonly items?: readonly QuoteLineDto[];
  readonly servicesTotal?: number;
  readonly partsTotal?: number;
  readonly discountTotal?: number;
  readonly total?: number;
  readonly rejectionReason?: string | null;
  readonly createdAt?: string;
}

export interface QuoteOptionDto {
  readonly kind?: QuoteItemKind;
  readonly itemId?: string;
  readonly name?: string;
  readonly detail?: string | null;
  readonly unitPrice?: number;
  readonly fractional?: boolean;
  readonly stock?: {
    readonly available?: number;
    readonly leadTimeDays?: number | null;
    readonly supplier?: string | null;
  } | null;
  readonly reason?: string;
}

export interface PaymentFeeDto {
  readonly method?: PaymentMethod;
  readonly feePercent?: number;
  readonly settlementDays?: number;
}

export interface PaymentSettingsDto {
  readonly methods?: readonly PaymentMethod[];
  readonly maxInstallments?: number;
  readonly interestFreeInstallments?: number;
  readonly monthlyInterestPercent?: number;
  readonly fees?: readonly PaymentFeeDto[];
}

export interface DiscountRuleDto extends DiscountDto {
  readonly id?: string;
  readonly name?: string;
  readonly condition?: DiscountCondition;
  readonly isActive?: boolean;
}

export interface QuoteOptionsDto {
  readonly packages?: readonly QuoteOptionDto[];
  readonly services?: readonly QuoteOptionDto[];
  readonly parts?: readonly QuoteOptionDto[];
  readonly suggestions?: readonly QuoteOptionDto[];
  readonly discountRules?: readonly DiscountRuleDto[];
  readonly discountLimitPercent?: number;
  readonly payment?: PaymentSettingsDto;
  readonly expirationDays?: readonly number[];
}

export interface OpenServiceOrderDto {
  readonly id?: string;
  readonly number?: number;
  readonly customerName?: string;
  readonly licensePlate?: string;
  readonly vehicle?: string;
}

export interface SendQuoteResultDto {
  readonly delivery?: QuoteDeliveryDto;
  readonly whatsAppUrl?: string | null;
}

export interface QuoteDraftInputDto {
  readonly items: readonly {
    readonly kind: QuoteItemKind;
    readonly itemId: string;
    readonly quantity: number;
    readonly unitPrice: number;
    readonly source: QuoteItemSource;
  }[];
  readonly discountMode: DiscountMode;
  readonly manualDiscount: {
    readonly type: DiscountTypeDto;
    readonly value: number;
    readonly reason: string;
  } | null;
  readonly payment: {
    readonly methods: readonly PaymentMethod[];
    readonly maxInstallments: number;
  };
}

export interface QuoteListData {
  readonly url: '/api/admin/quotes';
  readonly query: {
    readonly page: number;
    readonly pageSize: number;
    readonly status?: QuoteStatus;
  };
}

export interface QuoteCreateData {
  readonly url: '/api/admin/quotes';
  readonly body: { readonly serviceOrderId: string };
}

export interface OpenServiceOrdersData {
  readonly url: '/api/admin/quotes/service-orders';
}

export interface QuoteItemData {
  readonly url:
    | '/api/admin/quotes/{id}'
    | '/api/admin/quotes/{id}/options'
    | '/api/admin/quotes/{id}/versions'
    | '/api/admin/quotes/{id}/delivery'
    | '/api/admin/quotes/{id}/delivery/retry'
    | '/api/admin/quotes/{id}/approve';
  readonly path: { readonly id: string };
}

export interface QuoteDraftData {
  readonly url: '/api/admin/quotes/{id}/draft';
  readonly path: { readonly id: string };
  readonly body: QuoteDraftInputDto;
}

export interface QuoteRejectData {
  readonly url: '/api/admin/quotes/{id}/reject';
  readonly path: { readonly id: string };
  readonly body: { readonly reason: string };
}

export interface DiscountApprovalRequestData {
  readonly url: '/api/admin/quotes/{id}/discount-approvals';
  readonly path: { readonly id: string };
  readonly body: {
    readonly type: DiscountTypeDto;
    readonly value: number;
    readonly reason: string;
  };
}

export interface DiscountApprovalDecisionData {
  readonly url: '/api/admin/quotes/{id}/discount-approvals/{approvalId}/{action}';
  readonly path: {
    readonly id: string;
    readonly approvalId: string;
    readonly action: 'approve' | 'reject';
  };
}

export interface MessageDraftData {
  readonly url: '/api/admin/quotes/{id}/message-draft';
  readonly path: { readonly id: string };
  readonly body: { readonly channel: DeliveryChannel };
}

export interface SendQuoteData {
  readonly url: '/api/admin/quotes/{id}/send';
  readonly path: { readonly id: string };
  readonly body: {
    readonly channel: DeliveryChannel;
    readonly expiresInDays: number;
    readonly message: string;
  };
}
