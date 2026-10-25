import {
  ExportFormat,
  ExportStatus,
  ReportDefinition,
  ReportSchedule,
  ReportUnit,
} from './report.models';

export interface ReportRowDto {
  readonly label?: string;
  readonly value?: number;
}

export interface ReportResultDto {
  readonly rows?: readonly ReportRowDto[];
  readonly unit?: ReportUnit;
  readonly total?: number | null;
  readonly from?: string;
  readonly to?: string;
}

export interface SavedReportDto {
  readonly id?: string;
  readonly name?: string;
  readonly definition?: unknown;
  readonly schedule?: ReportSchedule | null;
  readonly updatedAt?: string;
}

export interface ExportJobDto {
  readonly id?: string;
  readonly format?: ExportFormat;
  readonly status?: ExportStatus;
  readonly large?: boolean;
  readonly fileUrl?: string | null;
  readonly fileName?: string | null;
}

export interface ReportRunData {
  readonly url: '/api/admin/reports/run';
  readonly body: { readonly definition: ReportDefinition };
}

export interface SavedReportsData {
  readonly url: '/api/admin/reports/saved';
  readonly body?: { readonly name: string; readonly definition: ReportDefinition };
}

export interface SavedReportData {
  readonly url: '/api/admin/reports/saved/{id}' | '/api/admin/reports/saved/{id}/schedule';
  readonly path: { readonly id: string };
  readonly body?:
    | { readonly name: string; readonly definition: ReportDefinition }
    | { readonly schedule: ReportSchedule | null };
}

export interface ReportExportData {
  readonly url: '/api/admin/reports/exports' | '/api/admin/reports/exports/{id}';
  readonly path?: { readonly id: string };
  readonly body?: { readonly definition: ReportDefinition; readonly format: ExportFormat };
}
