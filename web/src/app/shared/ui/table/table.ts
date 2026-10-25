import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  Directive,
  inject,
  input,
  model,
  output,
  TemplateRef,
} from '@angular/core';
import {
  ArrowUpDown as ArrowUpDownIcon,
  ChevronDown as ChevronDownIcon,
  ChevronUp as ChevronUpIcon,
} from 'lucide';
import { UiIcon } from '../icon/icon';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../button/button';
import { UiSkeleton } from '../skeleton/skeleton';

export interface TableColumn<T> {
  readonly key: string;
  readonly header: string;
  readonly sortable?: boolean;
  readonly align?: 'start' | 'end';
  readonly value?: (row: T) => string | number;
}

export interface TableSort {
  readonly key: string;
  readonly direction: 'asc' | 'desc';
}

export interface CellContext<T> {
  readonly $implicit: T;
}

@Directive({ selector: 'ng-template[uiCell]' })
export class UiCell<T> {
  readonly column = input.required<string>({ alias: 'uiCell' });
  readonly template = inject<TemplateRef<CellContext<T>>>(TemplateRef);
}

@Component({
  selector: 'ui-table',
  imports: [NgTemplateOutlet, UiIcon, UiButton, UiSkeleton],
  templateUrl: './table.html',
  styleUrl: './table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiTable<T> {
  readonly caption = input.required<string>();
  readonly columns = input.required<readonly TableColumn<T>[]>();
  readonly rows = input.required<readonly T[]>();
  readonly trackBy = input.required<(row: T) => string | number>();
  readonly loading = input(false);
  readonly emptyMessage = input<string>(messages.ui.table.empty);
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly totalItems = input(0);
  readonly sort = model<TableSort | null>(null);
  readonly pageChange = output<number>();

  protected readonly text = messages.ui.table;
  protected readonly loadingRows = [0, 1, 2];
  protected readonly icons = {
    sort: ArrowUpDownIcon,
    asc: ChevronUpIcon,
    desc: ChevronDownIcon,
  };

  private readonly cells = contentChildren(UiCell);

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalItems() / this.pageSize())),
  );

  protected cellTemplate(column: string): TemplateRef<CellContext<T>> | null {
    const cell = this.cells().find((candidate) => candidate.column() === column);
    return cell ? (cell.template as TemplateRef<CellContext<T>>) : null;
  }

  protected cellValue(row: T, column: TableColumn<T>): string | number {
    return column.value ? column.value(row) : '';
  }

  protected ariaSort(column: TableColumn<T>): 'ascending' | 'descending' | 'none' | null {
    if (!column.sortable) {
      return null;
    }

    const sort = this.sort();
    if (sort?.key !== column.key) {
      return 'none';
    }

    return sort.direction === 'asc' ? 'ascending' : 'descending';
  }

  protected toggleSort(column: TableColumn<T>): void {
    const sort = this.sort();
    const direction = sort?.key === column.key && sort.direction === 'asc' ? 'desc' : 'asc';
    this.sort.set({ key: column.key, direction });
  }

  protected goTo(page: number): void {
    this.pageChange.emit(page);
  }
}
