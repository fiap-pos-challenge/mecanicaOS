import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { documentKindOf } from '../../../app/shared/util/document';
import { isValidEmail } from '../../../app/shared/util/email';
import { isValidPhone } from '../../../app/shared/util/phone';
import { FakeImportError, FakeJobRecord } from '../fake-api/fake-database';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  accepted,
  fieldError,
  FakeRouter,
  ok,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, isPrincipal } from '../fake-api/fake-session';

export const JOB_PATHS = {
  export: '/api/admin/customers/:id/data-export',
  import: '/api/admin/imports/customers',
  item: '/api/admin/jobs/:id',
} as const;

export const MAX_IMPORT_BYTES = 2 * 1024 * 1024;
export const IMPORT_HEADER = ['nome', 'documento', 'telefone', 'email'] as const;

const text = messages.fake;

export function toJobResponse(job: FakeJobRecord): Record<string, unknown> {
  return {
    id: job.id,
    kind: job.kind,
    status: job.status,
    createdAt: job.createdAt,
    completedAt: job.completedAt,
    result:
      job.status !== 'Completed'
        ? null
        : job.kind === 'CustomerDataExport'
          ? { fileName: job.fileName, content: job.content }
          : { totalLines: job.totalLines, validLines: job.validLines, errors: job.errors },
  };
}

export function validateCustomerCsv(csv: string): { total: number; errors: FakeImportError[] } {
  const lines = csv
    .replace(/^\uFEFF/, '')
    .split(/\r?\n/)
    .filter((line) => line.trim().length > 0);
  const [header = '', ...rows] = lines;
  const separator = header.includes(';') ? ';' : ',';
  const columns = header.split(separator).map((column) => column.trim().toLowerCase());
  if (IMPORT_HEADER.some((column, index) => columns[index] !== column)) {
    return {
      total: rows.length,
      errors: [{ line: 1, field: 'cabecalho', message: text.importHeader }],
    };
  }

  const errors = rows.flatMap((row, index) => {
    const [name = '', document = '', phone = '', email = ''] = row
      .split(separator)
      .map((cell) => cell.trim());
    const line = index + 2;
    return [
      ...(name.length >= 2 ? [] : [{ line, field: 'nome', message: text.required }]),
      ...(documentKindOf(document)
        ? []
        : [{ line, field: 'documento', message: text.invalidDocument }]),
      ...(isValidPhone(phone) ? [] : [{ line, field: 'telefone', message: text.invalidPhone }]),
      ...(!email || isValidEmail(email)
        ? []
        : [{ line, field: 'email', message: text.invalidEmailField }]),
    ];
  });
  return { total: rows.length, errors };
}

export function createJobsFakeApi(state: FakeBackendState): FakeFeatureApi {
  const jobs = (): FakeJobRecord[] => state.database.jobs;
  const users = (): FakeBackendState['database']['users'] => state.database.users;

  const newJob = (kind: FakeJobRecord['kind']): FakeJobRecord => ({
    id: fakeId(),
    kind,
    status: 'Queued',
    polls: 0,
    createdAt: fakeNow(),
    completedAt: null,
    fileName: null,
    content: null,
    totalLines: null,
    validLines: null,
    errors: [],
  });

  const router = new FakeRouter()
    .on('POST', JOB_PATHS.export, (request, params) => {
      const principal = authorize(request, users(), ['Admin', 'Manager']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const customerId = params['id'] ?? '';
      const job = newJob('CustomerDataExport');
      job.fileName = `dados-titular-${customerId.slice(0, 8)}.json`;
      job.content = JSON.stringify(
        {
          customerId,
          generatedAt: fakeNow(),
          customer: state.database.customers.find((customer) => customer.id === customerId) ?? null,
          vehicles: state.database.vehicles.filter((vehicle) => vehicle.customerId === customerId),
          accessLog: state.database.sensitiveAccess.filter(
            (access) => access.customerId === customerId,
          ),
        },
        null,
        2,
      );
      jobs().push(job);
      return accepted(toJobResponse(job));
    })
    .on('POST', JOB_PATHS.import, (request) => {
      const principal = authorize(request, users(), ['Admin', 'Manager', 'Attendant']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const csv = typeof body['csv'] === 'string' ? body['csv'] : '';
      if (!csv.trim() || !readText(body, 'fileName').toLowerCase().endsWith('.csv')) {
        return validation([fieldError('file', 'import.invalidFile', text.importInvalidFile)]);
      }

      if (csv.length > MAX_IMPORT_BYTES) {
        return problem(413, 'import.fileTooLarge', text.importTooLarge);
      }

      const report = validateCustomerCsv(csv);
      const invalidLines = new Set(report.errors.map((error) => error.line));
      const job = newJob('CustomerImport');
      job.totalLines = report.total;
      job.validLines = report.total - [...invalidLines].filter((line) => line > 1).length;
      job.errors = report.errors;
      if (report.errors.some((error) => error.line === 1)) {
        job.validLines = 0;
      }

      jobs().push(job);
      return accepted(toJobResponse(job));
    })
    .on('GET', JOB_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), ['Admin', 'Manager', 'Attendant']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const job = jobs().find((candidate) => candidate.id === params['id']);
      if (!job) {
        return problem(404, 'job.notFound', text.jobNotFound);
      }

      job.polls += 1;
      if (job.status === 'Queued') {
        job.status = 'Processing';
      } else if (job.status === 'Processing' && job.polls >= 2) {
        job.status = 'Completed';
        job.completedAt = fakeNow();
      }

      return ok(toJobResponse(job));
    });

  return new FakeFeatureApi('jobs', router, state.scenarios);
}
