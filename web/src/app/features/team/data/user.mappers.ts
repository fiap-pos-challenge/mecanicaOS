import type { UserDto, UserDtoPagedResult } from '../../../core/api/generated/index';
import { required } from '../../../core/data/contract-error';
import { Page } from '../../../core/data/page';
import { User, UserStatus } from './user.models';

function statusOf(dto: UserDto): UserStatus {
  return dto.status ?? (dto.isActive === false ? 'Inactive' : 'Active');
}

export function toUser(dto: UserDto): User {
  return {
    id: required(dto.id, 'id'),
    name: required(dto.name, 'name'),
    email: required(dto.email, 'email'),
    role: required(dto.role, 'role'),
    isActive: required(dto.isActive, 'isActive'),
    status: statusOf(dto),
    invitationExpiresAt: dto.invitationExpiresAt ?? null,
    createdAt: required(dto.createdAt, 'createdAt'),
    updatedAt: dto.updatedAt ?? null,
  };
}

export function toUserPage(dto: UserDtoPagedResult): Page<User> {
  return {
    items: (dto.items ?? []).map(toUser),
    page: required(dto.page, 'page'),
    pageSize: required(dto.pageSize, 'pageSize'),
    totalItems: required(dto.totalItems, 'totalItems'),
  };
}
