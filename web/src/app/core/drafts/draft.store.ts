import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { SessionStateRegistry } from '../auth/session-state.registry';

export type DraftOwnership = 'first' | 'same' | 'changed';

@Injectable({ providedIn: 'root' })
export class DraftStore {
  private readonly drafts = new Map<string, unknown>();
  private owner: string | null = null;

  constructor() {
    inject(SessionStateRegistry).register(() => this.clear());
  }

  claim(userId: string): DraftOwnership {
    const ownership: DraftOwnership =
      this.owner === null ? 'first' : this.owner === userId ? 'same' : 'changed';
    if (ownership !== 'same') {
      this.drafts.clear();
    }

    this.owner = userId;
    return ownership;
  }

  save(key: string, value: unknown): void {
    if (this.owner !== null) {
      this.drafts.set(key, structuredClone(value));
    }
  }

  restore<T>(key: string): T | null {
    return this.drafts.has(key) ? structuredClone(this.drafts.get(key) as T) : null;
  }

  discard(key: string): void {
    this.drafts.delete(key);
  }

  clear(): void {
    this.drafts.clear();
    this.owner = null;
  }
}

export function bindDraft(form: AbstractControl, key: string): () => void {
  const drafts = inject(DraftStore);
  const restored = drafts.restore<unknown>(key);
  if (restored !== null) {
    form.patchValue(restored);
    form.markAsDirty();
  }

  form.valueChanges
    .pipe(takeUntilDestroyed(inject(DestroyRef)))
    .subscribe(() => drafts.save(key, form.getRawValue()));
  return () => drafts.discard(key);
}
