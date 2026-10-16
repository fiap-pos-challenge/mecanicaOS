import { HttpContext } from '@angular/common/http';
import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { required } from '../../../core/data/contract-error';
import {
  GetJobData,
  ImportCustomersData,
  JobDto,
  JobKind,
  JobStatus,
  RequestDataExportData,
} from './customer-jobs.contracts';

export interface ImportLineError {
  readonly line: number;
  readonly field: string;
  readonly message: string;
}

export interface Job {
  readonly id: string;
  readonly kind: JobKind;
  readonly status: JobStatus;
  readonly fileName: string | null;
  readonly content: string | null;
  readonly totalLines: number | null;
  readonly validLines: number | null;
  readonly errors: readonly ImportLineError[];
}

export function toJob(dto: JobDto): Job {
  return {
    id: required(dto.id, 'id'),
    kind: required(dto.kind, 'kind'),
    status: required(dto.status, 'status'),
    fileName: dto.result?.fileName ?? null,
    content: dto.result?.content ?? null,
    totalLines: dto.result?.totalLines ?? null,
    validLines: dto.result?.validLines ?? null,
    errors: (dto.result?.errors ?? []).map((error) => ({
      line: required(error.line, 'line'),
      field: error.field ?? '',
      message: error.message ?? '',
    })),
  };
}

export function isFinished(job: Job): boolean {
  return job.status === 'Completed' || job.status === 'Failed';
}

export abstract class CustomerJobsGateway {
  abstract requestExport(customerId: string): Observable<Job>;
  abstract importCustomers(fileName: string, csv: string): Observable<Job>;
  abstract job(id: string): Observable<Job>;
}

@Injectable({ providedIn: 'root' })
export class HttpCustomerJobsGateway extends CustomerJobsGateway {
  private readonly api = inject(ApiClient);

  protected context(): HttpContext {
    return new HttpContext();
  }

  requestExport(customerId: string): Observable<Job> {
    return this.api
      .send<RequestDataExportData, JobDto>('POST', '/api/admin/customers/{id}/data-export', {
        path: { id: customerId },
        context: this.context(),
      })
      .pipe(map(toJob));
  }

  importCustomers(fileName: string, csv: string): Observable<Job> {
    return this.api
      .send<ImportCustomersData, JobDto>('POST', '/api/admin/imports/customers', {
        body: { fileName, csv },
        context: this.context(),
      })
      .pipe(map(toJob));
  }

  job(id: string): Observable<Job> {
    return this.api
      .send<GetJobData, JobDto>('GET', '/api/admin/jobs/{id}', {
        path: { id },
        context: this.context(),
      })
      .pipe(map(toJob));
  }
}

export function provideCustomerJobsData(): Provider {
  return provideGateway(CustomerJobsGateway, 'jobs', {
    http: HttpCustomerJobsGateway,
  });
}
