import { AppRole } from '../../../core/auth/permissions';

export interface RoleDefinitionDto {
  readonly role?: AppRole;
  readonly permissions?: readonly string[] | null;
  readonly editable?: boolean;
}

export interface RoleListDto {
  readonly customizable?: boolean;
  readonly items?: readonly RoleDefinitionDto[] | null;
}

export interface RolesData {
  readonly url: '/api/admin/roles';
}

export interface UpdateRolePermissionsData {
  readonly url: '/api/admin/roles/{role}/permissions';
  readonly path: { readonly role: AppRole };
  readonly body: { readonly permissions: readonly string[] };
}
