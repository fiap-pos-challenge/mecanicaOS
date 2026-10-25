import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { centsFromDecimal } from '../../../shared/util/money';
import { DocumentKind } from '../../documents/data/document.models';
import { PORTAL_SESSION_HEADER, publicContext } from '../../documents/data/documents.gateway';
import { PaymentMethod } from '../../quotes/data/quote.models';
import { ServiceOrderStatus } from '../../service-orders/data/service-order.models';
import {
  ChargeStatus,
  ConfirmResult,
  PaymentStatus,
  PixCharge,
  PortalOrder,
} from './portal.models';

export interface PortalOrderDto {
  readonly number?: number;
  readonly shopName?: string;
  readonly customerFirstName?: string;
  readonly vehicle?: string;
  readonly licensePlate?: string;
  readonly status?: ServiceOrderStatus;
  readonly statusHistory?: readonly {
    readonly status?: ServiceOrderStatus;
    readonly changedAt?: string;
  }[];
  readonly photos?: readonly {
    readonly id?: string;
    readonly url?: string;
    readonly caption?: string;
  }[];
  readonly quote?: {
    readonly number?: number;
    readonly version?: number;
    readonly status?: 'Pending' | 'Approved' | 'Rejected';
    readonly items?: readonly {
      readonly id?: string;
      readonly name?: string;
      readonly kind?: 'Service' | 'Part';
      readonly total?: number;
    }[];
    readonly discountTotal?: number;
    readonly total?: number;
  } | null;
  readonly payment?: {
    readonly status?: PaymentStatus;
    readonly amountDue?: number;
    readonly methods?: readonly PaymentMethod[];
  } | null;
  readonly documents?: readonly {
    readonly kind?: DocumentKind;
    readonly code?: string;
    readonly issuedAt?: string;
  }[];
}

interface PortalData {
  readonly url:
    | '/api/public/portal/access'
    | '/api/public/portal/confirm'
    | '/api/public/portal/order'
    | '/api/public/portal/quote/approve'
    | '/api/public/portal/quote/reject'
    | '/api/public/portal/payments/pix'
    | '/api/public/portal/payments/card'
    | '/api/public/portal/payments/{id}';
  readonly path?: { readonly id: string };
  readonly body?: unknown;
}

export function toPortalOrder(dto: PortalOrderDto): PortalOrder {
  const quote = dto.quote ?? null;
  const payment = dto.payment ?? null;
  return {
    number: required(dto.number, 'number'),
    shopName: dto.shopName ?? '',
    customerFirstName: dto.customerFirstName ?? '',
    vehicle: dto.vehicle ?? '',
    plate: required(dto.licensePlate, 'licensePlate'),
    status: required(dto.status, 'status'),
    history: (dto.statusHistory ?? []).map((entry) => ({
      status: required(entry.status, 'statusHistory.status'),
      changedAt: required(entry.changedAt, 'statusHistory.changedAt'),
    })),
    photos: (dto.photos ?? []).map((photo) => ({
      id: required(photo.id, 'photos.id'),
      url: required(photo.url, 'photos.url'),
      caption: photo.caption ?? '',
    })),
    quote: quote
      ? {
          number: required(quote.number, 'quote.number'),
          version: quote.version ?? 1,
          status: required(quote.status, 'quote.status'),
          items: (quote.items ?? []).map((item) => ({
            id: required(item.id, 'quote.items.id'),
            name: required(item.name, 'quote.items.name'),
            kind: item.kind ?? 'Service',
            total: centsFromDecimal(item.total ?? 0),
          })),
          discount: centsFromDecimal(quote.discountTotal ?? 0),
          total: centsFromDecimal(quote.total ?? 0),
        }
      : null,
    payment: payment
      ? {
          status: payment.status ?? 'Pending',
          amountDue: centsFromDecimal(payment.amountDue ?? 0),
          methods: payment.methods ?? [],
        }
      : null,
    documents: (dto.documents ?? []).map((document) => ({
      kind: required(document.kind, 'documents.kind'),
      code: required(document.code, 'documents.code'),
      issuedAt: required(document.issuedAt, 'documents.issuedAt'),
    })),
  };
}

