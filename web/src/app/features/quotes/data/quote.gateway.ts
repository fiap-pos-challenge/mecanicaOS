import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { Page } from '../../../core/data/page';
import {
  DiscountApprovalDecisionData,
  DiscountApprovalDto,
  DiscountApprovalRequestData,
  MessageDraftData,
  OpenServiceOrderDto,
  OpenServiceOrdersData,
  QuoteCreateData,
  QuoteDeliveryDto,
  QuoteDraftData,
  QuoteDto,
  QuoteItemData,
  QuoteListData,
  QuoteOptionsDto,
  QuotePageDto,
  QuoteRejectData,
  QuoteVersionDto,
  SendQuoteData,
  SendQuoteResultDto,
} from './quote.contracts';
import {
  toApproval,
  toDelivery,
  toDiscountDto,
  toOpenServiceOrder,
  toQuote,
  toQuoteDraftDto,
  toQuoteOptions,
  toQuotePage,
  toQuoteVersion,
  toSendResult,
} from './quote.mappers';
import {
  DeliveryChannel,
  DiscountApproval,
  ManualDiscount,
  OpenServiceOrder,
  Quote,
  QuoteDelivery,
  QuoteDraft,
  QuoteListQuery,
  QuoteOptions,
  QuoteSummary,
  QuoteVersion,
  SendQuoteInput,
  SendQuoteResult,
} from './quote.models';

export abstract class QuoteGateway {
  abstract list(query: QuoteListQuery): Observable<Page<QuoteSummary>>;
  abstract openServiceOrders(): Observable<readonly OpenServiceOrder[]>;
  abstract create(serviceOrderId: string): Observable<Quote>;
  abstract get(id: string): Observable<Quote>;
  abstract options(id: string): Observable<QuoteOptions>;
  abstract saveDraft(id: string, draft: QuoteDraft): Observable<Quote>;
  abstract requestDiscountApproval(
    id: string,
    discount: ManualDiscount,
  ): Observable<DiscountApproval>;
  abstract decideDiscountApproval(
    id: string,
    approvalId: string,
    approve: boolean,
  ): Observable<DiscountApproval>;
  abstract messageDraft(id: string, channel: DeliveryChannel): Observable<string>;
  abstract send(id: string, input: SendQuoteInput): Observable<SendQuoteResult>;
  abstract delivery(id: string): Observable<QuoteDelivery>;
  abstract retryDelivery(id: string): Observable<QuoteDelivery>;
  abstract approve(id: string): Observable<Quote>;
  abstract reject(id: string, reason: string): Observable<Quote>;
  abstract createVersion(id: string): Observable<Quote>;
  abstract versions(id: string): Observable<readonly QuoteVersion[]>;
}

@Injectable({ providedIn: 'root' })
export class HttpQuoteGateway extends QuoteGateway {
  private readonly api = inject(ApiClient);

  list(query: QuoteListQuery): Observable<Page<QuoteSummary>> {
    return this.api
      .send<QuoteListData, QuotePageDto>('GET', '/api/admin/quotes', {
        query: {
          page: query.page,
          pageSize: query.pageSize,
          ...(query.status ? { status: query.status } : {}),
        },
      })
      .pipe(map(toQuotePage));
  }

