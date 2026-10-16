import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../testing/axe';
import { messages } from '../../core/i18n/messages.pt-BR';
import { UnderConstructionPage } from './under-construction-page';

describe('placeholder pages', () => {
  it('shows the screen name and the under construction state', async () => {
    const { container } = await render(UnderConstructionPage, { inputs: { screen: 'Catálogo' } });

    expect(screen.getByRole('heading', { level: 1, name: 'Catálogo' })).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { level: 2, name: messages.pages.underConstruction.title }),
    ).toBeInTheDocument();
    await expectNoAxeViolations(container);
  });
});
