import { HttpContextToken } from '@angular/common/http';
export const SKIP_GLOBAL_ERROR_HANDLING = new HttpContextToken<boolean>(() => false);
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);
export const RETRY_ENABLED = new HttpContextToken<boolean>(() => true);
export const REQUEST_TIMEOUT_MS = new HttpContextToken<number>(() => 15_000);
export const SESSION_RENEWED = new HttpContextToken<boolean>(() => false);
