import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiPendingNotice } from './pending-notice';

describe('UiPendingNotice', () => {
  it('explains that the back-end is not ready', async () => {
    const { container } = await render(UiPendingNotice, {
      inputs: { message: 'Estoque em breve.' },
    });

    expect(screen.getByRole('status')).toHaveTextContent(messages.errors.featurePendingTitle);
    expect(screen.getByText('Estoque em breve.')).toBeInTheDocument();
    await expectNoAxeViolations(container);
  });
});
