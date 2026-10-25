import { signal } from '@angular/core';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { TableColumn, TableSort, UiCell, UiTable } from './table';

interface Row {
  readonly id: number;
  readonly name: string;
  readonly total: number;
}

const columns: readonly TableColumn<Row>[] = [
  { key: 'id', header: 'Número', sortable: true, value: (row) => row.id },
  { key: 'name', header: 'Cliente' },
  {
    key: 'total',
    header: 'Total',
    sortable: true,
    align: 'end',
    value: (row) => `R$ ${row.total}`,
  },
  { key: 'empty', header: 'Vazio' },
];

const rows: readonly Row[] = [
  { id: 1, name: 'Ana', total: 10 },
  { id: 2, name: 'Bruno', total: 20 },
];

interface SetupOptions {
  readonly rows?: readonly Row[];
  readonly loading?: boolean;
  readonly page?: number;
  readonly totalItems?: number;
}

async function setup(options: SetupOptions = {}) {
  const sort = signal<TableSort | null>(null);
  const pageChange = vi.fn();
  const view = await render(
    `<ui-table caption="Clientes" [columns]="columns" [rows]="rows" [trackBy]="trackBy" [loading]="loading"
       [(sort)]="sort" [page]="page" [pageSize]="2" [totalItems]="totalItems" (pageChange)="pageChange($event)">
       <ng-template uiCell="name" let-row><strong>{{ row.name }}</strong></ng-template>
     </ui-table>`,
    {
      imports: [UiTable, UiCell],
      componentProperties: {
        columns,
        rows: options.rows ?? rows,
        trackBy: (row: Row) => row.id,
        loading: options.loading ?? false,
        sort,
        page: options.page ?? 1,
        totalItems: options.totalItems ?? 2,
        pageChange,
      },
    },
  );
  return { sort, pageChange, view };
}

describe('UiTable', () => {
  it('renders values and custom cell templates', async () => {
    const { view } = await setup();

    const table = screen.getByRole('table', { name: 'Clientes' });
    expect(within(table).getAllByRole('row')).toHaveLength(3);
    expect(within(table).getByText('Bruno').tagName).toBe('STRONG');
    expect(within(table).getByText('R$ 20')).toHaveClass('end');
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
    await expectNoAxeViolations(view.container);
  });

  it('sorts ascending, then descending, and reports it on the header', async () => {
    const { sort } = await setup();
    const header = screen.getByRole('columnheader', { name: /Número/ });
    const button = screen.getByRole('button', { name: messages.ui.table.sortBy('Número') });

    expect(header).toHaveAttribute('aria-sort', 'none');
    expect(screen.getByRole('columnheader', { name: 'Cliente' })).not.toHaveAttribute('aria-sort');

    await userEvent.click(button);
    expect(sort()).toEqual({ key: 'id', direction: 'asc' });
    expect(header).toHaveAttribute('aria-sort', 'ascending');

    await userEvent.click(button);
    expect(sort()).toEqual({ key: 'id', direction: 'desc' });
    expect(header).toHaveAttribute('aria-sort', 'descending');

    await userEvent.click(screen.getByRole('button', { name: messages.ui.table.sortBy('Total') }));
    expect(sort()).toEqual({ key: 'total', direction: 'asc' });
    expect(header).toHaveAttribute('aria-sort', 'none');
  });

  it('shows the empty message', async () => {
    await setup({ rows: [], totalItems: 0 });

    expect(screen.getByText(messages.ui.table.empty)).toBeInTheDocument();
  });

  it('shows skeleton rows while loading', async () => {
    const { view } = await setup({ loading: true });

    expect(view.container.querySelector('tbody')).toHaveAttribute('aria-busy', 'true');
    expect(view.container.querySelectorAll('ui-skeleton').length).toBeGreaterThan(0);
  });

  it('paginates and disables the edges', async () => {
    const { pageChange, view } = await setup({ page: 2, totalItems: 6 });

    const pager = screen.getByRole('navigation', { name: messages.ui.table.pagination });
    expect(within(pager).getByText(messages.ui.table.pageInfo(2, 3))).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: messages.ui.table.previous }));
    await userEvent.click(screen.getByRole('button', { name: messages.ui.table.next }));
    expect(pageChange.mock.calls).toEqual([[1], [3]]);
    await expectNoAxeViolations(view.container);
  });

  it('disables previous on the first page and next on the last', async () => {
    await setup({ page: 1, totalItems: 4 });

    expect(screen.getByRole('button', { name: messages.ui.table.previous })).toBeDisabled();
    expect(screen.getByRole('button', { name: messages.ui.table.next })).toBeEnabled();
  });
});
