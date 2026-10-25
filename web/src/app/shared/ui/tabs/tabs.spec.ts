import { signal } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { UiTabs } from './tabs';

const tabs = [
  { id: 'packages', label: 'Pacotes' },
  { id: 'services', label: 'Serviços' },
  { id: 'parts', label: 'Peças' },
];

async function setup() {
  const selected = signal('packages');
  const view = await render(
    '<ui-tabs label="Itens" [tabs]="tabs" [(selected)]="selected"><p>Conteúdo</p></ui-tabs>',
    {
      imports: [UiTabs],
      componentProperties: { tabs, selected },
    },
  );
  return { selected, view };
}

describe('UiTabs', () => {
  it('links the selected tab and the panel', async () => {
    const { view } = await setup();

    const tab = screen.getByRole('tab', { name: 'Pacotes' });
    expect(tab).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tabpanel', { name: 'Pacotes' })).toHaveTextContent('Conteúdo');
    expect(screen.getByRole('tablist', { name: 'Itens' })).toBeInTheDocument();
    await expectNoAxeViolations(view.container);
  });

  it('selects on click and with the keyboard', async () => {
    const { selected } = await setup();

    await userEvent.click(screen.getByRole('tab', { name: 'Serviços' }));
    expect(selected()).toBe('services');

    await userEvent.keyboard('{ArrowRight}');
    expect(selected()).toBe('parts');
    expect(screen.getByRole('tab', { name: 'Peças' })).toHaveFocus();

    await userEvent.keyboard('{ArrowRight}');
    expect(selected()).toBe('packages');

    await userEvent.keyboard('{ArrowLeft}');
    expect(selected()).toBe('parts');

    await userEvent.keyboard('{Home}');
    expect(selected()).toBe('packages');

    await userEvent.keyboard('{End}');
    expect(selected()).toBe('parts');

    await userEvent.keyboard('{Enter}');
    expect(selected()).toBe('parts');
  });
});
