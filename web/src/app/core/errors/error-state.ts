import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiError, isApiError, toApiError } from '../http/api-error';

export const ERROR_ROUTE = '/error';
export const UPDATE_ROUTE = '/update';

@Injectable({ providedIn: 'root' })
export class ErrorState {
  private readonly router = inject(Router);

  readonly last = signal<ApiError | null>(null);
  readonly failedUrl = signal<string | null>(null);

  capture(error: unknown, failedUrl: string | null = null): ApiError {
    const apiError = isApiError(error) ? error : toApiError(error, null);
    this.last.set(apiError);
    this.failedUrl.set(failedUrl);
    return apiError;
  }

  show(error: unknown): void {
    this.capture(error, this.router.url);
    void this.router.navigateByUrl(ERROR_ROUTE);
  }
}
