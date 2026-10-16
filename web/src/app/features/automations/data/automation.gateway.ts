import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { ContractError, required } from '../../../core/data/contract-error';
import {
  AutomationRule,
  QueueFailure,
  QueueHealth,
  QueueName,
  readRule,
  RuleDefinition,
} from './automation.models';

export interface AutomationRuleDto {
  readonly id?: string;
  readonly enabled?: boolean;
  readonly definition?: unknown;
  readonly runs24h?: number;
  readonly lastRunAt?: string | null;
}

export interface QueueHealthDto {
  readonly name?: QueueName;
  readonly processed?: number;
  readonly failed?: number;
  readonly pending?: number;
}

export interface QueueFailureDto {
  readonly id?: string;
  readonly queue?: QueueName;
  readonly summary?: string;
  readonly error?: string;
  readonly attempts?: number;
  readonly failedAt?: string;
}

export interface AutomationRulesData {
  readonly url: '/api/admin/automations/rules';
  readonly body?: { readonly definition: RuleDefinition };
}

export interface AutomationRuleData {
  readonly url: '/api/admin/automations/rules/{id}' | '/api/admin/automations/rules/{id}/status';
  readonly path: { readonly id: string };
  readonly body?: { readonly definition: RuleDefinition } | { readonly enabled: boolean };
}

export interface AutomationQueuesData {
  readonly url:
    | '/api/admin/automations/queues'
    | '/api/admin/automations/queues/failures'
    | '/api/admin/automations/queues/failures/{id}/retry';
  readonly path?: { readonly id: string };
}

export abstract class AutomationGateway {
  abstract rules(): Observable<readonly AutomationRule[]>;
  abstract create(definition: RuleDefinition): Observable<AutomationRule>;
  abstract update(id: string, definition: RuleDefinition): Observable<AutomationRule>;
  abstract setEnabled(id: string, enabled: boolean): Observable<AutomationRule>;
  abstract remove(id: string): Observable<void>;
  abstract queues(): Observable<readonly QueueHealth[]>;
  abstract failures(): Observable<readonly QueueFailure[]>;
  abstract retry(id: string): Observable<void>;
}

export function toRule(dto: AutomationRuleDto): AutomationRule {
  const definition = readRule(dto.definition);
  if (!definition) {
    throw new ContractError('definition');
  }

  return {
    id: required(dto.id, 'id'),
    enabled: dto.enabled ?? false,
    definition,
    runs24h: dto.runs24h ?? 0,
    lastRunAt: dto.lastRunAt ?? null,
  };
}

export function toQueue(dto: QueueHealthDto): QueueHealth {
  return {
    name: required(dto.name, 'queues.name'),
    processed: dto.processed ?? 0,
    failed: dto.failed ?? 0,
    pending: dto.pending ?? 0,
  };
}

export function toFailure(dto: QueueFailureDto): QueueFailure {
  return {
    id: required(dto.id, 'failures.id'),
    queue: required(dto.queue, 'failures.queue'),
    summary: required(dto.summary, 'failures.summary'),
    error: dto.error ?? '',
    attempts: dto.attempts ?? 0,
    failedAt: required(dto.failedAt, 'failures.failedAt'),
  };
}

@Injectable({ providedIn: 'root' })
export class HttpAutomationGateway extends AutomationGateway {
  private readonly api = inject(ApiClient);

  rules(): Observable<readonly AutomationRule[]> {
    return this.api
      .send<AutomationRulesData, { items?: readonly AutomationRuleDto[] }>(
        'GET',
        '/api/admin/automations/rules',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toRule)));
  }

  create(definition: RuleDefinition): Observable<AutomationRule> {
    return this.api
      .send<AutomationRulesData, AutomationRuleDto>('POST', '/api/admin/automations/rules', {
        body: { definition },
      })
      .pipe(map(toRule));
  }

  update(id: string, definition: RuleDefinition): Observable<AutomationRule> {
    return this.api
      .send<AutomationRuleData, AutomationRuleDto>('PUT', '/api/admin/automations/rules/{id}', {
        path: { id },
        body: { definition },
      })
      .pipe(map(toRule));
  }

  setEnabled(id: string, enabled: boolean): Observable<AutomationRule> {
    return this.api
      .send<AutomationRuleData, AutomationRuleDto>(
        'PUT',
        '/api/admin/automations/rules/{id}/status',
        { path: { id }, body: { enabled } },
      )
      .pipe(map(toRule));
  }

  remove(id: string): Observable<void> {
    return this.api
      .send<AutomationRuleData, unknown>('DELETE', '/api/admin/automations/rules/{id}', {
        path: { id },
      })
      .pipe(map(() => undefined));
  }

  queues(): Observable<readonly QueueHealth[]> {
    return this.api
      .send<AutomationQueuesData, { items?: readonly QueueHealthDto[] }>(
        'GET',
        '/api/admin/automations/queues',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toQueue)));
  }

  failures(): Observable<readonly QueueFailure[]> {
    return this.api
      .send<AutomationQueuesData, { items?: readonly QueueFailureDto[] }>(
        'GET',
        '/api/admin/automations/queues/failures',
      )
      .pipe(map((dto) => (dto.items ?? []).map(toFailure)));
  }

  retry(id: string): Observable<void> {
    return this.api
      .send<AutomationQueuesData, unknown>(
        'POST',
        '/api/admin/automations/queues/failures/{id}/retry',
        { path: { id } },
      )
      .pipe(map(() => undefined));
  }
}

export function provideAutomationData(): Provider {
  return provideGateway(AutomationGateway, 'automations', { http: HttpAutomationGateway });
}
