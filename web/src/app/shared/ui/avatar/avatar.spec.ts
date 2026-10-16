import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { initialsOf, UiAvatar } from './avatar';

describe('UiAvatar', () => {
  it('builds initials from the first and last names', () => {
    expect(initialsOf('Carla Mendes da Silva')).toBe('CS');
    expect(initialsOf('  joão ')).toBe('J');
    expect(initialsOf('')).toBe('');
  });

  it('exposes the name as the image label', async () => {
    const { container } = await render('<ui-avatar name="Carla Mendes" />', {
      imports: [UiAvatar],
    });

    expect(screen.getByRole('img', { name: 'Carla Mendes' })).toHaveTextContent('CM');
    await expectNoAxeViolations(container);
  });
});
