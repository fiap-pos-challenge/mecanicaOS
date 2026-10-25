import { render, screen } from '@testing-library/angular';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiKpi } from './kpi';

describe('UiKpi', () => {
  it('shows label, value and an upward delta', async () => {
    const { container } = await render(
      '<ui-kpi label="Faturamento" value="R$ 10" delta="12%" trend="up" />',
      {
        imports: [UiKpi],
      },
    );

    expect(screen.getByText('Faturamento')).toBeInTheDocument();
    expect(screen.getByText('R$ 10')).toBeInTheDocument();
    expect(screen.getByText(/12%/)).toHaveClass('delta--up');
    expect(screen.getByText(`${messages.ui.kpi.up}:`)).toBeInTheDocument();
    await expectNoAxeViolations(container);
  });

  it('shows a downward delta', async () => {
    await render('<ui-kpi label="A receber" value="R$ 5" delta="2 vencidos" trend="down" />', {
      imports: [UiKpi],
    });

    expect(screen.getByText(/2 vencidos/)).toHaveClass('delta--down');
    expect(screen.getByText(`${messages.ui.kpi.down}:`)).toBeInTheDocument();
  });

  it('shows a neutral delta and omits a missing one', async () => {
    const { container } = await render(
      '<ui-kpi label="OS" value="14" delta="2 aguardando" /><ui-kpi label="Sem" value="0" />',
      { imports: [UiKpi] },
    );

    expect(screen.getByText(/2 aguardando/)).not.toHaveClass('delta--up');
    expect(container.querySelectorAll('.delta')).toHaveLength(1);
  });
});
