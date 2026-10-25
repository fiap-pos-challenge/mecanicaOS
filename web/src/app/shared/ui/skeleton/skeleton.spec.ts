import { render } from '@testing-library/angular';
import { UiSkeleton } from './skeleton';

describe('UiSkeleton', () => {
  it('is hidden from assistive technology and sized by its inputs', async () => {
    const { container } = await render('<ui-skeleton width="50%" height="20px" />', {
      imports: [UiSkeleton],
    });
    const skeleton = container.querySelector('ui-skeleton');

    expect(skeleton).toHaveAttribute('aria-hidden', 'true');
    expect(skeleton).toHaveStyle({ width: '50%', height: '20px' });
  });
});
