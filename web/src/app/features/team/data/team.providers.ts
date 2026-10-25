import { Provider } from '@angular/core';
import { provideRolesData } from './roles.gateway';
import { provideTeamProfileData } from './team-profile.gateway';
import { provideUsersData } from './users.providers';

export function provideTeamData(): Provider[] {
  return [provideUsersData(), provideTeamProfileData(), provideRolesData()];
}
