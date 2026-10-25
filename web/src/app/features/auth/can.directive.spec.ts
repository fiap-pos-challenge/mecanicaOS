import { render, screen } from '@testing-library/angular';
import { provideTestApi } from '../../../testing/api';
import { signInAs } from '../../../testing/auth';
import { provideAuthData } from './data/auth.providers';
import { CanDirective } from './can.directive';

const template = `
  <button *appCan="'customers.delete'; else readOnly">Excluir</button>
  <ng-template #readOnly><span>Somente leitura</span></ng-template>
  <p *appCan="'finance.view'">Financeiro</p>
`;

describe('CanDirective', () => {
  async function setup() {
    return render(template, {
      imports: [CanDirective],
      providers: [...provideTestApi(), provideAuthData()],
    });
  }

  it('shows the content allowed for the profile and the fallback otherwise', async () => {
    const view = await setup();
    expect(screen.queryByRole('button', { name: 'Excluir' })).toBeNull();
    expect(screen.getByText('Somente leitura')).toBeInTheDocument();
    expect(screen.queryByText('Financeiro')).toBeNull();

    signInAs('Admin');
    view.fixture.detectChanges();

    expect(screen.getByRole('button', { name: 'Excluir' })).toBeInTheDocument();
    expect(screen.queryByText('Somente leitura')).toBeNull();
    expect(screen.getByText('Financeiro')).toBeInTheDocument();
  });

  it('hides actions the attendant cannot run', async () => {
    const view = await setup();
    signInAs('Attendant');
    view.fixture.detectChanges();

    expect(screen.queryByRole('button', { name: 'Excluir' })).toBeNull();
    expect(screen.getByText('Somente leitura')).toBeInTheDocument();
  });
});
