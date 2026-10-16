import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon, Check as CheckIcon, Minus as MinusIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import {
  AppRole,
  moduleOf,
  Permission,
  PermissionModule,
  PERMISSIONS,
} from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { RoleDefinition, RolesGateway } from '../data/roles.gateway';

export interface PermissionGroup {
  readonly module: PermissionModule;
  readonly permissions: readonly Permission[];
}

export const PERMISSION_LABELS: Readonly<Record<Permission, string>> = messages.permissions.labels;
export const MODULE_LABELS: Readonly<Record<PermissionModule, string>> =
  messages.permissions.modules;

export function groupPermissions(permissions: readonly Permission[]): readonly PermissionGroup[] {
  const groups = new Map<PermissionModule, Permission[]>();
  for (const permission of permissions) {
    const module = moduleOf(permission);
    groups.set(module, [...(groups.get(module) ?? []), permission]);
  }

  return [...groups].map(([module, items]) => ({ module, permissions: items }));
}

type LoadState = 'loading' | 'ready' | 'error' | 'pending';

@Component({
  selector: 'app-permissions-page',
  imports: [RouterLink, UiButton, UiIcon, UiPendingNotice, UiSkeleton],
  templateUrl: './permissions-page.html',
  styleUrl: './permissions-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PermissionsPage {
  private readonly gateway = inject(RolesGateway);

  protected readonly text = messages.team.permissions;
  protected readonly roleLabels = messages.roles;
  protected readonly labels = PERMISSION_LABELS;
  protected readonly pending = messages.pending;
  protected readonly modules = MODULE_LABELS;
  protected readonly icons = { back: ArrowLeftIcon, yes: CheckIcon, no: MinusIcon };
  protected readonly groups = groupPermissions(PERMISSIONS);

  protected readonly state = signal<LoadState>('loading');
  protected readonly roles = signal<readonly RoleDefinition[]>([]);
  protected readonly customizable = signal(false);
  protected readonly roleGroups = computed(() =>
    this.roles().map((role) => ({
      role,
      groups: groupPermissions(
        PERMISSIONS.filter((permission) => role.permissions.has(permission)),
      ),
    })),
  );

  constructor() {
    void this.load();
  }

  protected allowed(role: RoleDefinition, permission: Permission): boolean {
    return role.permissions.has(permission);
  }

  protected roleLabel(role: AppRole): string {
    return this.roleLabels[role];
  }

  protected async load(): Promise<void> {
    this.state.set('loading');
    try {
      const catalog = await firstValueFrom(this.gateway.list());
      this.roles.set(catalog.roles);
      this.customizable.set(catalog.customizable);
      this.state.set('ready');
    } catch (error) {
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }
}
