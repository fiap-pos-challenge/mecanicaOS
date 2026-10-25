import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { sparklineGeometry, UiSparkline } from './sparkline';

describe('UiSparkline', () => {
  it('draws the line up to the last value and describes the values', async () => {
    const { container } = await render(UiSparkline, {
      inputs: { values: [2, 4, 3], label: 'OS por semana' },
    });

    expect(screen.getByRole('img', { name: 'OS por semana: 2, 4, 3' })).toBeInTheDocument();
    expect(container.querySelector('path')?.getAttribute('d')).toBe('M4,24L55,4L106,14');
    expect(container.querySelector('circle')?.getAttribute('cx')).toBe('106');
    await expectNoAxeViolations(container);
  });

  it('handles empty and single values', () => {
    expect(sparklineGeometry([], 110, 28)).toEqual({ path: '', last: null });
    expect(sparklineGeometry([0], 110, 28)).toEqual({ path: 'M4,24', last: { x: 4, y: 24 } });
  });
});
