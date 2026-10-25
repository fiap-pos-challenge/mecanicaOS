import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShieldCheck as ShieldIcon, UserPlus as UserPlusIcon } from 'lucide';
import { BACKEND_ROLES, BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAvatar } from '../../../shared/ui/avatar/avatar';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiSparkline } from '../../../shared/ui/sparkline/sparkline';
import { StatusTone, UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatDateTime } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { CanDirective } from '../../auth/can.directive';
import { AuthStore } from '../../auth/state/auth.store';
import { ProductivityPeriod } from '../data/team-profile.models';
import { User, USER_STATUSES, UserStatus } from '../data/user.models';
import { InviteMemberDialog } from '../dialogs/invite-member-dialog';
import { TeamActions } from '../state/team-actions';
import { producesServices, TeamMember, TeamStore, TeamView } from '../state/team.store';
import {
  AccessBlock,
  accessBlockOf,
  invitationExpired,
  ROLE_TONES,
  STATUS_TONES,
} from './team-display';

type RoleFilter = 'all' | BackendRole;
type PeriodOption = '30' | '90';

@Component({
  selector: 'app-team-page',
  imports: [
    CanDirective,
    NgTemplateOutlet,
    RouterLink,
    UiAvatar,
    UiButton,
    UiCell,
    UiControl,
    UiField,
    UiIcon,
    UiPendingNotice,
    UiSegmented,
    UiSkeleton,
    UiSparkline,
    UiStatusPill,
    UiTable,
  ],
  providers: [TeamStore],
  templateUrl: './team-page.html',
  styleUrl: './team-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamPage {
  protected readonly store = inject(TeamStore);
  private readonly auth = inject(AuthStore);
  private readonly dialogs = inject(UiDialogService);
  private readonly actions = inject(TeamActions);

  protected readonly screen = messages.screens.team;
  protected readonly text = messages.team.list;
  protected readonly statuses = messages.team.statuses;
  protected readonly pending = messages.pending;
  protected readonly roleLabels = messages.roles;
  protected readonly roleTones = ROLE_TONES;
  protected readonly statusTones = STATUS_TONES;
  protected readonly statusOptions = USER_STATUSES;
  protected readonly icons = { invite: UserPlusIcon, permissions: ShieldIcon };
  protected readonly skeletons = [1, 2, 3];

  protected readonly roleOptions: readonly SegmentedOption<RoleFilter>[] = [
    { value: 'all', label: this.text.roleAll },
    ...BACKEND_ROLES.map((role) => ({ value: role, label: messages.roles[role] })),
  ];
  protected readonly periodOptions: readonly SegmentedOption<PeriodOption>[] = [
    { value: '30', label: this.text.period30 },
    { value: '90', label: this.text.period90 },
  ];
  protected readonly viewOptions: readonly SegmentedOption<TeamView>[] = [
    { value: 'cards', label: this.text.viewCards },
    { value: 'list', label: this.text.viewList },
  ];
  protected readonly columns: readonly TableColumn<TeamMember>[] = [
    { key: 'name', header: this.text.columns.name },
    { key: 'email', header: this.text.columns.email },
    { key: 'role', header: this.text.columns.role },
    { key: 'status', header: this.text.columns.status },
    { key: 'production', header: this.text.columns.production },
    { key: 'actions', header: this.text.columns.actions },
  ];

  protected readonly roleFilter = computed<RoleFilter>(() => this.store.filters().role ?? 'all');
  protected readonly period = computed<PeriodOption>(() =>
    this.store.period() === 90 ? '90' : '30',
  );
  protected readonly currentUserId = computed(() => this.auth.user()?.id ?? null);
  protected readonly canManage = computed(() => this.auth.can('team.manage'));
  protected readonly trackMember = (member: TeamMember): string => member.user.id;

  constructor() {
    void this.store.load();
  }

  protected setRole(value: RoleFilter): void {
    void this.store.update({ role: value === 'all' ? null : value });
  }

  protected setStatus(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const status = USER_STATUSES.find((candidate) => candidate === value) ?? null;
    void this.store.update({ status });
  }

  protected setPeriod(value: PeriodOption): void {
    const period: ProductivityPeriod = value === '90' ? 90 : 30;
    void this.store.setPeriod(period);
  }

  protected invite(): void {
    this.dialogs
      .open<InviteMemberDialog, null, User>(InviteMemberDialog)
      .closed.subscribe((user) => {
        if (user) {
          void this.store.load();
        }
      });
  }

  protected async deactivate(user: User): Promise<void> {
    if (await this.actions.deactivate(user)) {
      await this.store.load();
    }
  }

  protected async reactivate(user: User): Promise<void> {
    if (await this.actions.reactivate(user)) {
      await this.store.load();
    }
  }

  protected async resend(user: User): Promise<void> {
    if (await this.actions.resend(user)) {
      await this.store.load();
    }
  }

  protected block(user: User): AccessBlock | null {
    return accessBlockOf(user, this.currentUserId(), this.store.activeAdmins());
  }

  protected blockReason(block: AccessBlock): string {
    return block === 'self' ? this.text.selfReason : this.text.lastAdminReason;
  }

  protected specialties(member: TeamMember): string {
    const items = member.profile?.specialties ?? [];
    return items.length > 0 ? items.join(' · ') : this.text.noSpecialties;
  }

  protected producer(member: TeamMember): boolean {
    return producesServices(member.user.role);
  }

  protected revenue(member: TeamMember): string {
    return member.productivity ? formatBrl(member.productivity.revenue) : '—';
  }

  protected invitation(user: User): string {
    return invitationExpired(user) || !user.invitationExpiresAt
      ? this.text.invitationExpired
      : this.text.invitationUntil(formatDateTime(user.invitationExpiresAt));
  }

  protected roleTone(user: User): StatusTone {
    return ROLE_TONES[user.role];
  }

  protected roleName(user: User): string {
    return messages.roles[user.role];
  }

  protected status(user: User): UserStatus {
    return user.status;
  }
}
