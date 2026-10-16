import { TestBed } from '@angular/core/testing';
import { testEnvironment } from '../../../testing/api';
import { APP_ENVIRONMENT } from '../config/app-environment';
import { InactivityService } from './inactivity.service';

describe('InactivityService', () => {
  afterEach(() => vi.useRealTimers());

  function setup(): InactivityService {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: APP_ENVIRONMENT,
          useValue: {
            ...testEnvironment(),
            session: {
              idleWarningMs: 25 * 60_000,
              idleLogoutMs: 30 * 60_000,
              versionCheckIntervalMs: 60_000,
            },
          },
        },
      ],
    });
    return TestBed.inject(InactivityService);
  }

  it('warns at 25 minutes with a countdown and times out at 30', () => {
    const service = setup();
    const timeout = vi.fn();
    service.start(timeout);

    vi.advanceTimersByTime(25 * 60_000 - 1);
    expect(service.warning()).toBe(false);
    vi.advanceTimersByTime(1);
    expect(service.warning()).toBe(true);
    expect(service.remainingSeconds()).toBe(300);
    vi.advanceTimersByTime(60_000);
    expect(service.remainingSeconds()).toBe(240);
    vi.advanceTimersByTime(4 * 60_000);

    expect(timeout).toHaveBeenCalledTimes(1);
    expect(service.warning()).toBe(false);
  });

  it('restarts the clock on activity before the warning, but not during it', () => {
    const service = setup();
    const timeout = vi.fn();
    service.start(timeout);

    vi.advanceTimersByTime(20 * 60_000);
    document.dispatchEvent(new KeyboardEvent('keydown'));
    vi.advanceTimersByTime(20 * 60_000);
    expect(service.warning()).toBe(false);
    vi.advanceTimersByTime(5 * 60_000);
    expect(service.warning()).toBe(true);
    document.dispatchEvent(new Event('pointerdown'));
    expect(service.warning()).toBe(true);

    service.stay();
    expect(service.warning()).toBe(false);
    vi.advanceTimersByTime(29 * 60_000);
    expect(timeout).not.toHaveBeenCalled();
  });

  it('stops listening when stopped', () => {
    const service = setup();
    const timeout = vi.fn();
    service.start(timeout);
    service.stop();

    vi.advanceTimersByTime(60 * 60_000);

    expect(timeout).not.toHaveBeenCalled();
  });

  it('applies the idle time chosen in the security settings', () => {
    const service = setup();
    const timeout = vi.fn();
    service.start(timeout);
    service.setIdleMinutes(10);

    vi.advanceTimersByTime(5 * 60_000);
    expect(service.warning()).toBe(true);
    vi.advanceTimersByTime(5 * 60_000);
    expect(timeout).toHaveBeenCalled();

    service.stop();
    service.setIdleMinutes(6);
    service.start(timeout);
    vi.advanceTimersByTime(3 * 60_000 - 1);
    expect(service.warning()).toBe(false);
    vi.advanceTimersByTime(1);
    expect(service.warning()).toBe(true);
  });
});
