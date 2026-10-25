import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiCard } from './card';

describe('UiCard', () => {
  it('shows title, subtitle and content', async () => {
    const { container } = await render(
      '<ui-card title="Pátio" subtitle="Por status"><p>Corpo</p></ui-card>',
      {
        imports: [UiCard],
      },
    );

    expect(screen.getByRole('heading', { level: 3, name: 'Pátio' })).toBeInTheDocument();
    expect(screen.getByText('Por status')).toBeInTheDocument();
    expect(screen.getByText('Corpo')).toBeInTheDocument();
    await expectNoAxeViolations(container);
  });

  it('renders only the content when there is no title', async () => {
    await render('<ui-card><p>Só corpo</p></ui-card>', { imports: [UiCard] });

    expect(screen.queryByRole('heading')).not.toBeInTheDocument();
  });
});
