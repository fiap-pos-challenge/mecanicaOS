import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiPlate } from './plate';

describe('UiPlate', () => {
  it('normalizes the plate and announces what it is', async () => {
    const { container } = await render('<ui-plate value="fjk-4e21" />', { imports: [UiPlate] });

    expect(container.querySelector('ui-plate')).toHaveTextContent(`${messages.ui.plate} FJK4E21`);
    expect(screen.getByText(messages.ui.plate)).toHaveClass('visually-hidden');
    await expectNoAxeViolations(container);
  });
});
