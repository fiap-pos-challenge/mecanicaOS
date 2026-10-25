import { Provider } from '@angular/core';
import { provideGateway } from '../../../core/config/provide-gateway';
import { provideAddressLookupData } from './address-lookup.gateway';
import { provideCustomerInsightsData } from './customer-insights.gateway';
import { provideCustomerJobsData } from './customer-jobs.gateway';
import { CustomersGateway, HttpCustomersGateway } from './customers.gateway';
import { provideVehiclesData } from './vehicles.gateway';

export function provideCustomersData(): Provider[] {
  return [
    provideGateway(CustomersGateway, 'customers', { http: HttpCustomersGateway }),
    provideVehiclesData(),
    provideCustomerInsightsData(),
    provideAddressLookupData(),
    provideCustomerJobsData(),
  ];
}
