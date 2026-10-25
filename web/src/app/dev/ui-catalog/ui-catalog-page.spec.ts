import { provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { UiToastRegion } from '../../shared/ui/toast/toast';
import { UiCatalogPage } from './ui-catalog-page';
import { uiCatalogTexts } from './ui-catalog-texts';

const text = uiCatalogTexts;

describe('UiCatalogPage', () => {
  async function setup() {
    return render(`<app-ui-catalog-page /><ui-toast-region />`, {
      imports: [UiCatalogPage, UiToastRegion],
      providers: [provideRouter([])],
    });
  }

  it('shows every section of the design system', async () => {
    await setup();

    for (const title of Object.values(text.sections)) {
      expect(screen.getByRole('heading', { level: 2, name: title })).toBeInTheDocument();
    }
  });

  it('sorts, paginates and toggles the table states', async () => {
    await setup();
    const table = screen.getByRole('table', { name: text.table.caption });
    expect(within(table).getAllByRole('row')[1]).toHaveTextContent('3321');

    await userEvent.click(screen.getByRole('button', { name: /Ordenar por Veículo/ }));
    expect(within(table).getAllByRole('row')[1]).toHaveTextContent('Fiat');

    await userEvent.click(screen.getByRole('button', { name: /Ordenar por Total/ }));
    await userEvent.click(screen.getByRole('button', { name: /Ordenar por Total/ }));
    expect(within(table).getAllByRole('row')[1]).toHaveTextContent('Civic');

    await userEvent.click(screen.getByRole('button', { name: /Próxima página/ }));
    expect(screen.getByText(/Página 2 de 2/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: text.table.toggleEmpty }));
    expect(screen.getByText(/Nenhum registro/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: text.table.toggleLoading }));
    expect(table.querySelector('tbody')).toHaveAttribute('aria-busy', 'true');
  });

  it('switches tabs and segments', async () => {
    await setup();

    await userEvent.click(screen.getByRole('tab', { name: 'Peças' }));
    expect(screen.getByText(text.tabContent('Peças'))).toBeInTheDocument();

    await userEvent.click(screen.getByRole('radio', { name: text.segmented.month }));
    expect(screen.getByRole('radio', { name: text.segmented.month })).toHaveAttribute(
      'aria-checked',
      'true',
    );
  });

  it('opens the dialog and shows the toasts', async () => {
    await setup();

    await userEvent.click(screen.getByRole('button', { name: text.dialog.open }));
    const dialog = await screen.findByRole('dialog', { name: text.dialog.title });
    await userEvent.click(within(dialog).getByRole('button', { name: text.dialog.confirm }));

    await userEvent.click(screen.getByRole('button', { name: text.toast.info }));
    await userEvent.click(screen.getByRole('button', { name: text.toast.error }));

    expect(await screen.findByRole('alert')).toHaveTextContent(text.toast.errorMessage);
    expect(screen.getByText(text.toast.infoMessage)).toBeInTheDocument();
  });
});
