import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { onlyDigits } from '../../../app/shared/util/digits';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  fieldError,
  FakeRouter,
  ok,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { authorize, isPrincipal } from '../fake-api/fake-session';

export const ADDRESS_PATHS = { lookup: '/api/addresses/lookup' } as const;

export const FAKE_ZIP_CODES: Readonly<Record<string, Readonly<Record<string, string>>>> = {
  '01310100': {
    street: 'Avenida Paulista',
    district: 'Bela Vista',
    city: 'São Paulo',
    state: 'SP',
  },
  '01001000': { street: 'Praça da Sé', district: 'Sé', city: 'São Paulo', state: 'SP' },
  '20040002': {
    street: 'Rua da Assembleia',
    district: 'Centro',
    city: 'Rio de Janeiro',
    state: 'RJ',
  },
  '30130010': {
    street: 'Avenida Afonso Pena',
    district: 'Centro',
    city: 'Belo Horizonte',
    state: 'MG',
  },
  '80010000': { street: 'Rua XV de Novembro', district: 'Centro', city: 'Curitiba', state: 'PR' },
  '90010150': {
    street: 'Rua dos Andradas',
    district: 'Centro Histórico',
    city: 'Porto Alegre',
    state: 'RS',
  },
};

export function createAddressesFakeApi(state: FakeBackendState): FakeFeatureApi {
  const router = new FakeRouter().on('POST', ADDRESS_PATHS.lookup, (request) => {
    const principal = authorize(request, state.database.users, ['Admin', 'Manager', 'Attendant']);
    if (!isPrincipal(principal)) {
      return principal;
    }

    const zipCode = onlyDigits(readText(readBody(request), 'zipCode'));
    if (zipCode.length !== 8) {
      return validation([
        fieldError('zipCode', 'customer.invalidZipCode', messages.fake.invalidZipCode),
      ]);
    }

    const address = FAKE_ZIP_CODES[zipCode];
    return address
      ? ok({ zipCode, complement: null, number: null, ...address })
      : problem(404, 'address.zipCodeNotFound', messages.fake.zipCodeNotFound);
  });

  return new FakeFeatureApi('addresses', router, state.scenarios);
}
