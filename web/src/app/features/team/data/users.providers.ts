import { Provider } from '@angular/core';
import { provideGateway } from '../../../core/config/provide-gateway';
import { HttpUsersGateway, UsersGateway } from './users.gateway';

export function provideUsersData(): Provider {
  return provideGateway(UsersGateway, 'users', { http: HttpUsersGateway });
}
