import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? '') : '';
  return `${first}${last}`.toUpperCase();
}

@Component({
  selector: 'ui-avatar',
  template: '{{ initials() }}',
  styleUrl: './avatar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'img',
    '[attr.aria-label]': 'name()',
  },
})
export class UiAvatar {
  readonly name = input.required<string>();

  protected readonly initials = computed(() => initialsOf(this.name()));
}
