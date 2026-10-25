import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { InactivityService } from '../../../core/session/inactivity.service';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { AuthStore } from '../../auth/state/auth.store';
import { Reauthentication, SettingsGateway } from '../data/settings.gateway';
import { ReauthDialog, ReauthDialogData } from '../dialogs/reauth-dialog';

export type SectionLoad = 'loading' | 'ready' | 'pending' | 'error';

export type ChangeOutcome<T> =
  { readonly ok: true; readonly value: T } | { readonly ok: false; readonly error: string };

export function loadState(error: unknown): SectionLoad {
  return isFeaturePending(error) ? 'pending' : 'error';
}

@Injectable()
export class SettingsStore {
  readonly gateway = inject(SettingsGateway);
  private readonly dialogs = inject(UiDialogService);
  private readonly inactivity = inject(InactivityService);

  readonly canManage = inject(AuthStore).can('settings.manage');
  readonly workshopName = signal<string | null>(null);
  readonly planName = signal<string | null>(null);
  readonly pending = signal(false);

  async load<T>(
    source: Observable<T>,
    state: { set(value: SectionLoad): void },
  ): Promise<T | null> {
    state.set('loading');
    try {
      const value = await firstValueFrom(source);
      state.set('ready');
      return value;
    } catch (error) {
      state.set(loadState(error));
      this.pending.set(isFeaturePending(error));
      return null;
    }
  }

  async change<T>(change: string, work: (token: string) => Observable<T>): Promise<T | null> {
    const ref = this.dialogs.open<ReauthDialog<T>, ReauthDialogData<T>, ChangeOutcome<T>>(
      ReauthDialog,
      { change, run: (proof) => this.run(proof, work) },
    );
    const outcome = await firstValueFrom(ref.closed);
    return outcome?.ok ? outcome.value : null;
  }

  async run<T>(
    proof: Reauthentication,
    work: (token: string) => Observable<T>,
  ): Promise<ChangeOutcome<T>> {
    try {
      const token = await firstValueFrom(this.gateway.reauthenticate(proof));
      return { ok: true, value: await firstValueFrom(work(token)) };
    } catch (error) {
      return { ok: false, error: isApiError(error) ? error.detail : messages.errors.unknown };
    }
  }

  applyIdle(minutes: number): void {
    this.inactivity.setIdleMinutes(minutes);
  }
}
