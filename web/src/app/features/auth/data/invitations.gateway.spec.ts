import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { FAKE_INVITATION_TOKEN } from '../../../../testing/fake-api/fake-api/fake-database';
import { isStrongPassword, maskEmail } from '../../../../testing/fake-api/auth/auth.fake-api';
import { AuthGateway } from './auth.gateway';
import { HttpInvitationsGateway, InvitationsGateway } from './invitations.gateway';

describe('invitation fake helpers', () => {
  it('checks password strength and masks e-mails', () => {
    expect(isStrongPassword('Oficina@2026')).toBe(true);
    expect(isStrongPassword('oficina2026')).toBe(false);
    expect(maskEmail('pedro@oficina.local')).toBe('p•••@oficina.local');
  });
});

describe.each(API_MODES)('InvitationsGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  function setup(): InvitationsGateway {
    harness.configure(mode);
    return TestBed.inject(InvitationsGateway);
  }

  it('shows the invitation and lets the person define the password once', async () => {
    const gateway = setup();

    expect(gateway).toBeInstanceOf(HttpInvitationsGateway);
    const invitation = await firstValueFrom(gateway.inspect(FAKE_INVITATION_TOKEN));
    expect(invitation).toMatchObject({
      name: 'Pedro Convidado',
      maskedEmail: 'p•••@oficina.local',
      role: 'Mechanic',
    });
    await expect(
      firstValueFrom(
        TestBed.inject(AuthGateway).login({ email: 'pedro@oficina.local', password: 'x' }),
      ),
    ).rejects.toMatchObject({ status: 401 });

    await expect(
      firstValueFrom(gateway.accept(FAKE_INVITATION_TOKEN, 'fraca')),
    ).rejects.toMatchObject({
      status: 400,
      code: 'common.validationFailed',
    });
    await firstValueFrom(gateway.accept(FAKE_INVITATION_TOKEN, 'Oficina@2026'));
    await expect(
      firstValueFrom(gateway.accept(FAKE_INVITATION_TOKEN, 'Oficina@2026')),
    ).rejects.toMatchObject({ status: 409, code: 'identity.invitationAlreadyUsed' });
    const login = await firstValueFrom(
      TestBed.inject(AuthGateway).login({ email: 'pedro@oficina.local', password: 'Oficina@2026' }),
    );
    expect(login.kind).toBe('session');
    if (mode === 'http') {
      expect(harness.requests.some((request) => request.url.includes(FAKE_INVITATION_TOKEN))).toBe(
        false,
      );
    }
  });

  it('explains unknown and expired invitations', async () => {
    const gateway = setup();

    await expect(firstValueFrom(gateway.inspect('unknown'))).rejects.toMatchObject({
      status: 404,
      code: 'identity.invitationNotFound',
    });
    const invitation = harness.state(mode).database.invitations[0];
    if (invitation) {
      invitation.expiresAt = '2020-01-01T00:00:00Z';
    }
    await expect(firstValueFrom(gateway.inspect(FAKE_INVITATION_TOKEN))).rejects.toMatchObject({
      status: 422,
      code: 'identity.invitationExpired',
    });
  });
});
