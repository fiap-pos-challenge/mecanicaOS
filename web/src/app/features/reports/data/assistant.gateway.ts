import { inject, Injectable, Provider } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { provideGateway } from '../../../core/config/provide-gateway';
import { ContractError, required } from '../../../core/data/contract-error';
import { readDefinition, ReportDefinition, validateDefinition } from './report.models';

export interface ReportSuggestion {
  readonly definition: ReportDefinition;
  readonly explanation: string;
}

export interface ReportSuggestionDto {
  readonly definition?: unknown;
  readonly explanation?: string;
}

export interface AssistantData {
  readonly url: '/api/admin/assistant/report-definitions';
  readonly body: { readonly question: string };
}

export const QUESTION_MIN = 8;
export const QUESTION_MAX = 300;

export function validQuestion(question: string): boolean {
  const length = question.trim().length;
  return length >= QUESTION_MIN && length <= QUESTION_MAX;
}

export function toSuggestion(dto: ReportSuggestionDto): ReportSuggestion {
  const definition = readDefinition(dto.definition);
  if (!definition || validateDefinition(definition).length > 0) {
    throw new ContractError('definition');
  }

  return { definition, explanation: required(dto.explanation, 'explanation') };
}

export abstract class AssistantGateway {
  abstract suggestReport(question: string): Observable<ReportSuggestion>;
}

@Injectable({ providedIn: 'root' })
export class HttpAssistantGateway extends AssistantGateway {
  private readonly api = inject(ApiClient);

  suggestReport(question: string): Observable<ReportSuggestion> {
    return this.api
      .send<AssistantData, ReportSuggestionDto>('POST', '/api/admin/assistant/report-definitions', {
        body: { question: question.trim() },
      })
      .pipe(map(toSuggestion));
  }
}

export function provideAssistantData(): Provider {
  return provideGateway(AssistantGateway, 'assistant', { http: HttpAssistantGateway });
}
