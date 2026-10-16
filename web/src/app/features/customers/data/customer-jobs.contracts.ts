export type JobStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed';

export type JobKind = 'CustomerDataExport' | 'CustomerImport';

export interface ImportErrorDto {
  readonly line?: number;
  readonly field?: string | null;
  readonly message?: string | null;
}

export interface JobResultDto {
  readonly fileName?: string | null;
  readonly content?: string | null;
  readonly totalLines?: number | null;
  readonly validLines?: number | null;
  readonly errors?: readonly ImportErrorDto[] | null;
}

export interface JobDto {
  readonly id?: string;
  readonly kind?: JobKind;
  readonly status?: JobStatus;
  readonly createdAt?: string;
  readonly completedAt?: string | null;
  readonly result?: JobResultDto | null;
}

export interface RequestDataExportData {
  readonly url: '/api/admin/customers/{id}/data-export';
  readonly path: { readonly id: string };
}

export interface ImportCustomersData {
  readonly url: '/api/admin/imports/customers';
  readonly body: { readonly fileName: string; readonly csv: string };
}

export interface GetJobData {
  readonly url: '/api/admin/jobs/{id}';
  readonly path: { readonly id: string };
}
