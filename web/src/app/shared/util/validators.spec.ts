import { FormControl } from '@angular/forms';
import { appValidators } from './validators';

describe('appValidators', () => {
  it('ignores empty values', () => {
    expect(appValidators.document(new FormControl(''))).toBeNull();
    expect(appValidators.plate(new FormControl(null))).toBeNull();
  });

  it('accepts valid values', () => {
    expect(appValidators.document(new FormControl('529.982.247-25'))).toBeNull();
    expect(appValidators.plate(new FormControl('BRA2E19'))).toBeNull();
    expect(appValidators.phone(new FormControl('11987654321'))).toBeNull();
    expect(appValidators.email(new FormControl('ana@oficina.local'))).toBeNull();
  });

  it('flags invalid values with their own key', () => {
    expect(appValidators.document(new FormControl('123'))).toEqual({ document: true });
    expect(appValidators.plate(new FormControl('XX'))).toEqual({ plate: true });
    expect(appValidators.phone(new FormControl('123'))).toEqual({ phone: true });
    expect(appValidators.email(new FormControl(42))).toEqual({ email: true });
  });
});
