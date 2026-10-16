import { signal } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiSegmented } from './segmented';

const options = [
  { value: 'today', label: 'Hoje' },
  { value: 'week', label: '7 dias' },
  { value: 'month', label: '30 dias' },
];

async function setup() {
  const value = signal('week');
  const view = await render(
    '<ui-segmented label="Período" [options]="options" [(value)]="value" />',
    {
      imports: [UiSegmented],
      componentProperties: { options, value },
    },
  );
  return { value, view };
}

describe('UiSegmented', () => {
  it('exposes a radio group with the checked option', async () => {
    const { view } = await setup();

    expect(screen.getByRole('radiogroup', { name: 'Período' })).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: '7 dias' })).toHaveAttribute('aria-checked', 'true');
    expect(screen.getByRole('radio', { name: 'Hoje' })).toHaveAttribute('tabindex', '-1');
    await expectNoAxeViolations(view.container);
  });

  it('selects on click', async () => {
    const { value } = await setup();

    await userEvent.click(screen.getByRole('radio', { name: '30 dias' }));

    expect(value()).toBe('month');
  });

  it('moves with the arrow keys and wraps around', async () => {
    const { value } = await setup();
    screen.getByRole('radio', { name: '7 dias' }).focus();

    await userEvent.keyboard('{ArrowRight}');
    expect(value()).toBe('month');
    expect(screen.getByRole('radio', { name: '30 dias' })).toHaveFocus();

    await userEvent.keyboard('{ArrowDown}');
    expect(value()).toBe('today');

    await userEvent.keyboard('{ArrowLeft}');
    expect(value()).toBe('month');

    await userEvent.keyboard('{ArrowUp}');
    expect(value()).toBe('week');

    await userEvent.keyboard('{Enter}');
    expect(value()).toBe('week');
  });
});
