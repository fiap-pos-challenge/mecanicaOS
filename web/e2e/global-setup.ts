import { request } from '@playwright/test';
import { ACCOUNTS, adminToken, createUser, deactivateUser } from './accounts';

export default async function globalSetup(): Promise<void> {
  const api = await request.newContext({ baseURL: 'http://localhost:4300' });
  const token = await adminToken(api);
  await createUser(api, token, ACCOUNTS.attendant);
  await createUser(api, token, ACCOUNTS.mechanic);
  await deactivateUser(api, token, await createUser(api, token, ACCOUNTS.disabled));
  await api.dispose();
}
