import { AbstractControl, FormControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { Cents, formatAmount, parseBrl } from '../../../shared/util/money';
import { isValidQuantity } from '../data/catalog.math';

export function moneyValidator(control: AbstractControl): ValidationErrors | null {
  const value: unknown = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const parsed = typeof value === 'string' ? parseBrl(value) : null;
  return parsed !== null && parsed >= 0 ? null : { money: true };
}

export function numberRange(min: number, max: number, integer = false): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: unknown = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const number = Number(value);
    return Number.isFinite(number) &&
      number >= min &&
      number <= max &&
      (!integer || Number.isInteger(number))
      ? null
      : { range: true };
  };
}

export function quantityValidator(control: AbstractControl): ValidationErrors | null {
  return isValidQuantity(Number(control.value)) ? null : { quantity: true };
}

export function moneyOf(value: string | null | undefined): Cents | null {
  return value ? parseBrl(value) : null;
}

export function amountText(value: Cents | null | undefined): string {
  return value === null || value === undefined ? '' : formatAmount(value);
}

export function formatMoneyControl(control: FormControl<string>): void {
  const parsed = moneyOf(control.value);
  if (parsed !== null && parsed >= 0) {
    control.setValue(formatAmount(parsed));
  }
}

export function optionalText(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}
