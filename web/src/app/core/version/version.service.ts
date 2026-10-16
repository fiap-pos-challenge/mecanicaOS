import { DOCUMENT } from '@angular/common';
import { HttpClient, HttpContext } from '@angular/common/http';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { catchError, interval, map, Observable, of, switchMap } from 'rxjs';
import { APP_ENVIRONMENT } from '../config/app-environment';
import { RETRY_ENABLED, SKIP_GLOBAL_ERROR_HANDLING } from '../http/http-context';

const bundlePattern = /main-[\w-]+\.js/;

export function isChunkLoadError(error: unknown): boolean {
  const message = error instanceof Error ? `${error.name} ${error.message}` : String(error);
  return /ChunkLoadError|Failed to fetch dynamically imported module|Importing a module script failed/i.test(
    message,
  );
}

@Injectable({ providedIn: 'root' })
export class VersionService {
  private readonly http = inject(HttpClient);
  private readonly document = inject(DOCUMENT);
  private readonly intervalMs = inject(APP_ENVIRONMENT).session.versionCheckIntervalMs;
  private readonly destroyRef = inject(DestroyRef);

  readonly outdated = signal(false);

  currentBundle(): string | null {
    const scripts = Array.from(this.document.querySelectorAll('script[src]'));
    for (const script of scripts) {
      const match = bundlePattern.exec(script.getAttribute('src') ?? '');
      if (match) {
        return match[0];
      }
    }

    return null;
  }

  check(): Observable<boolean> {
    const current = this.currentBundle();
    if (current === null) {
      return of(false);
    }

    return this.http
      .get('/index.html', {
        responseType: 'text',
        headers: { 'Cache-Control': 'no-cache' },
        context: new HttpContext().set(SKIP_GLOBAL_ERROR_HANDLING, true).set(RETRY_ENABLED, false),
      })
      .pipe(
        map((html) => {
          const latest = bundlePattern.exec(html)?.[0];
          const changed = latest !== undefined && latest !== current;
          if (changed) {
            this.outdated.set(true);
          }

          return changed;
        }),
        catchError(() => of(false)),
      );
  }

  watch(): void {
    const subscription = interval(this.intervalMs)
      .pipe(switchMap(() => this.check()))
      .subscribe();
    this.destroyRef.onDestroy(() => subscription.unsubscribe());
  }

  markOutdated(): void {
    this.outdated.set(true);
  }

  reload(): void {
    this.document.defaultView?.location.reload();
  }
}
