export const APP_TIME_ZONE = 'America/Sao_Paulo';

const isoUtcPattern = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]00:00)$/;

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  timeZone: APP_TIME_ZONE,
  dateStyle: 'short',
});
const dateTimeFormatter = new Intl.DateTimeFormat('pt-BR', {
  timeZone: APP_TIME_ZONE,
  dateStyle: 'short',
  timeStyle: 'short',
});
const timeFormatter = new Intl.DateTimeFormat('pt-BR', {
  timeZone: APP_TIME_ZONE,
  timeStyle: 'short',
});
const partsFormatter = new Intl.DateTimeFormat('en-CA', {
  timeZone: APP_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

export function parseUtc(value: string): Date | null {
  if (!isoUtcPattern.test(value)) {
    return null;
  }

  const date = new Date(value.replace(/(\.\d{3})\d+/, '$1'));
  return Number.isNaN(date.getTime()) ? null : date;
}

function requireUtc(value: string): Date {
  const date = parseUtc(value);
  if (!date) {
    throw new RangeError('Expected an ISO 8601 UTC date.');
  }

  return date;
}

export function formatDate(isoUtc: string): string {
  return dateFormatter.format(requireUtc(isoUtc));
}

export function formatDateTime(isoUtc: string): string {
  return dateTimeFormatter.format(requireUtc(isoUtc)).replace(',', '');
}

export function formatTime(isoUtc: string): string {
  return timeFormatter.format(requireUtc(isoUtc));
}

export function localDateKey(isoUtc: string): string {
  return partsFormatter.format(requireUtc(isoUtc));
}

export function toUtcIso(date: Date): string {
  return date.toISOString();
}
