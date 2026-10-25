import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import type { AddressDto } from '../../../core/api/generated/index';
import { provideGateway } from '../../../core/config/provide-gateway';
import { isApiError } from '../../../core/http/api-error';
import { SKIP_GLOBAL_ERROR_HANDLING } from '../../../core/http/http-context';
import { onlyDigits } from '../../../shared/util/digits';
import { toAddress } from './customer.mappers';
import { Address } from './customer.models';

export interface AddressLookupData {
  readonly url: '/api/addresses/lookup';
  readonly body: { readonly zipCode: string };
}

export abstract class AddressLookupGateway {
  abstract lookup(zipCode: string): Observable<Address | null>;
}

@Injectable({ providedIn: 'root' })
export class HttpAddressLookupGateway extends AddressLookupGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext().set(SKIP_GLOBAL_ERROR_HANDLING, true);
  }

  lookup(zipCode: string): Observable<Address | null> {
    return this.api
      .send<AddressLookupData, AddressDto>('POST', '/api/addresses/lookup', {
        body: { zipCode: onlyDigits(zipCode) },
        context: this.context(),
      })
      .pipe(
        map((dto) => toAddress(dto)),
        catchError((error: unknown) =>
          isApiError(error) && error.status === 404 ? of(null) : throwError(() => error),
        ),
      );
  }
}

export function provideAddressLookupData(): Provider {
  return provideGateway(AddressLookupGateway, 'addresses', {
    http: HttpAddressLookupGateway,
  });
}
