import { render } from '@testing-library/angular';
import type { IconNode } from 'lucide';
import { UiIcon } from './icon';

const everyShape: IconNode = [
  ['path', { d: 'M1 1h2' }],
  ['circle', { cx: 12, cy: 12, r: 3 }],
  ['ellipse', { cx: 5, cy: 6, rx: 2, ry: 1 }],
  ['rect', { x: 2, y: 3, width: 4, height: 5, rx: 1 }],
  ['line', { x1: 0, y1: 1, x2: 2, y2: 3 }],
  ['polyline', { points: '1 2 3 4' }],
  ['polygon', { points: '5 6 7 8' }],
  ['text', {}],
];

describe('UiIcon', () => {
  it('draws every supported shape, hidden from assistive technology', async () => {
    const { container } = await render(UiIcon, {
      inputs: { icon: everyShape, size: 20, strokeWidth: 1.5 },
    });
    const svg = container.querySelector('svg');

    expect(svg).toHaveAttribute('aria-hidden', 'true');
    expect(svg).toHaveAttribute('width', '20');
    expect(svg).toHaveAttribute('stroke-width', '1.5');
    expect(svg?.querySelector('path')).toHaveAttribute('d', 'M1 1h2');
    expect(svg?.querySelector('circle')).toHaveAttribute('r', '3');
    expect(svg?.querySelector('ellipse')).toHaveAttribute('rx', '2');
    expect(svg?.querySelector('rect')).toHaveAttribute('height', '5');
    expect(svg?.querySelector('rect')).not.toHaveAttribute('ry');
    expect(svg?.querySelector('line')).toHaveAttribute('x2', '2');
    expect(svg?.querySelector('polyline')).toHaveAttribute('points', '1 2 3 4');
    expect(svg?.querySelector('polygon')).toHaveAttribute('points', '5 6 7 8');
    expect(svg?.children).toHaveLength(7);
  });

  it('uses the default size', async () => {
    const { container } = await render(UiIcon, { inputs: { icon: [['path', { d: 'M0 0' }]] } });

    expect(container.querySelector('svg')).toHaveAttribute('width', '18');
  });
});
