import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiEmptyState } from './empty-state';

describe('UiEmptyState', () => {
  it('shows a level 2 title, the description and the actions', async () => {
    const { container } = await render(
      '<ui-empty-state title="Nada aqui" description="Cadastre o primeiro"><button type="button">Novo</button></ui-empty-state>',
      { imports: [UiEmptyState] },
    );

    expect(screen.getByRole('heading', { level: 2, name: 'Nada aqui' })).toBeInTheDocument();
    expect(screen.getByText('Cadastre o primeiro')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Novo' })).toBeInTheDocument();
    await expectNoAxeViolations(container);
  });

  it('supports a level 3 title without description', async () => {
    const { container } = await render('<ui-empty-state title="Vazio" [headingLevel]="3" />', {
      imports: [UiEmptyState],
    });

    expect(screen.getByRole('heading', { level: 3, name: 'Vazio' })).toBeInTheDocument();
    expect(container.querySelector('.description')).toBeNull();
  });
});