export abstract class PortalGateway {
  abstract access(token: string): Observable<number>;
  abstract confirm(token: string, lastDigits: string): Observable<ConfirmResult>;
  abstract order(session: string): Observable<PortalOrder>;
  abstract approve(session: string, itemIds: readonly string[]): Observable<PortalOrder>;
  abstract reject(session: string, reason: string): Observable<PortalOrder>;
  abstract pix(session: string): Observable<PixCharge>;
  abstract charge(session: string, paymentId: string): Observable<ChargeStatus>;
  abstract card(session: string): Observable<string>;
}

@Injectable({ providedIn: 'root' })
export class HttpPortalGateway extends PortalGateway {
  private readonly api = inject(ApiClient);

  access(token: string): Observable<number> {
    return this.call<{ attemptsLeft?: number }>('POST', '/api/public/portal/access', null, {
      token,
    }).pipe(map((dto) => dto.attemptsLeft ?? 3));
  }

  confirm(token: string, lastDigits: string): Observable<ConfirmResult> {
    return this.call<{
      status?: 'Confirmed' | 'WrongDigits';
      session?: string;
      attemptsLeft?: number;
    }>('POST', '/api/public/portal/confirm', null, { token, lastDigits }).pipe(
      map((dto): ConfirmResult =>
        dto.status === 'Confirmed'
          ? { kind: 'confirmed', session: required(dto.session, 'session') }
          : { kind: 'wrongDigits', attemptsLeft: dto.attemptsLeft ?? 0 },
      ),
    );
  }

  order(session: string): Observable<PortalOrder> {
    return this.call<PortalOrderDto>('GET', '/api/public/portal/order', session).pipe(
      map(toPortalOrder),
    );
  }

  approve(session: string, itemIds: readonly string[]): Observable<PortalOrder> {
    return this.call<PortalOrderDto>('POST', '/api/public/portal/quote/approve', session, {
      itemIds,
    }).pipe(map(toPortalOrder));
  }

  reject(session: string, reason: string): Observable<PortalOrder> {
    return this.call<PortalOrderDto>('POST', '/api/public/portal/quote/reject', session, {
      reason: reason.trim(),
    }).pipe(map(toPortalOrder));
  }

  pix(session: string): Observable<PixCharge> {
    return this.call<Partial<PixCharge>>('POST', '/api/public/portal/payments/pix', session).pipe(
      map((dto) => ({
        paymentId: required(dto.paymentId, 'paymentId'),
        copyPaste: required(dto.copyPaste, 'copyPaste'),
        qrCodeDataUrl: required(dto.qrCodeDataUrl, 'qrCodeDataUrl'),
        expiresAt: required(dto.expiresAt, 'expiresAt'),
      })),
    );
  }

  charge(session: string, paymentId: string): Observable<ChargeStatus> {
    return this.api
      .send<PortalData, { status?: ChargeStatus }>('GET', '/api/public/portal/payments/{id}', {
        path: { id: paymentId },
        headers: { [PORTAL_SESSION_HEADER]: session },
        context: publicContext(),
      })
      .pipe(map((dto) => dto.status ?? 'Pending'));
  }

  card(session: string): Observable<string> {
    return this.call<{ redirectUrl?: string }>(
      'POST',
      '/api/public/portal/payments/card',
      session,
    ).pipe(map((dto) => required(dto.redirectUrl, 'redirectUrl')));
  }

  private call<T>(
    method: 'GET' | 'POST',
    url: Exclude<PortalData['url'], '/api/public/portal/payments/{id}'>,
    session: string | null,
    body?: unknown,
  ): Observable<T> {
    return this.api.send<PortalData, T>(method, url, {
      ...(body === undefined ? {} : { body }),
      ...(session ? { headers: { [PORTAL_SESSION_HEADER]: session } } : {}),
      context: publicContext(),
    });
  }
}

export function providePortalData(): Provider {
  return provideGateway(PortalGateway, 'portal', { http: HttpPortalGateway });
}
