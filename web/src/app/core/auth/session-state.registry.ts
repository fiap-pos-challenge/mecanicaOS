import { Injectable } from '@angular/core';

export type SessionStateReset = () => void;

@Injectable({ providedIn: 'root' })
export class SessionStateRegistry {
  private readonly resets = new Set<SessionStateReset>();

  register(reset: SessionStateReset): () => void {
    this.resets.add(reset);
    return () => this.resets.delete(reset);
  }

  resetAll(): void {
    for (const reset of this.resets) {
      reset();
    }
  }
}
