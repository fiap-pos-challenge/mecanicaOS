import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { AddressLookupGateway, HttpAddressLookupGateway } from './address-lookup.gateway';

describe.each(API_MODES)('AddressLookupGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(): Promise<AddressLookupGateway> {
    harness.configure(mode);
    await harness.login();
    return TestBed.inject(AddressLookupGateway);
  }

  it('fills the address of a known zip code through the backend contract', async () => {
    const gateway = await setup();

    expect(gateway).toBeInstanceOf(HttpAddressLookupGateway);
    expect(await firstValueFrom(gateway.lookup('01310-100'))).toEqual({
      zipCode: '01310100',
      street: 'Avenida Paulista',
      number: null,
      complement: null,
      district: 'Bela Vista',
      city: 'São Paulo',
      state: 'SP',
    });
    if (mode === 'http') {
      expect(
        harness.requests.every((request) => request.url.startsWith('http://localhost/api/')),
      ).toBe(true);
    }
  });

  it('answers null for an unknown zip code and fails on invalid input', async () => {
    const gateway = await setup();

    expect(await firstValueFrom(gateway.lookup('99999-999'))).toBeNull();
    await expect(firstValueFrom(gateway.lookup('123'))).rejects.toMatchObject({ status: 400 });
  });
});
