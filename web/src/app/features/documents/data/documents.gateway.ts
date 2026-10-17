import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import { SKIP_AUTH, SKIP_GLOBAL_ERROR_HANDLING } from '../../../core/http/http-context';
import { centsFromDecimal } from '../../../shared/util/money';
import { PaymentMethod } from '../../quotes/data/quote.models';
import {
  DocumentChannel,
  DocumentDownload,
  DocumentFilter,
  DocumentKind,
  DocumentModel,
  DocumentSummary,
  DocumentVerification,
} from './document.models';
import { Page } from '../../../core/data/page';

export const PORTAL_SESSION_HEADER = 'X-Portal-Session';

export interface DocumentDto {
  readonly kind?: DocumentKind;
  readonly code?: string | null;
  readonly number?: number;
  readonly version?: number | null;
  readonly issuedAt?: string;
  readonly shopName?: string;
  readonly customerName?: string;
  readonly vehicle?: string;
  readonly lines?: readonly {
    readonly name?: string;
    readonly quantity?: number;
    readonly unitPrice?: number;
    readonly total?: number;
  }[];
  readonly servicesTotal?: number;
  readonly partsTotal?: number;
  readonly discountTotal?: number;
  readonly total?: number;
  readonly paidAt?: string | null;
  readonly paymentMethods?: readonly PaymentMethod[];
}

interface DocumentData {
  readonly url:
    | '/api/admin/documents/{code}'
    | '/api/admin/documents/{code}/pdf'
    | '/api/admin/documents/{code}/send'
    | '/api/public/portal/documents/{code}'
    | '/api/public/portal/documents/{code}/pdf'
    | '/api/public/documents/{code}/verification';
  readonly path: { readonly code: string };
  readonly body?: { readonly channel: DocumentChannel };
}

interface DocumentListData {
  readonly url: '/api/admin/documents';
  readonly query: {
    readonly kind?: DocumentKind;
    readonly code?: string;
    readonly page: number;
    readonly pageSize: number;
  };
}

interface DocumentSummaryDto {
  readonly code?: string;
  readonly kind?: DocumentKind;
  readonly number?: number;
  readonly customerName?: string;
  readonly total?: number;
  readonly issuedAt?: string;
}

export function toDocumentSummary(dto: DocumentSummaryDto): DocumentSummary {
  return {
    code: required(dto.code, 'code'),
    kind: required(dto.kind, 'kind'),
    number: required(dto.number, 'number'),
    customerName: dto.customerName ?? '',
    total: centsFromDecimal(dto.total ?? 0),
    issuedAt: required(dto.issuedAt, 'issuedAt'),
  };
}

export function toDocument(dto: DocumentDto): DocumentModel {
  return {
    kind: required(dto.kind, 'kind'),
    code: dto.code ?? null,
    number: required(dto.number, 'number'),
    version: dto.version ?? null,
    issuedAt: required(dto.issuedAt, 'issuedAt'),
    shopName: dto.shopName ?? '',
    customerName: dto.customerName ?? '',
    vehicle: dto.vehicle ?? '',
    lines: (dto.lines ?? []).map((line) => ({
      name: required(line.name, 'lines.name'),
      quantity: line.quantity ?? 1,
      unitPrice: centsFromDecimal(line.unitPrice ?? 0),
      total: centsFromDecimal(line.total ?? 0),
    })),
    services: centsFromDecimal(dto.servicesTotal ?? 0),
    parts: centsFromDecimal(dto.partsTotal ?? 0),
    discount: centsFromDecimal(dto.discountTotal ?? 0),
    total: centsFromDecimal(dto.total ?? 0),
    paidAt: dto.paidAt ?? null,
    paymentMethods: dto.paymentMethods ?? [],
  };
}

export function publicContext(): HttpContext {
  return new HttpContext().set(SKIP_AUTH, true).set(SKIP_GLOBAL_ERROR_HANDLING, true);
}

export abstract class DocumentsGateway {
  abstract list(filter: DocumentFilter): Observable<Page<DocumentSummary>>;
  abstract get(code: string, portalSession?: string): Observable<DocumentModel>;
  abstract download(code: string, portalSession?: string): Observable<DocumentDownload>;
  abstract send(code: string, channel: DocumentChannel): Observable<void>;
  abstract verify(code: string): Observable<DocumentVerification>;
}

@Injectable({ providedIn: 'root' })
export class HttpDocumentsGateway extends DocumentsGateway {
  private readonly api = inject(ApiClient);

  get(code: string, portalSession?: string): Observable<DocumentModel> {
    return this.api
      .send<DocumentData, DocumentDto>(
        'GET',
        portalSession ? '/api/public/portal/documents/{code}' : '/api/admin/documents/{code}',
        this.options(code, portalSession),
      )
      .pipe(map(toDocument));
  }

  list(filter: DocumentFilter): Observable<Page<DocumentSummary>> {
    return this.api
      .send<
        DocumentListData,
        {
          items?: readonly DocumentSummaryDto[];
          page?: number;
          pageSize?: number;
          totalItems?: number;
        }
      >('GET', '/api/admin/documents', {
        query: {
          ...(filter.kind ? { kind: filter.kind } : {}),
          ...(filter.code.trim() ? { code: filter.code.trim() } : {}),
          page: filter.page,
          pageSize: filter.pageSize,
        },
      })
      .pipe(
        map((dto) => ({
          items: (dto.items ?? []).map(toDocumentSummary),
          page: dto.page ?? filter.page,
          pageSize: dto.pageSize ?? filter.pageSize,
          totalItems: dto.totalItems ?? 0,
        })),
      );
  }

  download(code: string, portalSession?: string): Observable<DocumentDownload> {
    return this.api
      .send<DocumentData, { url?: string; fileName?: string }>(
        'GET',
        portalSession
          ? '/api/public/portal/documents/{code}/pdf'
          : '/api/admin/documents/{code}/pdf',
        this.options(code, portalSession),
      )
      .pipe(
        map((dto) => ({
          url: required(dto.url, 'url'),
          fileName: dto.fileName ?? `${code}.pdf`,
        })),
      );
  }

  send(code: string, channel: DocumentChannel): Observable<void> {
    return this.api
      .send<DocumentData, unknown>('POST', '/api/admin/documents/{code}/send', {
        path: { code },
        body: { channel },
      })
      .pipe(map(() => undefined));
  }

  verify(code: string): Observable<DocumentVerification> {
    return this.api
      .send<
        DocumentData,
        {
          valid?: boolean;
          kind?: DocumentKind | null;
          number?: number | null;
          issuedAt?: string | null;
          total?: number | null;
          shopName?: string | null;
        }
      >('GET', '/api/public/documents/{code}/verification', {
        path: { code },
        context: publicContext(),
      })
      .pipe(
        map((dto) => ({
          valid: dto.valid ?? false,
          kind: dto.kind ?? null,
          number: dto.number ?? null,
          issuedAt: dto.issuedAt ?? null,
          total: dto.total === null || dto.total === undefined ? null : centsFromDecimal(dto.total),
          shopName: dto.shopName ?? null,
        })),
      );
  }

  private options(code: string, portalSession: string | undefined) {
    return portalSession
      ? {
          path: { code },
          headers: { [PORTAL_SESSION_HEADER]: portalSession },
          context: publicContext(),
        }
      : { path: { code } };
  }
}

export function provideDocumentsData(): Provider {
  return provideGateway(DocumentsGateway, 'documents', { http: HttpDocumentsGateway });
}
