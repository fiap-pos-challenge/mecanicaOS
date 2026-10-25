import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { AppRole, Permission, PERMISSIONS } from '../../../core/auth/permissions';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import {
  RoleDefinitionDto,
  RoleListDto,
  RolesData,
  UpdateRolePermissionsData,
} from './roles.contracts';

export interface RoleDefinition {
  readonly role: AppRole;
  readonly permissions: ReadonlySet<Permission>;
  readonly editable: boolean;
}

export interface RoleCatalog {
  readonly customizable: boolean;
  readonly roles: readonly RoleDefinition[];
}

function isPermission(value: string): value is Permission {
  return PERMISSIONS.some((permission) => permission === value);
}

export function toRoleDefinition(dto: RoleDefinitionDto): RoleDefinition {
  return {
    role: required(dto.role, 'role'),
    permissions: new Set((dto.permissions ?? []).filter(isPermission)),
    editable: dto.editable ?? false,
  };
}

export abstract class RolesGateway {
  abstract list(): Observable<RoleCatalog>;
  abstract updatePermissions(role: AppRole, permissions: readonly Permission[]): Observable<void>;
}

@Injectable({ providedIn: 'root' })
export class HttpRolesGateway extends RolesGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  list(): Observable<RoleCatalog> {
    return this.api
      .send<RolesData, RoleListDto>('GET', '/api/admin/roles', { context: this.context() })
      .pipe(
        map((dto) => ({
          customizable: dto.customizable ?? false,
          roles: (dto.items ?? []).map(toRoleDefinition),
        })),
      );
  }

  updatePermissions(role: AppRole, permissions: readonly Permission[]): Observable<void> {
    return this.api
      .send<UpdateRolePermissionsData, unknown>('PUT', '/api/admin/roles/{role}/permissions', {
        path: { role },
        body: { permissions },
        context: this.context(),
      })
      .pipe(map(() => undefined));
  }
}

export function provideRolesData(): Provider {
  return provideGateway(RolesGateway, 'roles', { http: HttpRolesGateway });
}
