import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { documentKindOf } from './document';
import { isValidEmail } from './email';
import { isValidPhone } from './phone';
import { isValidPlate } from './plate';

function textValidator(key: string, isValid: (value: string) => boolean): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: unknown = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    return typeof value === 'string' && isValid(value) ? null : { [key]: true };
  };
}

export const appValidators = {
  document: textValidator('document', (value) => documentKindOf(value) !== null),
  plate: textValidator('plate', isValidPlate),
  phone: textValidator('phone', isValidPhone),
  email: textValidator('email', isValidEmail),
} as const;
