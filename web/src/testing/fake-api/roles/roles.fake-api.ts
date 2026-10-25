import { APP_ROLES, ROLE_PERMISSIONS } from '../../../app/core/auth/permissions';
import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { FakeRouter, ok, problem } from '../fake-api/fake-http';
import { authorize, isPrincipal } from '../fake-api/fake-session';

export const ROLE_PATHS = {
  collection: '/api/admin/roles',
  permissions: '/api/admin/roles/:role/permissions',
} as const;

export function createRolesFakeApi(state: FakeBackendState): FakeFeatureApi {
  const users = (): FakeBackendState['database']['users'] => state.database.users;

  const router = new FakeRouter()
    .on('GET', ROLE_PATHS.collection, (request) => {
      const principal = authorize(request, users(), ['Admin', 'Manager']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return ok({
        customizable: false,
        items: APP_ROLES.map((role) => ({
          role,
          permissions: [...ROLE_PERMISSIONS[role]],
          editable: false,
        })),
      });
    })
    .on('PUT', ROLE_PATHS.permissions, (request) => {
      const principal = authorize(request, users(), ['Admin']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return problem(501, 'roles.customizationUnavailable', messages.fake.rolesNotCustomizable);
    });

  return new FakeFeatureApi('roles', router, state.scenarios);
}
