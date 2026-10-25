import { addCents, cents, Cents } from '../../../shared/util/money';
import { PaymentMethod } from '../../quotes/data/quote.models';
import { DocumentKind } from '../../documents/data/document.models';
import {
  SERVICE_ORDER_STATUSES,
  ServiceOrderStatus,
} from '../../service-orders/data/service-order.models';

export type ConfirmResult =
  | { readonly kind: 'confirmed'; readonly session: string }
  | { readonly kind: 'wrongDigits'; readonly attemptsLeft: number };

export interface PortalItem {
  readonly id: string;
  readonly name: string;
  readonly kind: 'Service' | 'Part';
  readonly total: Cents;
}

export interface PortalQuote {
  readonly number: number;
  readonly version: number;
  readonly status: 'Pending' | 'Approved' | 'Rejected';
  readonly items: readonly PortalItem[];
  readonly discount: Cents;
  readonly total: Cents;
}

export type PaymentStatus = 'Pending' | 'Paid' | 'PartiallyPaid' | 'Refunded';

export interface PortalPayment {
  readonly status: PaymentStatus;
  readonly amountDue: Cents;
  readonly methods: readonly PaymentMethod[];
}

export interface PortalOrder {
  readonly number: number;
  readonly shopName: string;
  readonly customerFirstName: string;
  readonly vehicle: string;
  readonly plate: string;
  readonly status: ServiceOrderStatus;
  readonly history: readonly { readonly status: ServiceOrderStatus; readonly changedAt: string }[];
  readonly photos: readonly {
    readonly id: string;
    readonly url: string;
    readonly caption: string;
  }[];
  readonly quote: PortalQuote | null;
  readonly payment: PortalPayment | null;
  readonly documents: readonly {
    readonly kind: DocumentKind;
    readonly code: string;
    readonly issuedAt: string;
  }[];
}

export interface PixCharge {
  readonly paymentId: string;
  readonly copyPaste: string;
  readonly qrCodeDataUrl: string;
  readonly expiresAt: string;
}

export type ChargeStatus = 'Pending' | 'Paid' | 'Expired';

export interface PortalStep {
  readonly status: ServiceOrderStatus;
  readonly state: 'done' | 'current' | 'next';
  readonly at: string | null;
}

export function portalSteps(order: Pick<PortalOrder, 'status' | 'history'>): readonly PortalStep[] {
  const current = SERVICE_ORDER_STATUSES.indexOf(order.status);
  return SERVICE_ORDER_STATUSES.map((status, index) => ({
    status,
    state: index < current ? 'done' : index === current ? 'current' : 'next',
    at:
      index <= current
        ? ([...order.history].reverse().find((entry) => entry.status === status)?.changedAt ?? null)
        : null,
  }));
}

export function portalTotal(
  items: readonly PortalItem[],
  selected: ReadonlySet<string>,
  discount: Cents,
): { readonly subtotal: Cents; readonly discount: Cents; readonly total: Cents } {
  const full = addCents(...items.map((item) => item.total));
  const subtotal = addCents(
    ...items.filter((item) => selected.has(item.id)).map((item) => item.total),
  );
  const share = full <= 0 ? cents(0) : cents(Math.round((discount * subtotal) / full));
  const applied = cents(Math.min(share, subtotal));
  return { subtotal, discount: applied, total: cents(subtotal - applied) };
}
