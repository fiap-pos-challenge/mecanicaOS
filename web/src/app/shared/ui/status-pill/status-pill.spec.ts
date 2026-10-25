import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiStatusPill } from './status-pill';

describe('UiStatusPill', () => {
  it('applies the tone', async () => {
    const { container } = await render(
      '<ui-status-pill tone="bad">Atrasada</ui-status-pill><ui-status-pill>Recebida</ui-status-pill>',
      {
        imports: [UiStatusPill],
      },
    );

    expect(screen.getByText('Atrasada')).toHaveClass('ui-status-pill--bad');
    expect(screen.getByText('Recebida')).toHaveClass('ui-status-pill--neutral');
    await expectNoAxeViolations(container);
  });
});
