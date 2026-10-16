import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  model,
  viewChildren,
} from '@angular/core';

export interface TabItem {
  readonly id: string;
  readonly label: string;
}

let nextTabsId = 0;

@Component({
  selector: 'ui-tabs',
  templateUrl: './tabs.html',
  styleUrl: './tabs.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiTabs {
  readonly tabs = input.required<readonly TabItem[]>();
  readonly label = input.required<string>();
  readonly selected = model.required<string>();

  protected readonly baseId = `ui-tabs-${++nextTabsId}`;
  private readonly buttons = viewChildren<ElementRef<HTMLButtonElement>>('tab');

  protected tabId(id: string): string {
    return `${this.baseId}-tab-${id}`;
  }

  protected panelId(): string {
    return `${this.baseId}-panel`;
  }

  protected onKeydown(event: KeyboardEvent): void {
    const tabs = this.tabs();
    const current = tabs.findIndex((tab) => tab.id === this.selected());
    const targets: Record<string, number> = {
      ArrowRight: (current + 1) % tabs.length,
      ArrowLeft: (current - 1 + tabs.length) % tabs.length,
      Home: 0,
      End: tabs.length - 1,
    };
    const next = targets[event.key];
    const tab = next === undefined ? undefined : tabs[next];
    if (next === undefined || !tab) {
      return;
    }

    event.preventDefault();
    this.selected.set(tab.id);
    this.buttons()[next]?.nativeElement.focus();
  }
}
