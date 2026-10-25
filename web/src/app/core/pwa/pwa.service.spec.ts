import { TestBed } from '@angular/core/testing';
import { SessionStateRegistry } from '../auth/session-state.registry';
import {
  PWA_ENABLED,
  PwaService,
  SERVICE_WORKER_URL,
  TRUSTED_TYPES_POLICY,
  trustedServiceWorkerUrl,
} from './pwa.service';

function setup(enabled: boolean) {
  TestBed.configureTestingModule({ providers: [{ provide: PWA_ENABLED, useValue: enabled }] });
  return TestBed.inject(PwaService);
}

describe('PwaService', () => {
  afterEach(() => {
    Reflect.deleteProperty(navigator, 'serviceWorker');
    Reflect.deleteProperty(window, 'caches');
  });

  it('creates the trusted script URL only for the service worker', () => {
    const createPolicy = vi.fn(
      (_name: string, rules: { createScriptURL(url: string): string }) => ({
        createScriptURL: (url: string) => rules.createScriptURL(url),
      }),
    );
    expect(trustedServiceWorkerUrl(undefined)).toBe(SERVICE_WORKER_URL);
    expect(trustedServiceWorkerUrl({ createPolicy })).toBe(SERVICE_WORKER_URL);
    expect(createPolicy).toHaveBeenCalledWith(TRUSTED_TYPES_POLICY, expect.anything());
    const rules = createPolicy.mock.calls[0]?.[1];
    expect(() => rules?.createScriptURL('/evil.js')).toThrow(TypeError);
  });

  it('registers the service worker and offers installation', async () => {
    const register = vi.fn().mockResolvedValue({});
    Object.defineProperty(navigator, 'serviceWorker', { value: { register }, configurable: true });
    const pwa = setup(true);

    await pwa.register();
    expect(register).toHaveBeenCalledWith(SERVICE_WORKER_URL, { scope: '/' });
    expect(await pwa.install()).toBe(false);

    const prompt = Object.assign(new Event('beforeinstallprompt'), {
      prompt: vi.fn().mockResolvedValue(undefined),
      userChoice: Promise.resolve({ outcome: 'accepted' as const }),
    });
    window.dispatchEvent(prompt);
    expect(pwa.canInstall()).toBe(true);
    expect(await pwa.install()).toBe(true);
    expect(pwa.canInstall()).toBe(false);
  });

  it('skips registration in development and clears caches on logout', async () => {
    const register = vi.fn();
    Object.defineProperty(navigator, 'serviceWorker', { value: { register }, configurable: true });
    const deleted: string[] = [];
    Object.defineProperty(window, 'caches', {
      value: {
        keys: vi.fn().mockResolvedValue(['oficina-shell-v1']),
        delete: vi.fn(async (key: string) => deleted.push(key)),
      },
      configurable: true,
    });
    const pwa = setup(false);

    await pwa.register();
    expect(register).not.toHaveBeenCalled();
    TestBed.inject(SessionStateRegistry).resetAll();
    await vi.waitFor(() => expect(deleted).toEqual(['oficina-shell-v1']));
    Reflect.deleteProperty(window, 'caches');
    await pwa.clearCaches();
  });
});
