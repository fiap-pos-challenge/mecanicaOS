import { signal } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiToggle } from './toggle';

describe('UiToggle', () => {
  it('is a switch that flips its checked state', async () => {
    const checked = signal(false);
    const { container } = await render(
      '<button uiToggle [(checked)]="checked">Avisar cliente</button>',
      {
        imports: [UiToggle],
        componentProperties: { checked },
      },
    );

    const toggle = screen.getByRole('switch', { name: 'Avisar cliente' });
    expect(toggle).toHaveAttribute('aria-checked', 'false');

    await userEvent.click(toggle);

    expect(checked()).toBe(true);
    expect(toggle).toHaveAttribute('aria-checked', 'true');
    expect(toggle).toHaveClass('ui-toggle--on');
    await expectNoAxeViolations(container);
  });
});
