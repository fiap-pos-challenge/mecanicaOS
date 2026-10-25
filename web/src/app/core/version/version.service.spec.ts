import { HttpTestingController } from '@angular/common/http/testing';
import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { provideInterceptorTest } from '../../../testing/http';
import { isChunkLoadError, VersionService } from './version.service';

describe('VersionService', () => {
  afterEach(() => {
    document.querySelectorAll('script[data-test-bundle]').forEach((script) => script.remove());
    vi.useRealTimers();
  });

  function setup(bundle: string | null): { service: VersionService; http: HttpTestingController } {
    if (bundle) {
      const script = document.createElement('script');
      script.setAttribute('src', bundle);
      script.setAttribute('data-test-bundle', '');
      document.head.append(script);
    }

    TestBed.configureTestingModule({ providers: provideInterceptorTest() });
    return { service: TestBed.inject(VersionService), http: TestBed.inject(HttpTestingController) };
  }

  it('detects a new bundle published behind the same address', async () => {
    const { service, http } = setup('main-AAAA1111.js');
    const changed = firstValueFrom(service.check());

    http.expectOne('/index.html').flush('<script src="main-BBBB2222.js" type="module"></script>');

    expect(await changed).toBe(true);
    expect(service.outdated()).toBe(true);
    expect(service.currentBundle()).toBe('main-AAAA1111.js');
  });

  it('stays quiet when the bundle is the same, missing or the request fails', async () => {
    const { service, http } = setup('main-AAAA1111.js');

    const same = firstValueFrom(service.check());
    http.expectOne('/index.html').flush('<script src="main-AAAA1111.js"></script>');
    const missing = firstValueFrom(service.check());
    http.expectOne('/index.html').flush('<html></html>');
    const failed = firstValueFrom(service.check());
    http.expectOne('/index.html').flush(null, { status: 503, statusText: 'Down' });

    expect([await same, await missing, await failed]).toEqual([false, false, false]);
    expect(service.outdated()).toBe(false);
  });

  it('skips the check in development builds without hashed bundles', async () => {
    const { service, http } = setup(null);

    expect(await firstValueFrom(service.check())).toBe(false);
    http.expectNone('/index.html');
  });

  it('polls on an interval and reloads on demand', () => {
    vi.useFakeTimers();
    const { service, http } = setup('main-AAAA1111.js');
    const reload = vi.fn();
    Object.defineProperty(TestBed.inject(DOCUMENT), 'defaultView', {
      configurable: true,
      value: { location: { reload } },
    });

    service.watch();
    vi.advanceTimersByTime(60_000);
    http.expectOne('/index.html').flush('<script src="main-CCCC.js"></script>');
    service.reload();
    service.markOutdated();

    expect(service.outdated()).toBe(true);
    expect(reload).toHaveBeenCalled();
    Object.defineProperty(TestBed.inject(DOCUMENT), 'defaultView', {
      configurable: true,
      value: window,
    });
  });

  it('recognizes lazy chunk failures', () => {
    expect(
      isChunkLoadError(new TypeError('Failed to fetch dynamically imported module: x.js')),
    ).toBe(true);
    expect(isChunkLoadError('ChunkLoadError: Loading chunk 3 failed')).toBe(true);
    expect(isChunkLoadError(new Error('boom'))).toBe(false);
  });
});
