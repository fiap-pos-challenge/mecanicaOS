import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { ContractError, required } from '../../../core/data/contract-error';
import {
  ExportJobDto,
  ReportExportData,
  ReportResultDto,
  ReportRunData,
  SavedReportData,
  SavedReportDto,
  SavedReportsData,
} from './report.contracts';
import {
  ExportFormat,
  ExportJob,
  readDefinition,
  ReportDefinition,
  ReportResult,
  ReportSchedule,
  SavedReport,
} from './report.models';

export abstract class ReportGateway {
  abstract run(definition: ReportDefinition): Observable<ReportResult>;
  abstract saved(): Observable<readonly SavedReport[]>;
  abstract save(name: string, definition: ReportDefinition): Observable<SavedReport>;
  abstract update(id: string, name: string, definition: ReportDefinition): Observable<SavedReport>;
  abstract remove(id: string): Observable<void>;
  abstract schedule(id: string, schedule: ReportSchedule | null): Observable<SavedReport>;
  abstract export(definition: ReportDefinition, format: ExportFormat): Observable<ExportJob>;
  abstract exportStatus(id: string): Observable<ExportJob>;
}

export function toResult(dto: ReportResultDto): ReportResult {
  return {
    rows: (dto.rows ?? []).map((row) => ({
      label: required(row.label, 'rows.label'),
      value: row.value ?? 0,
    })),
    unit: dto.unit ?? 'count',
    total: dto.total ?? null,
    from: required(dto.from, 'from'),
    to: required(dto.to, 'to'),
  };
}

export function toSavedReport(dto: SavedReportDto): SavedReport {
  const definition = readDefinition(dto.definition);
  if (!definition) {
    throw new ContractError('definition');
  }

  return {
    id: required(dto.id, 'id'),
    name: required(dto.name, 'name'),
    definition,
    schedule: dto.schedule ?? null,
    updatedAt: required(dto.updatedAt, 'updatedAt'),
  };
}

export function toExportJob(dto: ExportJobDto): ExportJob {
  return {
    id: required(dto.id, 'id'),
    format: required(dto.format, 'format'),
    status: required(dto.status, 'status'),
    large: dto.large ?? false,
    fileUrl: dto.fileUrl ?? null,
    fileName: dto.fileName ?? null,
  };
}

@Injectable({ providedIn: 'root' })
export class HttpReportGateway extends ReportGateway {
  private readonly api = inject(ApiClient);

  run(definition: ReportDefinition): Observable<ReportResult> {
    return this.api
      .send<ReportRunData, ReportResultDto>('POST', '/api/admin/reports/run', {
        body: { definition },
      })
      .pipe(map(toResult));
  }

  saved(): Observable<readonly SavedReport[]> {
    return this.api
      .send<SavedReportsData, { items?: readonly SavedReportDto[] }>(
        'GET',
        '/api/admin/reports/saved',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toSavedReport)));
  }

  save(name: string, definition: ReportDefinition): Observable<SavedReport> {
    return this.api
      .send<SavedReportsData, SavedReportDto>('POST', '/api/admin/reports/saved', {
        body: { name: name.trim(), definition },
      })
      .pipe(map(toSavedReport));
  }

  update(id: string, name: string, definition: ReportDefinition): Observable<SavedReport> {
    return this.api
      .send<SavedReportData, SavedReportDto>('PUT', '/api/admin/reports/saved/{id}', {
        path: { id },
        body: { name: name.trim(), definition },
      })
      .pipe(map(toSavedReport));
  }

  remove(id: string): Observable<void> {
    return this.api
      .send<SavedReportData, unknown>('DELETE', '/api/admin/reports/saved/{id}', { path: { id } })
      .pipe(map(() => undefined));
  }

  schedule(id: string, schedule: ReportSchedule | null): Observable<SavedReport> {
    return this.api
      .send<SavedReportData, SavedReportDto>('PUT', '/api/admin/reports/saved/{id}/schedule', {
        path: { id },
        body: { schedule },
      })
      .pipe(map(toSavedReport));
  }

  export(definition: ReportDefinition, format: ExportFormat): Observable<ExportJob> {
    return this.api
      .send<ReportExportData, ExportJobDto>('POST', '/api/admin/reports/exports', {
        body: { definition, format },
      })
      .pipe(map(toExportJob));
  }

  exportStatus(id: string): Observable<ExportJob> {
    return this.api
      .send<ReportExportData, ExportJobDto>('GET', '/api/admin/reports/exports/{id}', {
        path: { id },
      })
      .pipe(map(toExportJob));
  }
}

export function provideReportData(): Provider {
  return provideGateway(ReportGateway, 'reports', { http: HttpReportGateway });
}
