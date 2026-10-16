import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideInterceptorTest } from '../../../testing/http';
import { messages } from '../../core/i18n/messages.pt-BR';
import { VersionService } from '../../core/version/version.service';
import { UpdateBanner } from './update-banner';

describe('UpdateBanner', () => {
  it('appears only when a new version exists and reloads on demand', async () => {
    const view = await render(UpdateBanner, { providers: provideInterceptorTest() });
    const version = TestBed.inject(VersionService);
    const reload = vi.spyOn(version, 'reload').mockImplementation(() => undefined);
    expect(screen.queryByText(messages.pages.update.banner)).toBeNull();

    version.markOutdated();
    view.fixture.detectChanges();
    await userEvent.click(screen.getByRole('button', { name: messages.pages.update.reload }));

    expect(screen.getByRole('status')).toHaveTextContent(messages.pages.update.banner);
    expect(reload).toHaveBeenCalled();
  });
});
