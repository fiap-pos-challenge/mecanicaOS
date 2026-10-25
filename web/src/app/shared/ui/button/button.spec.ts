import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from './button';

describe('UiButton', () => {
  it('renders the variant and size classes', async () => {
    const { container } = await render(
      '<button uiButton variant="primary" size="sm" type="button">Salvar</button>',
      {
        imports: [UiButton],
      },
    );

    const button = screen.getByRole('button', { name: 'Salvar' });
    expect(button).toHaveClass('ui-button--primary', 'ui-button--sm');
    expect(button).not.toHaveAttribute('aria-busy');
    await expectNoAxeViolations(container);
  });

  it('works on links', async () => {
    await render('<a uiButton href="/x">Abrir</a>', { imports: [UiButton] });

    expect(screen.getByRole('link', { name: 'Abrir' })).toHaveClass('ui-button--secondary');
  });

  it('announces loading and ignores clicks while loading', async () => {
    const clicked = vi.fn();
    const { container } = await render(
      '<button uiButton type="button" [loading]="true" (click)="clicked()">Enviar</button>',
      { imports: [UiButton], componentProperties: { clicked } },
    );

    const button = screen.getByRole('button');
    await userEvent.click(button);

    expect(button).toHaveAttribute('aria-busy', 'true');
    expect(button).toHaveTextContent(messages.ui.loading);
    expect(clicked).not.toHaveBeenCalled();
    await expectNoAxeViolations(container);
  });

  it('lets clicks through when not loading', async () => {
    const clicked = vi.fn();
    await render('<button uiButton type="button" (click)="clicked()">Enviar</button>', {
      imports: [UiButton],
      componentProperties: { clicked },
    });

    await userEvent.click(screen.getByRole('button'));

    expect(clicked).toHaveBeenCalledOnce();
  });
});
