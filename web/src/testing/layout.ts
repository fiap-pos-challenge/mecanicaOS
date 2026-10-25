import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Provider } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export class FakeBreakpointObserver {
  readonly state = new BehaviorSubject<BreakpointState>({ matches: false, breakpoints: {} });

  observe(): Observable<BreakpointState> {
    return this.state;
  }

  setMobile(matches: boolean): void {
    this.state.next({ matches, breakpoints: {} });
  }
}

export function provideFakeBreakpoints(mobile: boolean): {
  observer: FakeBreakpointObserver;
  provider: Provider;
} {
  const observer = new FakeBreakpointObserver();
  observer.setMobile(mobile);
  return { observer, provider: { provide: BreakpointObserver, useValue: observer } };
}

export class MemoryStorage implements Storage {
  private readonly values = new Map<string, string>();

  get length(): number {
    return this.values.size;
  }

  clear(): void {
    this.values.clear();
  }

  getItem(key: string): string | null {
    return this.values.get(key) ?? null;
  }

  key(index: number): string | null {
    return [...this.values.keys()][index] ?? null;
  }

  removeItem(key: string): void {
    this.values.delete(key);
  }

  setItem(key: string, value: string): void {
    this.values.set(key, value);
  }

  keys(): string[] {
    return [...this.values.keys()];
  }
}
