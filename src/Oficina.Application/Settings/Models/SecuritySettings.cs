namespace Oficina.Application.Settings.Models;

public sealed record SecuritySettings(bool RequireTwoFactorForAll, int IdleMinutes, int LockoutAttempts, int LockoutMinutes);
