import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { AutomationGateway } from '../data/automation.gateway';
import {
  AutomationRule,
  QueueFailure,
  QueueHealth,
  RuleDefinition,
} from '../data/automation.models';

export type AutomationLoad = 'loading' | 'ready' | 'pending' | 'error';

export type AutomationOutcome =
  { readonly ok: true } | { readonly ok: false; readonly error: string };

function failure(error: unknown): AutomationOutcome {
  return { ok: false, error: isApiError(error) ? error.detail : messages.errors.unknown };
}

@Injectable()
export class AutomationsStore {
  private readonly gateway = inject(AutomationGateway);

  readonly rules = signal<readonly AutomationRule[]>([]);
  readonly queues = signal<readonly QueueHealth[]>([]);
  readonly failures = signal<readonly QueueFailure[]>([]);
  readonly state = signal<AutomationLoad>('loading');
  readonly queueState = signal<AutomationLoad>('loading');
  readonly busy = signal<string | null>(null);

  async load(): Promise<void> {
    await Promise.all([this.loadRules(), this.loadQueues()]);
  }

  async loadRules(): Promise<void> {
    this.state.set('loading');
    try {
      this.rules.set(await firstValueFrom(this.gateway.rules()));
      this.state.set('ready');
    } catch (error) {
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }

  async loadQueues(): Promise<void> {
    this.queueState.set('loading');
    try {
      const [queues, failures] = await Promise.all([
        firstValueFrom(this.gateway.queues()),
        firstValueFrom(this.gateway.failures()),
      ]);
      this.queues.set(queues);
      this.failures.set(failures);
      this.queueState.set('ready');
    } catch (error) {
      this.queueState.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }

  async save(id: string | null, definition: RuleDefinition): Promise<AutomationOutcome> {
    try {
      const rule = await firstValueFrom(
        id ? this.gateway.update(id, definition) : this.gateway.create(definition),
      );
      this.rules.update((rules) =>
        id ? rules.map((item) => (item.id === id ? rule : item)) : [rule, ...rules],
      );
      return { ok: true };
    } catch (error) {
      return failure(error);
    }
  }

  async toggle(rule: AutomationRule): Promise<AutomationOutcome> {
    return this.run(rule.id, async () => {
      const updated = await firstValueFrom(this.gateway.setEnabled(rule.id, !rule.enabled));
      this.rules.update((rules) => rules.map((item) => (item.id === rule.id ? updated : item)));
    });
  }

  async remove(rule: AutomationRule): Promise<AutomationOutcome> {
    return this.run(rule.id, async () => {
      await firstValueFrom(this.gateway.remove(rule.id));
      this.rules.update((rules) => rules.filter((item) => item.id !== rule.id));
    });
  }

  async retry(item: QueueFailure): Promise<AutomationOutcome> {
    const outcome = await this.run(item.id, async () => {
      await firstValueFrom(this.gateway.retry(item.id));
    });
    if (outcome.ok) {
      await this.loadQueues();
    }

    return outcome;
  }

  private async run(id: string, work: () => Promise<void>): Promise<AutomationOutcome> {
    this.busy.set(id);
    try {
      await work();
      return { ok: true };
    } catch (error) {
      return failure(error);
    } finally {
      this.busy.set(null);
    }
  }
}
