import { APIRequestContext, expect } from '@playwright/test';

export const E2E_PASSWORD = 'Oficina@2026';

export interface Account {
  readonly email: string;
  readonly password: string;
  readonly name: string;
  readonly role: 'Admin' | 'Attendant' | 'Mechanic';
}

export const ACCOUNTS = {
  admin: {
    email: process.env['DEV_ADMIN_EMAIL'] ?? 'admin@oficina.local',
    password: process.env['DEV_ADMIN_PASSWORD'] ?? '',
    name: 'Administrador',
    role: 'Admin',
  },
  attendant: {
    email: 'atendente.e2e@oficina.local',
    password: E2E_PASSWORD,
    name: 'Atendente E2E',
    role: 'Attendant',
  },
  mechanic: {
    email: 'mecanico.e2e@oficina.local',
    password: E2E_PASSWORD,
    name: 'Mecanico E2E',
    role: 'Mechanic',
  },
  disabled: {
    email: 'desativado.e2e@oficina.local',
    password: E2E_PASSWORD,
    name: 'Desativado E2E',
    role: 'Mechanic',
  },
} as const satisfies Record<string, Account>;

export async function adminToken(request: APIRequestContext): Promise<string> {
  const response = await request.post('/api/auth/login', {
    data: { email: ACCOUNTS.admin.email, password: ACCOUNTS.admin.password },
  });
  expect(response.status()).toBe(200);
  return ((await response.json()) as { accessToken: string }).accessToken;
}

export async function createUser(
  request: APIRequestContext,
  token: string,
  account: Account,
): Promise<string> {
  const response = await request.post('/api/admin/users', {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      name: account.name,
      email: account.email,
      password: account.password,
      role: account.role,
    },
  });
  expect([201, 409]).toContain(response.status());
  if (response.status() === 409) {
    const search = await request.post('/api/admin/users/search', {
      headers: { Authorization: `Bearer ${token}` },
      data: { email: account.email },
    });
    return ((await search.json()) as { id: string }).id;
  }

  return ((await response.json()) as { id: string }).id;
}

export async function deactivateUser(
  request: APIRequestContext,
  token: string,
  id: string,
): Promise<void> {
  const response = await request.post(`/api/admin/users/${id}/deactivate`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  expect([204, 409]).toContain(response.status());
}
