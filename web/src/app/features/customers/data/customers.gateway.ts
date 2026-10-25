import { HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import type {
  DeleteApiAdminCustomersByIdData,
  GetApiAdminCustomersByIdData,
  GetApiAdminCustomersByIdResponse,
  GetApiAdminCustomersData,
  GetApiAdminCustomersResponse,
  PostApiAdminCustomersByIdRevealData,
  PostApiAdminCustomersByIdRevealResponse,
  PostApiAdminCustomersData,
  PostApiAdminCustomersQueryData,
  PostApiAdminCustomersQueryResponse,
  PostApiAdminCustomersResponse,
  PostApiAdminCustomersSearchData,
  PostApiAdminCustomersSearchResponse,
  PutApiAdminCustomersByIdData,
  PutApiAdminCustomersByIdResponse,
} from '../../../core/api/generated/index';
import { Page, PageRequest } from '../../../core/data/page';
import { PersonalDataSearch } from '../../../core/data/search';
import {
  toCreateCustomerRequest,
  toCustomer,
  toCustomerPage,
  toQueryCustomersRequest,
  toUpdateCustomerRequest,
} from './customer.mappers';
import {
  Customer,
  CustomerInput,
  CustomerQuery,
  CustomerUpdate,
  RevealReason,
  SensitiveField,
} from './customer.models';

export abstract class CustomersGateway {
  abstract list(request: PageRequest): Observable<Page<Customer>>;
  abstract query(query: CustomerQuery): Observable<Page<Customer>>;
  abstract reveal(
    id: string,
    field: SensitiveField,
    reason: RevealReason,
  ): Observable<string | null>;
  abstract get(id: string): Observable<Customer>;
  abstract findByDocument(search: PersonalDataSearch<'document'>): Observable<Customer>;
  abstract create(input: CustomerInput): Observable<Customer>;
  abstract update(id: string, input: CustomerUpdate): Observable<Customer>;
  abstract remove(id: string): Observable<void>;
}

@Injectable({ providedIn: 'root' })
export class HttpCustomersGateway extends CustomersGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  list(request: PageRequest): Observable<Page<Customer>> {
    return this.api
      .send<GetApiAdminCustomersData, GetApiAdminCustomersResponse>('GET', '/api/admin/customers', {
        query: request,
        context: this.context(),
      })
      .pipe(map(toCustomerPage));
  }

  query(query: CustomerQuery): Observable<Page<Customer>> {
    return this.api
      .send<PostApiAdminCustomersQueryData, PostApiAdminCustomersQueryResponse>(
        'POST',
        '/api/admin/customers/query',
        { body: toQueryCustomersRequest(query), context: this.context() },
      )
      .pipe(map(toCustomerPage));
  }

  reveal(id: string, field: SensitiveField, reason: RevealReason): Observable<string | null> {
    return this.api
      .send<PostApiAdminCustomersByIdRevealData, PostApiAdminCustomersByIdRevealResponse>(
        'POST',
        '/api/admin/customers/{id}/reveal',
        { path: { id }, body: { field, reason }, context: this.context() },
      )
      .pipe(map((dto) => dto.value ?? null));
  }

  get(id: string): Observable<Customer> {
    return this.api
      .send<GetApiAdminCustomersByIdData, GetApiAdminCustomersByIdResponse>(
        'GET',
        '/api/admin/customers/{id}',
        {
          path: { id },
          context: this.context(),
        },
      )
      .pipe(map(toCustomer));
  }

  findByDocument(search: PersonalDataSearch<'document'>): Observable<Customer> {
    return this.api
      .send<PostApiAdminCustomersSearchData, PostApiAdminCustomersSearchResponse>(
        'POST',
        '/api/admin/customers/search',
        {
          body: { document: search.document },
          context: this.context(),
        },
      )
      .pipe(map(toCustomer));
  }

  create(input: CustomerInput): Observable<Customer> {
    return this.api
      .send<PostApiAdminCustomersData, PostApiAdminCustomersResponse>(
        'POST',
        '/api/admin/customers',
        {
          body: toCreateCustomerRequest(input),
          context: this.context(),
        },
      )
      .pipe(map(toCustomer));
  }

  update(id: string, input: CustomerUpdate): Observable<Customer> {
    return this.api
      .send<PutApiAdminCustomersByIdData, PutApiAdminCustomersByIdResponse>(
        'PUT',
        '/api/admin/customers/{id}',
        {
          path: { id },
          body: toUpdateCustomerRequest(input),
          context: this.context(),
        },
      )
      .pipe(map(toCustomer));
  }

  remove(id: string): Observable<void> {
    return this.api
      .send<DeleteApiAdminCustomersByIdData, unknown>('DELETE', '/api/admin/customers/{id}', {
        path: { id },
        context: this.context(),
      })
      .pipe(map(() => undefined));
  }
}
