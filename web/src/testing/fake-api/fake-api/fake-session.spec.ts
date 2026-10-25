import { FakeRequest } from '../fake-request';
import { createFakeDatabase } from './fake-database';
import { authorize, isPrincipal } from './fake-session';

function jwt(claims: Record<string, unknown>): string {
  const encode = (value: object): string =>
    btoa(JSON.stringify(value)).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '');
  return `${encode({ alg: 'HS256' })}.${encode(claims)}.signature`;
}

function request(token: string): FakeRequest {
  return {
    method: 'GET',
    path: '/api/x',
    query: new URLSearchParams(),
    body: null,
    authorization: `Bearer ${token}`,
  };
}

describe('authorize in the fake API', () => {
  const users = createFakeDatabase().users;
  const future = Math.floor(Date.now() / 1000) + 3600;

  it('accepts the real JWT of the API reading subject and role', () => {
    const principal = authorize(
      request(jwt({ sub: 'u1', role: 'Attendant', exp: future })),
      users,
      ['Attendant'],
    );

    expect(principal).toEqual({ id: 'u1', role: 'Attendant' });
    expect(isPrincipal(principal)).toBe(true);
  });

  it('answers 401 for expired, malformed or unknown tokens and 403 for other roles', () => {
    expect(
      authorize(request(jwt({ sub: 'u1', role: 'Admin', exp: 1 })), users, ['Admin']),
    ).toMatchObject({ status: 401 });
    expect(authorize(request('a.%%%.c'), users, ['Admin'])).toMatchObject({ status: 401 });
    expect(authorize(request('plain'), users, ['Admin'])).toMatchObject({ status: 401 });
    expect(
      authorize(request(jwt({ sub: 'u1', role: 'Mechanic', exp: future })), users, ['Admin']),
    ).toMatchObject({
      status: 403,
    });
  });
});
