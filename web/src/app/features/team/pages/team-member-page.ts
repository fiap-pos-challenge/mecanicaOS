import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon, X as XIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { BACKEND_ROLES, BackendRole } from '../../../core/auth/permissions';
import { applyApiErrors, serverErrorOf } from '../../../core/forms/apply-api-errors';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAvatar } from '../../../shared/ui/avatar/avatar';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiIconButton } from '../../../shared/ui/icon-button/icon-button';
import { UiKpi } from '../../../shared/ui/kpi/kpi';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiSparkline } from '../../../shared/ui/sparkline/sparkline';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDate } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { TeamProfileGateway } from '../data/team-profile.gateway';
import {
  MAX_COMMISSION_PERCENT,
  MAX_DISCOUNT_PERCENT,
  MAX_SPECIALTIES,
  Productivity,
  ProductivityPeriod,
  TeamProfile,
  WEEK_DAYS,
  WeekDay,
  WorkShift,
} from '../data/team-profile.models';
import { User } from '../data/user.models';
import { UsersGateway } from '../data/users.gateway';
import { TeamActions } from '../state/team-actions';
import { producesServices } from '../state/team.store';
import { AccessBlock, accessBlockOf, ROLE_TONES, STATUS_TONES } from './team-display';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error';
type PeriodOption = '30' | '90';

export const DEFAULT_SHIFT = { start: '08:00', end: '17:00' } as const;

export function shiftValidator(control: AbstractControl): ValidationErrors | null {
  const works = control.get('works')?.value === true;
  const start = String(control.get('start')?.value ?? '');
  const end = String(control.get('end')?.value ?? '');
  return !works || (start !== '' && end !== '' && start < end) ? null : { shift: true };
}

export function percentValidator(max: number) {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = Number(control.value);
    return control.value !== null &&
      control.value !== '' &&
      Number.isFinite(value) &&
      value >= 0 &&
      value <= max
      ? null
      : { percent: true };
  };
}

function shiftGroup(day: WeekDay, shift: WorkShift | undefined) {
  return new FormGroup(
    {
      day: new FormControl<WeekDay>(day, { nonNullable: true }),
      works: new FormControl(shift !== undefined, { nonNullable: true }),
      start: new FormControl(shift?.start ?? DEFAULT_SHIFT.start, { nonNullable: true }),
      end: new FormControl(shift?.end ?? DEFAULT_SHIFT.end, { nonNullable: true }),
    },
    { validators: shiftValidator },
  );
}

