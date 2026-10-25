import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { ApiError } from '../http/api-error';
import { applyApiErrors, controlPath, serverErrorOf } from './apply-api-errors';

function apiError(fieldErrors: ApiError['fieldErrors']): ApiError {
  return {
    kind: 'api-error',
    status: 400,
    code: 'common.validationFailed',
    title: 't',
    detail: 'd',
    traceId: null,
    fieldErrors,
    retryAfterSeconds: null,
  };
}

describe('applyApiErrors', () => {
  it('converts indexed field names to control paths', () => {
    expect(controlPath('items[2].quantity')).toBe('items.2.quantity');
  });

  it('puts server messages on matching controls and returns the rest', () => {
    const form = new FormGroup({
      name: new FormControl('x'),
      address: new FormGroup({ zipCode: new FormControl('1') }),
      items: new FormArray([new FormControl(1)]),
    });
    form.controls.name.setErrors({ required: true });

    const unmatched = applyApiErrors(
      form,
      apiError([
        { field: 'name', code: 'a', message: 'Nome inválido.' },
        { field: 'address.zipCode', code: 'b', message: 'CEP inválido.' },
        { field: 'items[0]', code: 'c', message: 'Quantidade inválida.' },
        { field: 'unknown', code: 'd', message: 'Outro.' },
      ]),
    );

    expect(form.controls.name.errors).toEqual({ required: true, server: 'Nome inválido.' });
    expect(form.controls.name.touched).toBe(true);
    expect(serverErrorOf(form.get('address.zipCode'))).toBe('CEP inválido.');
    expect(serverErrorOf(form.get('items.0'))).toBe('Quantidade inválida.');
    expect(unmatched).toEqual([{ field: 'unknown', code: 'd', message: 'Outro.' }]);
  });

  it('reads no server error from clean or missing controls', () => {
    expect(serverErrorOf(new FormControl(''))).toBeNull();
    expect(serverErrorOf(null)).toBeNull();
  });
});
