import { AbstractControl } from '@angular/forms';
import { ApiError, FieldError } from '../http/api-error';

export const SERVER_ERROR_KEY = 'server';

export function controlPath(field: string): string {
  return field.replace(/\[(\d+)\]/g, '.$1');
}

export function applyApiErrors(form: AbstractControl, error: ApiError): readonly FieldError[] {
  return error.fieldErrors.filter((fieldError) => {
    const control = form.get(controlPath(fieldError.field));
    if (!control) {
      return true;
    }

    control.setErrors({ ...(control.errors ?? {}), [SERVER_ERROR_KEY]: fieldError.message });
    control.markAsTouched();
    return false;
  });
}

export function serverErrorOf(control: AbstractControl | null): string | null {
  const message: unknown = control?.errors?.[SERVER_ERROR_KEY];
  return typeof message === 'string' ? message : null;
}
