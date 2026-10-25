import { TestBed } from '@angular/core/testing';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { ContractError } from '../../../core/data/contract-error';
import { validateCustomerCsv } from '../../../../testing/fake-api/jobs/jobs.fake-api';
import {
  CustomerJobsGateway,
  HttpCustomerJobsGateway,
  isFinished,
  toJob,
} from './customer-jobs.gateway';
import { CustomersGateway } from './customers.gateway';
import { pollJob } from './job-polling';

const validCsv =
  'nome;documento;telefone;email\nMaria;529.982.247-25;(11) 98765-4321;maria@x.com\n';
const mixedCsv =
  'nome,documento,telefone,email\nA,123,1,bad\nJoão Silva,11222333000181,1133334444,\n';

describe('jobs contract helpers', () => {
  it('maps the job and knows when it is finished', () => {
    const job = toJob({
      id: 'j',
      kind: 'CustomerImport',
      status: 'Completed',
      result: { errors: [{ line: 2 }] },
    });

    expect(job.errors).toEqual([{ line: 2, field: '', message: '' }]);
    expect(isFinished(job)).toBe(true);
    expect(isFinished({ ...job, status: 'Processing' })).toBe(false);
    expect(() => toJob({ id: 'j' })).toThrow(ContractError);
  });

  it('validates every line of the spreadsheet', () => {
    expect(validateCustomerCsv(validCsv)).toEqual({ total: 1, errors: [] });
    expect(
      validateCustomerCsv(mixedCsv).errors.map((error) => `${error.line}:${error.field}`),
    ).toEqual(['2:nome', '2:documento', '2:telefone', '2:email']);
    expect(validateCustomerCsv('a;b\n1;2').errors[0]).toMatchObject({
      line: 1,
      field: 'cabecalho',
    });
  });
});

describe.each(API_MODES)('CustomerJobsGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string): Promise<CustomerJobsGateway> {
    harness.configure(mode);
    await harness.login(email);
    return TestBed.inject(CustomerJobsGateway);
  }

  it('exports the data of the holder through the queue', async () => {
    const gateway = await setup();
    const customer = (
      await firstValueFrom(TestBed.inject(CustomersGateway).list({ page: 1, pageSize: 1 }))
    ).items[0];

    const queued = await firstValueFrom(gateway.requestExport(customer?.id ?? ''));
    const done = await lastValueFrom(pollJob((id) => gateway.job(id), queued.id, 1));

    expect(gateway).toBeInstanceOf(HttpCustomerJobsGateway);
    expect(queued.status).toBe('Queued');
    expect(done).toMatchObject({
      status: 'Completed',
      fileName: expect.stringMatching(/^dados-titular-.*\.json$/),
    });
    expect(JSON.parse(done.content ?? '{}')).toMatchObject({ customerId: customer?.id });
  });

  it('imports a spreadsheet and reports the errors by line', async () => {
    const gateway = await setup();

    const queued = await firstValueFrom(gateway.importCustomers('clientes.csv', mixedCsv));
    const done = await lastValueFrom(pollJob((id) => gateway.job(id), queued.id, 1));

    expect(done).toMatchObject({ status: 'Completed', totalLines: 2, validLines: 1 });
    expect(done.errors).toHaveLength(4);
    const header = await lastValueFrom(
      pollJob(
        (id) => gateway.job(id),
        (await firstValueFrom(gateway.importCustomers('x.csv', 'a;b\n1;2'))).id,
        1,
      ),
    );
    expect(header.validLines).toBe(0);
  });

  it('rejects files that are not CSV, too large or unknown jobs', async () => {
    const gateway = await setup();

    await expect(firstValueFrom(gateway.importCustomers('x.xlsx', validCsv))).rejects.toMatchObject(
      { status: 400 },
    );
    await expect(firstValueFrom(gateway.importCustomers('x.csv', ' '))).rejects.toMatchObject({
      status: 400,
    });
    await expect(
      firstValueFrom(gateway.importCustomers('x.csv', 'a'.repeat(2 * 1024 * 1024 + 1))),
    ).rejects.toMatchObject({ status: 413 });
    await expect(firstValueFrom(gateway.job('missing'))).rejects.toMatchObject({ status: 404 });
  });

  it('keeps the export for administrators', async () => {
    const gateway = await setup('ana@oficina.local');

    await expect(firstValueFrom(gateway.requestExport('any'))).rejects.toMatchObject({
      status: 403,
    });
  });
});
