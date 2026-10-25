import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiControl, UiField } from './field';

const imports = [UiField, UiControl];

describe('UiField', () => {
  it('links the label and the hint to the control', async () => {
    const { container } = await render(
      '<ui-field label="Nome" hint="Como aparece no documento" [required]="true"><input uiControl /></ui-field>',
      { imports },
    );

    const input = screen.getByRole('textbox', { name: /Nome/ });
    expect(input).toHaveAccessibleDescription('Como aparece no documento');
    expect(input).toHaveAttribute('aria-required', 'true');
    expect(input).not.toHaveAttribute('aria-invalid');
    expect(input).toHaveClass('ui-control');
    await expectNoAxeViolations(container);
  });

  it('shows the error instead of the hint and marks the control invalid', async () => {
    const { container } = await render(
      '<ui-field label="CPF" hint="Só números" error="CPF inválido"><input uiControl /></ui-field>',
      { imports },
    );

    const input = screen.getByRole('textbox', { name: 'CPF' });
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAccessibleDescription('CPF inválido');
    expect(screen.queryByText('Só números')).not.toBeInTheDocument();
    await expectNoAxeViolations(container);
  });

  it('works with select and textarea without hint or error', async () => {
    const { container } = await render(
      `<ui-field label="Tipo"><select uiControl><option>A</option></select></ui-field>
       <ui-field label="Observações"><textarea uiControl></textarea></ui-field>`,
      { imports },
    );

    expect(screen.getByRole('combobox', { name: 'Tipo' })).not.toHaveAttribute('aria-describedby');
    expect(screen.getByRole('textbox', { name: 'Observações' })).toBeInTheDocument();
    await expectNoAxeViolations(container);
  });
});
