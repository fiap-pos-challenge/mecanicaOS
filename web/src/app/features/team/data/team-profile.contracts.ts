export type WeekDay =
  'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';

export interface WorkShiftDto {
  readonly day?: WeekDay;
  readonly start?: string;
  readonly end?: string;
}

export interface TeamProfileDto {
  readonly userId?: string;
  readonly specialties?: readonly string[] | null;
  readonly commissionPercent?: number;
  readonly discountLimitPercent?: number;
  readonly schedule?: readonly WorkShiftDto[] | null;
  readonly updatedAt?: string | null;
}

export interface TeamProfileListDto {
  readonly items?: readonly TeamProfileDto[] | null;
}

export interface TeamProfileInputDto {
  readonly specialties: readonly string[];
  readonly commissionPercent: number;
  readonly discountLimitPercent: number;
  readonly schedule: readonly WorkShiftDto[];
}

export interface ProductivityDto {
  readonly userId?: string;
  readonly serviceOrders?: number;
  readonly hours?: number;
  readonly occupancyPercent?: number;
  readonly revenue?: number;
  readonly reworkPercent?: number;
  readonly weekly?: readonly number[] | null;
}

export interface ProductivityListDto {
  readonly periodDays?: number;
  readonly items?: readonly ProductivityDto[] | null;
}

export interface TeamProfilesQueryData {
  readonly url: '/api/admin/team/profiles/query';
  readonly body: { readonly userIds: readonly string[] };
}

export interface TeamProfileData {
  readonly url: '/api/admin/team/{userId}/profile';
  readonly path: { readonly userId: string };
}

export interface UpdateTeamProfileData {
  readonly url: '/api/admin/team/{userId}/profile';
  readonly path: { readonly userId: string };
  readonly body: TeamProfileInputDto;
}

export interface ProductivityQueryData {
  readonly url: '/api/admin/team/productivity';
  readonly body: { readonly userIds: readonly string[]; readonly periodDays: ProductivityPeriod };
}

export type ProductivityPeriod = 30 | 90;
