import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { isApiUrl } from '../../api/api-url';
import { AuthTokenStore } from '../../auth/auth-token.store';
import { API_ORIGIN, APP_ENVIRONMENT } from '../../config/app-environment';
import { SKIP_AUTH } from '../http-context';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthTokenStore).token();
  const isApi = isApiUrl(request.url, inject(API_ORIGIN), inject(APP_ENVIRONMENT).apiBaseUrl);
  if (!token || !isApi || request.context.get(SKIP_AUTH)) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
