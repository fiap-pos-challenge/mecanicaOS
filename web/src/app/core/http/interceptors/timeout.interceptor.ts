import { HttpInterceptorFn } from '@angular/common/http';
import { timeout } from 'rxjs';
import { REQUEST_TIMEOUT_MS } from '../http-context';

export const timeoutInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(timeout(request.context.get(REQUEST_TIMEOUT_MS)));
