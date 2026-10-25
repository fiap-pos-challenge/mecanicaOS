import { TestBed } from '@angular/core/testing';
import { patchState } from '@ngrx/signals';
import { unprotected } from '@ngrx/signals/testing';
import { AuthTokenStore } from '../app/core/auth/auth-token.store';
import { BackendRole } from '../app/core/auth/permissions';
import { CurrentUser } from '../app/features/auth/data/auth.models';
import { AuthStore } from '../app/features/auth/state/auth.store';
import { issueToken } from './fake-api/fake-api/fake-session';
import { FakeBackend } from './fake-api/fake-backend.service';

export const TEST_USERS: Readonly<Record<BackendRole, CurrentUser>> = {
  Admin: { id: 'u-admin', name: 'Ana Admin', email: 'admin@oficina.local', role: 'Admin' },
  Attendant: {
    id: 'u-att',
    name: 'Beto Atendente',
    email: 'beto@oficina.local',
    role: 'Attendant',
  },
  Mechanic: { id: 'u-mec', name: 'Caio Mecânico', email: 'caio@oficina.local', role: 'Mechanic' },
  Manager: { id: 'u-man', name: 'Gabi Gestora', email: 'gabi@oficina.local', role: 'Manager' },
};

export function signInAs(role: BackendRole): CurrentUser {
  const user = TEST_USERS[role];
  patchState(unprotected(TestBed.inject(AuthStore)), { user, status: 'authenticated' });
  return user;
}

const FAKE_EMAILS: Readonly<Record<BackendRole, string>> = {
  Admin: 'admin@oficina.local',
  Attendant: 'ana@oficina.local',
  Mechanic: 'marcos@oficina.local',
  Manager: 'gestor@oficina.local',
};

export function signInWithFakeApi(role: BackendRole): CurrentUser {
  const email = FAKE_EMAILS[role];
  const record = TestBed.inject(FakeBackend).state.database.users.find(
    (candidate) => candidate.email === email,
  );
  if (!record) {
    throw new Error(`Seeded fake user ${email} is missing.`);
  }

  const user: CurrentUser = { id: record.id, name: record.name, email, role };
  TestBed.inject(AuthTokenStore).set(issueToken(record.id));
  patchState(unprotected(TestBed.inject(AuthStore)), { user, status: 'authenticated' });
  return user;
}
