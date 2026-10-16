import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiChip } from './chip';

describe('UiChip', () => {
  it('toggles its pressed state', async () => {
    const { container } = await render('<button uiChip>Freios</button>', { imports: [UiChip] });
    const chip = screen.getByRole('button', { name: 'Freios' });

    expect(chip).toHaveAttribute('aria-pressed', 'false');
    await userEvent.click(chip);
    expect(chip).toHaveAttribute('aria-pressed', 'true');
    expect(chip).toHaveClass('ui-chip--selected');
    await expectNoAxeViolations(container);
  });
});