@Component({
  selector: 'app-team-member-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    UiAvatar,
    UiButton,
    UiCard,
    UiControl,
    UiField,
    UiIcon,
    UiIconButton,
    UiKpi,
    UiPendingNotice,
    UiSegmented,
    UiSkeleton,
    UiSparkline,
    UiStatusPill,
  ],
  templateUrl: './team-member-page.html',
  styleUrl: './team-member-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamMemberPage {
  readonly id = input.required<string>();

  private readonly users = inject(UsersGateway);
  private readonly profiles = inject(TeamProfileGateway);
  private readonly auth = inject(AuthStore);
  private readonly actions = inject(TeamActions);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.team.member;
  protected readonly listText = messages.team.list;
  protected readonly statuses = messages.team.statuses;
  protected readonly roleLabels = messages.roles;
  protected readonly roleTones = ROLE_TONES;
  protected readonly statusTones = STATUS_TONES;
  protected readonly roles = BACKEND_ROLES;
  protected readonly icons = { back: ArrowLeftIcon, remove: XIcon };
  protected readonly maxCommission = MAX_COMMISSION_PERCENT;
  protected readonly maxDiscount = MAX_DISCOUNT_PERCENT;
  protected readonly periodOptions: readonly SegmentedOption<PeriodOption>[] = [
    { value: '30', label: messages.team.list.period30 },
    { value: '90', label: messages.team.list.period90 },
  ];

  protected readonly state = signal<LoadState>('loading');
  protected readonly user = signal<User | null>(null);
  protected readonly profile = signal<TeamProfile | null>(null);
  protected readonly productivity = signal<Productivity | null>(null);
  protected readonly period = signal<ProductivityPeriod>(30);
  protected readonly activeAdmins = signal(0);
  protected readonly savingAccess = signal(false);
  protected readonly savingProfile = signal(false);
  protected readonly accessError = signal<string | null>(null);
  protected readonly profileError = signal<string | null>(null);
  protected readonly specialtyError = signal<string | null>(null);
  protected readonly profilePending = signal(false);
  protected readonly pending = messages.pending;

  protected readonly canManage = computed(() => this.auth.can('team.manage'));
  private readonly roleValue = signal<BackendRole>('Mechanic');
  private request = 0;
  protected readonly isMechanic = computed(() => this.roleValue() === 'Mechanic');
  protected readonly producer = computed(() => {
    const user = this.user();
    return user !== null && producesServices(user.role);
  });
  protected readonly block = computed<AccessBlock | null>(() => {
    const user = this.user();
    return user ? accessBlockOf(user, this.auth.user()?.id ?? null, this.activeAdmins()) : null;
  });
  protected readonly periodOption = computed<PeriodOption>(() =>
    this.period() === 90 ? '90' : '30',
  );

  protected readonly access = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)],
    }),
    role: new FormControl<BackendRole>('Mechanic', { nonNullable: true }),
  });
  protected readonly specialtyInput = new FormControl('', { nonNullable: true });
  protected readonly professional = new FormGroup({
    specialties: new FormControl<readonly string[]>([], { nonNullable: true }),
    commissionPercent: new FormControl<number | null>(0, {
      validators: [percentValidator(MAX_COMMISSION_PERCENT)],
    }),
    discountLimitPercent: new FormControl<number | null>(0, {
      validators: [percentValidator(MAX_DISCOUNT_PERCENT)],
    }),
    schedule: new FormArray(WEEK_DAYS.map((day) => shiftGroup(day, undefined))),
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
    this.access.controls.role.valueChanges.subscribe((role) => {
      this.roleValue.set(role);
      this.syncDiscount(role);
    });
  }

  protected dayLabel(day: WeekDay): string {
    return this.text.days[day];
  }

  protected shortDay(day: WeekDay): string {
    return this.text.dayLabels[day];
  }

  protected since(user: User): string {
    return this.text.since(formatDate(user.createdAt));
  }

  protected revenue(productivity: Productivity): string {
    return formatBrl(productivity.revenue);
  }

  protected blockReason(block: AccessBlock): string {
    return block === 'self' ? this.listText.selfReason : this.listText.lastAdminReason;
  }

  protected accessErrorOf(): string | null {
    const control = this.access.controls.name;
    if (!control.touched || control.valid) {
      return null;
    }

    return (
      serverErrorOf(control) ??
      (control.hasError('required')
        ? messages.team.invite.required
        : messages.team.invite.invalidName)
    );
  }

  protected percentError(name: 'commissionPercent' | 'discountLimitPercent'): string | null {
    const control = this.professional.controls[name];
    if (!control.touched || control.valid) {
      return null;
    }

    return (
      serverErrorOf(control) ??
      this.text.invalidPercent(
        name === 'commissionPercent' ? MAX_COMMISSION_PERCENT : MAX_DISCOUNT_PERCENT,
      )
    );
  }

  protected shiftInvalid(index: number): boolean {
    const group = this.professional.controls.schedule.at(index);
    return group.touched && group.hasError('shift');
  }

  protected addSpecialty(event?: Event): void {
    event?.preventDefault();
    const value = this.specialtyInput.value.trim();
    const current = this.professional.controls.specialties.value;
    if (value === '') {
      return;
    }

    if (value.length < 2 || value.length > 40) {
      this.specialtyError.set(this.text.invalidSpecialty);
      return;
    }

    if (current.some((item) => item.toLowerCase() === value.toLowerCase())) {
      this.specialtyError.set(this.text.duplicatedSpecialty);
      return;
    }

    if (current.length >= MAX_SPECIALTIES) {
      this.specialtyError.set(this.text.tooManySpecialties);
      return;
    }

    this.specialtyError.set(null);
    this.professional.controls.specialties.setValue([...current, value]);
    this.professional.markAsDirty();
    this.specialtyInput.setValue('');
  }

  protected removeSpecialty(specialty: string): void {
    this.professional.controls.specialties.setValue(
      this.professional.controls.specialties.value.filter((item) => item !== specialty),
    );
    this.professional.markAsDirty();
  }

  protected async saveAccess(): Promise<void> {
    const user = this.user();
    if (!user || this.access.invalid) {
      this.access.markAllAsTouched();
      return;
    }

    this.savingAccess.set(true);
    this.accessError.set(null);
    try {
      const value = this.access.getRawValue();
      const updated = await firstValueFrom(
        this.users.update(user.id, { name: value.name.trim(), role: value.role }),
      );
      this.user.set(updated);
      this.access.markAsPristine();
      this.toast.show(this.text.accessSaved, { tone: 'success' });
      await this.loadProductivity(updated);
    } catch (error) {
      if (isApiError(error)) {
        const unmatched = applyApiErrors(this.access, error);
        this.accessError.set(
          error.fieldErrors.length > 0 && unmatched.length === 0 ? null : error.detail,
        );
      } else {
        this.accessError.set(messages.errors.unknown);
      }
    } finally {
      this.savingAccess.set(false);
    }
  }

  protected async saveProfile(): Promise<void> {
    const user = this.user();
    if (!user || this.professional.invalid) {
      this.professional.markAllAsTouched();
      return;
    }

    this.savingProfile.set(true);
    this.profileError.set(null);
    const value = this.professional.getRawValue();
    try {
      const saved = await firstValueFrom(
        this.profiles.update(user.id, {
          specialties: value.specialties,
          commissionPercent: Number(value.commissionPercent),
          discountLimitPercent: Number(value.discountLimitPercent),
          schedule: value.schedule
            .filter((shift) => shift.works)
            .map((shift) => ({ day: shift.day, start: shift.start, end: shift.end })),
        }),
      );
      this.profile.set(saved);
      this.professional.markAsPristine();
      this.toast.show(this.text.profileSaved, { tone: 'success' });
    } catch (error) {
      this.handleProfileError(error);
    } finally {
      this.savingProfile.set(false);
    }
  }

  protected async changePeriod(option: PeriodOption): Promise<void> {
    this.period.set(option === '90' ? 90 : 30);
    const user = this.user();
    if (user) {
      await this.loadProductivity(user);
    }
  }

  protected async deactivate(): Promise<void> {
    const user = this.user();
    if (user && (await this.actions.deactivate(user))) {
      await this.reloadUser(user.id);
    }
  }

  protected async reactivate(): Promise<void> {
    const user = this.user();
    if (user && (await this.actions.reactivate(user))) {
      await this.reloadUser(user.id);
    }
  }

  protected async resend(): Promise<void> {
    const user = this.user();
    if (user && (await this.actions.resend(user))) {
      await this.reloadUser(user.id);
    }
  }

  protected async retry(): Promise<void> {
    await this.load(this.id());
  }

  private handleProfileError(error: unknown): void {
    if (!isApiError(error)) {
      this.profileError.set(messages.errors.unknown);
      return;
    }

    if (error.code === 'team.mechanicDiscount') {
      const control = this.professional.controls.discountLimitPercent;
      control.setErrors({ server: error.detail });
      control.markAsTouched();
      return;
    }

    const unmatched = applyApiErrors(this.professional, error);
    this.profileError.set(
      error.fieldErrors.length > 0 && unmatched.length === 0 ? null : error.detail,
    );
  }

  private syncDiscount(role: BackendRole): void {
    const control = this.professional.controls.discountLimitPercent;
    if (role === 'Mechanic') {
      control.setValue(0);
      control.disable();
    } else if (this.canManage()) {
      control.enable();
    }
  }

  private async reloadUser(id: string): Promise<void> {
    const [user, admins] = await Promise.all([
      firstValueFrom(this.users.get(id)),
      this.countAdmins(),
    ]);
    this.user.set(user);
    this.activeAdmins.set(admins);
  }

  private async countAdmins(): Promise<number> {
    return (
      await firstValueFrom(
        this.users.list({ page: 1, pageSize: 1, role: 'Admin', status: 'Active' }),
      )
    ).totalItems;
  }

  private async loadProductivity(user: User): Promise<void> {
    if (!producesServices(user.role)) {
      this.productivity.set(null);
      return;
    }

    try {
      const report = await firstValueFrom(this.profiles.productivity([user.id], this.period()));
      this.productivity.set(report.items.get(user.id) ?? null);
    } catch (error) {
      this.productivity.set(null);
      this.profilePending.update((pending) => pending || isFeaturePending(error));
    }
  }

  private async load(id: string): Promise<void> {
    const request = ++this.request;
    this.state.set('loading');
    try {
      const [user, profile, admins] = await Promise.all([
        firstValueFrom(this.users.get(id)),
        firstValueFrom(this.profiles.get(id)).catch((error: unknown) => {
          this.profilePending.set(isFeaturePending(error));
          return null;
        }),
        this.countAdmins(),
      ]);
      if (request !== this.request) {
        return;
      }

      this.user.set(user);
      this.profile.set(profile);
      this.activeAdmins.set(admins);
      this.fill(user, profile);
      this.state.set('ready');
      await this.loadProductivity(user);
    } catch (error) {
      if (request === this.request) {
        this.state.set(isApiError(error) && error.status === 404 ? 'notFound' : 'error');
      }
    }
  }

  private fill(user: User, profile: TeamProfile | null): void {
    this.access.reset({ name: user.name, role: user.role });
    this.roleValue.set(user.role);
    const schedule = profile?.schedule ?? [];
    this.professional.reset({
      specialties: profile?.specialties ?? [],
      commissionPercent: profile?.commissionPercent ?? 0,
      discountLimitPercent: profile?.discountLimitPercent ?? 0,
      schedule: WEEK_DAYS.map((day) => {
        const shift = schedule.find((item) => item.day === day);
        return {
          day,
          works: shift !== undefined,
          start: shift?.start ?? DEFAULT_SHIFT.start,
          end: shift?.end ?? DEFAULT_SHIFT.end,
        };
      }),
    });
    if (this.canManage()) {
      this.access.enable();
      this.professional.enable();
      this.specialtyInput.enable();
    } else {
      this.access.disable();
      this.professional.disable();
      this.specialtyInput.disable();
    }

    this.syncDiscount(user.role);
  }
}
