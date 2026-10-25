import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { Permission } from '../../core/auth/permissions';
import { AuthStore } from './state/auth.store';

@Directive({ selector: '[appCan]' })
export class CanDirective {
  readonly appCan = input.required<Permission>();
  readonly appCanElse = input<TemplateRef<unknown> | null>(null);

  private readonly store = inject(AuthStore);
  private readonly template = inject<TemplateRef<unknown>>(TemplateRef);
  private readonly container = inject(ViewContainerRef);

  constructor() {
    effect(() => {
      const allowed = this.store.can(this.appCan());
      const fallback = this.appCanElse();
      this.container.clear();
      if (allowed) {
        this.container.createEmbeddedView(this.template);
      } else if (fallback) {
        this.container.createEmbeddedView(fallback);
      }
    });
  }
}
