import { HttpInterceptorFn } from '@angular/common/http';
import { demoApiInterceptor } from './demo.interceptor';

export const DEMO_MODE = true;

export const DEMO_INTERCEPTORS: readonly HttpInterceptorFn[] = [demoApiInterceptor];