  openServiceOrders(): Observable<readonly OpenServiceOrder[]> {
    return this.api
      .send<OpenServiceOrdersData, { items?: readonly OpenServiceOrderDto[] }>(
        'GET',
        '/api/admin/quotes/service-orders',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toOpenServiceOrder)));
  }

  create(serviceOrderId: string): Observable<Quote> {
    return this.api
      .send<QuoteCreateData, QuoteDto>('POST', '/api/admin/quotes', { body: { serviceOrderId } })
      .pipe(map(toQuote));
  }

  get(id: string): Observable<Quote> {
    return this.item<QuoteDto>('GET', '/api/admin/quotes/{id}', id).pipe(map(toQuote));
  }

  options(id: string): Observable<QuoteOptions> {
    return this.item<QuoteOptionsDto>('GET', '/api/admin/quotes/{id}/options', id).pipe(
      map(toQuoteOptions),
    );
  }

  saveDraft(id: string, draft: QuoteDraft): Observable<Quote> {
    return this.api
      .send<QuoteDraftData, QuoteDto>('PUT', '/api/admin/quotes/{id}/draft', {
        path: { id },
        body: toQuoteDraftDto(draft),
      })
      .pipe(map(toQuote));
  }

  requestDiscountApproval(id: string, discount: ManualDiscount): Observable<DiscountApproval> {
    return this.api
      .send<DiscountApprovalRequestData, DiscountApprovalDto>(
        'POST',
        '/api/admin/quotes/{id}/discount-approvals',
        {
          path: { id },
          body: { ...toDiscountDto(discount.value), reason: discount.reason.trim() },
        },
      )
      .pipe(map(toApproval));
  }

  decideDiscountApproval(
    id: string,
    approvalId: string,
    approve: boolean,
  ): Observable<DiscountApproval> {
    return this.api
      .send<DiscountApprovalDecisionData, DiscountApprovalDto>(
        'POST',
        '/api/admin/quotes/{id}/discount-approvals/{approvalId}/{action}',
        { path: { id, approvalId, action: approve ? 'approve' : 'reject' } },
      )
      .pipe(map(toApproval));
  }

  messageDraft(id: string, channel: DeliveryChannel): Observable<string> {
    return this.api
      .send<MessageDraftData, { message?: string }>(
        'POST',
        '/api/admin/quotes/{id}/message-draft',
        {
          path: { id },
          body: { channel },
        },
      )
      .pipe(map((dto) => dto.message ?? ''));
  }

  send(id: string, input: SendQuoteInput): Observable<SendQuoteResult> {
    return this.api
      .send<SendQuoteData, SendQuoteResultDto>('POST', '/api/admin/quotes/{id}/send', {
        path: { id },
        body: {
          channel: input.channel,
          expiresInDays: input.expiresInDays,
          message: input.message,
        },
      })
      .pipe(map(toSendResult));
  }

  delivery(id: string): Observable<QuoteDelivery> {
    return this.item<QuoteDeliveryDto>('GET', '/api/admin/quotes/{id}/delivery', id).pipe(
      map(toDelivery),
    );
  }

  retryDelivery(id: string): Observable<QuoteDelivery> {
    return this.item<QuoteDeliveryDto>('POST', '/api/admin/quotes/{id}/delivery/retry', id).pipe(
      map(toDelivery),
    );
  }

  approve(id: string): Observable<Quote> {
    return this.item<QuoteDto>('POST', '/api/admin/quotes/{id}/approve', id).pipe(map(toQuote));
  }

  reject(id: string, reason: string): Observable<Quote> {
    return this.api
      .send<QuoteRejectData, QuoteDto>('POST', '/api/admin/quotes/{id}/reject', {
        path: { id },
        body: { reason: reason.trim() },
      })
      .pipe(map(toQuote));
  }

  createVersion(id: string): Observable<Quote> {
    return this.item<QuoteDto>('POST', '/api/admin/quotes/{id}/versions', id).pipe(map(toQuote));
  }

  versions(id: string): Observable<readonly QuoteVersion[]> {
    return this.item<{ items?: readonly QuoteVersionDto[] }>(
      'GET',
      '/api/admin/quotes/{id}/versions',
      id,
    ).pipe(map((dto) => (dto.items ?? []).map(toQuoteVersion)));
  }

  private item<T>(method: 'GET' | 'POST', url: QuoteItemData['url'], id: string): Observable<T> {
    return this.api.send<QuoteItemData, T>(method, url, { path: { id } });
  }
}

export function provideQuoteData(): Provider {
  return provideGateway(QuoteGateway, 'quotes', { http: HttpQuoteGateway });
}
