import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiIconButton } from './icon-button';

describe('UiIconButton', () => {
  it('uses the label as accessible name and tooltip', async () => {
    const { container } = await render(
      '<button uiIconButton label="Fechar" [outlined]="true"></button>',
      {
        imports: [UiIconButton],
      },
    );

    const button = screen.getByRole('button', { name: 'Fechar' });
    expect(button).toHaveAttribute('title', 'Fechar');
    expect(button).toHaveAttribute('type', 'button');
    expect(button).toHaveClass('ui-icon-button--outlined');
    await expectNoAxeViolations(container);
  });
});
