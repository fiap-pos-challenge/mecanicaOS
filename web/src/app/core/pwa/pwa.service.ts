import { DOCUMENT } from '@angular/common';
import { inject, Injectable, InjectionToken, isDevMode, signal } from '@angular/core';
import { SessionStateRegistry } from '../auth/session-state.registry';
import { OfflineQueueStorage } from './offline-queue.storage';

export const SERVICE_WORKER_URL = '/sw.js';
export const TRUSTED_TYPES_POLICY = 'oficina#sw';

export const PWA_ENABLED = new InjectionToken<boolean>('PWA_ENABLED', {
  providedIn: 'root',
  factory: () => !isDevMode(),
});

interface ScriptUrlPolicy {
  createScriptURL(url: string): unknown;
}

interface PolicyFactory {
  createPolicy(name: string, rules: { createScriptURL(url: string): string }): ScriptUrlPolicy;
}

interface InstallPrompt extends Event {
  prompt(): Promise<void>;
  readonly userChoice: Promise<{ readonly outcome: 'accepted' | 'dismissed' }>;
}

export function trustedServiceWorkerUrl(factory: PolicyFactory | undefined): string {
  if (!factory) {
    return SERVICE_WORKER_URL;
  }

  const policy = factory.createPolicy(TRUSTED_TYPES_POLICY, {
    createScriptURL: (url) => {
      if (url !== SERVICE_WORKER_URL) {
        throw new TypeError('Unexpected service worker URL.');
      }

      return url;
    },
  });
  return policy.createScriptURL(SERVICE_WORKER_URL) as string;
}

@Injectable({ providedIn: 'root' })
export class PwaService {
  private readonly window = inject(DOCUMENT).defaultView;
  private readonly enabled = inject(PWA_ENABLED);
  private installPrompt: InstallPrompt | null = null;

  readonly canInstall = signal(false);

  constructor() {
    const offline = inject(OfflineQueueStorage);
    inject(SessionStateRegistry).register(() => {
      void this.clearCaches();
      void offline.clear();
    });
  }

  async register(): Promise<void> {
    const window = this.window;
    if (!this.enabled || !window || !('serviceWorker' in window.navigator)) {
      return;
    }

    window.addEventListener('beforeinstallprompt', (event) => {
      event.preventDefault();
      this.installPrompt = event as InstallPrompt;
      this.canInstall.set(true);
    });
    const factory = (window as Window & { trustedTypes?: PolicyFactory }).trustedTypes;
    await window.navigator.serviceWorker
      .register(trustedServiceWorkerUrl(factory), { scope: '/' })
      .catch(() => undefined);
  }

  async install(): Promise<boolean> {
    const prompt = this.installPrompt;
    if (!prompt) {
      return false;
    }

    await prompt.prompt();
    const choice = await prompt.userChoice;
    this.installPrompt = null;
    this.canInstall.set(false);
    return choice.outcome === 'accepted';
  }

  async clearCaches(): Promise<void> {
    const storage = this.window?.caches;
    if (!storage) {
      return;
    }

    const keys = await storage.keys();
    await Promise.all(keys.map((key) => storage.delete(key)));
  }
}
