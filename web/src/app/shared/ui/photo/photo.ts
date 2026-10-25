import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { PhotoSource } from './photo-source';

@Component({
  selector: 'ui-photo',
  templateUrl: './photo.html',
  styleUrl: './photo.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[style.aspect-ratio]': 'ratio()' },
})
export class UiPhoto {
  readonly path = input.required<string>();
  readonly alt = input.required<string>();
  readonly width = input.required<number>();
  readonly height = input.required<number>();

  private readonly source = inject(PhotoSource);

  protected readonly ratio = computed(() => `${this.width()} / ${this.height()}`);

  protected readonly image = rxResource({
    params: () => this.path(),
    stream: ({ params }) => (params.startsWith('data:') ? of(params) : this.source.load(params)),
  });
}
