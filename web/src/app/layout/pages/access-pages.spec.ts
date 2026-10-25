import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi } from '../../../testing/api';
import { signInAs } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { ErrorState } from '../../core/errors/error-state';
import { messages } from '../../core/i18n/messages.pt-BR';
import { VersionService } from '../../core/version/version.service';
import { provideAuthData } from '../../features/auth/data/auth.providers';
import { ForbiddenPage } from './forbidden-page';
import { NotFoundPage } from './not-found-page';
import { ServerErrorPage } from './server-error-page';
import { UpdatePage } from './update-page';

const providers = [...provideTestApi(), provideAuthData()];

describe('access and status pages', () => {
  it('explains access denied and links to the home of the profile', async () => {
    const { container, fixture } = await render(ForbiddenPage, { providers });
    signInAs('Mechanic');
    fixture.detectChanges();

    expect(
      screen.getByRole('heading', { level: 1, name: messages.pages.forbidden.title }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: messages.shell.home })).toHaveAttribute(
      'href',
      '/mechanic',
    );
    await expectNoAxeViolations(container);
  });

  it('offers the way home from a missing page', async () => {
    const { fixture } = await render(NotFoundPage, { providers });
    signInAs('Attendant');
    fixture.detectChanges();

    expect(screen.getByRole('link', { name: messages.shell.home })).toHaveAttribute(
      'href',
      '/dashboard',
    );
  });

  it('shows the trace id of the failure and retries the failed address', async () => {
    const { container } = await render(ServerErrorPage, {
      providers,
      configureTestBed: () => {
        TestBed.inject(ErrorState).capture(
          {
            kind: 'api-error',
            status: 500,
            code: 'common.unexpected',
            title: 'Erro',
            detail: 'Falhou',
            traceId: '00-trace-01',
            fieldErrors: [],
            retryAfterSeconds: null,
          },
          '/customers',
        );
      },
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    expect(screen.getByText('00-trace-01')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: messages.pages.serverError.retry }));

    expect(navigate).toHaveBeenCalledWith('/customers');
    await expectNoAxeViolations(container);
  });

  it('says when there is no trace id and retries at home', async () => {
    await render(ServerErrorPage, { providers });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    expect(screen.getByText(messages.pages.serverError.noTrace)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: messages.pages.serverError.retry }));

    expect(navigate).toHaveBeenCalledWith('/login');
  });

  it('reloads the application on the new version screen', async () => {
    const { container } = await render(UpdatePage, { providers });
    const reload = vi
      .spyOn(TestBed.inject(VersionService), 'reload')
      .mockImplementation(() => undefined);

    await userEvent.click(screen.getByRole('button', { name: messages.pages.update.reload }));

    expect(reload).toHaveBeenCalled();
    await expectNoAxeViolations(container);
  });
});
