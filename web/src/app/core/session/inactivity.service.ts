import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { APP_ENVIRONMENT } from '../config/app-environment';

export const IDLE_WARNING_LEAD_MS = 5 * 60_000;

export const ACTIVITY_EVENTS = ['pointerdown', 'keydown', 'wheel', 'touchstart', 'scroll'] as const;

@Injectable({ providedIn: 'root' })
export class InactivityService {
  private readonly settings = inject(APP_ENVIRONMENT).session;
  private readonly document = inject(DOCUMENT);
  private warnTimer: ReturnType<typeof setTimeout> | null = null;
  private logoutTimer: ReturnType<typeof setTimeout> | null = null;
  private ticker: ReturnType<typeof setInterval> | null = null;
  private deadline = 0;
  private onTimeout: (() => void) | null = null;
  private logoutMs = this.settings.idleLogoutMs;
  private warningMs = this.settings.idleWarningMs;

  readonly warning = signal(false);
  readonly remainingSeconds = signal(0);

  private readonly activity = (): void => {
    if (!this.warning()) {
      this.schedule();
    }
  };

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stop());
  }

  start(onTimeout: () => void): void {
    this.stop();
    this.onTimeout = onTimeout;
    for (const event of ACTIVITY_EVENTS) {
      this.document.addEventListener(event, this.activity, { passive: true, capture: true });
    }

    this.schedule();
  }

  setIdleMinutes(minutes: number): void {
    this.logoutMs = minutes * 60_000;
    this.warningMs = Math.max(this.logoutMs - IDLE_WARNING_LEAD_MS, this.logoutMs / 2);
    if (this.onTimeout && !this.warning()) {
      this.schedule();
    }
  }

  stay(): void {
    this.schedule();
  }

  stop(): void {
    this.clearTimers();
    this.onTimeout = null;
    for (const event of ACTIVITY_EVENTS) {
      this.document.removeEventListener(event, this.activity, { capture: true });
    }
  }

  private schedule(): void {
    this.clearTimers();
    this.deadline = Date.now() + this.logoutMs;
    this.warnTimer = setTimeout(() => this.warn(), this.warningMs);
    this.logoutTimer = setTimeout(() => this.timeout(), this.logoutMs);
  }

  private warn(): void {
    this.warning.set(true);
    this.tick();
    this.ticker = setInterval(() => this.tick(), 1000);
  }

  private tick(): void {
    this.remainingSeconds.set(Math.max(0, Math.ceil((this.deadline - Date.now()) / 1000)));
  }

  private timeout(): void {
    const onTimeout = this.onTimeout;
    this.stop();
    onTimeout?.();
  }

  private clearTimers(): void {
    for (const timer of [this.warnTimer, this.logoutTimer]) {
      if (timer !== null) {
        clearTimeout(timer);
      }
    }

    if (this.ticker !== null) {
      clearInterval(this.ticker);
    }

    this.warnTimer = null;
    this.logoutTimer = null;
    this.ticker = null;
    this.warning.set(false);
  }
}
