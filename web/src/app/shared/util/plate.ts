export type PlateFormat = 'old' | 'mercosul';

const oldPattern = /^[A-Z]{3}\d{4}$/;
const mercosulPattern = /^[A-Z]{3}\d[A-Z]\d{2}$/;

export function normalizePlate(value: string): string {
  return value.replace(/[-\s]/g, '').toUpperCase();
}

export function plateFormatOf(value: string): PlateFormat | null {
  const plate = normalizePlate(value);
  if (oldPattern.test(plate)) {
    return 'old';
  }

  return mercosulPattern.test(plate) ? 'mercosul' : null;
}

export function isValidPlate(value: string): boolean {
  return plateFormatOf(value) !== null;
}

export function formatPlate(value: string): string {
  const plate = normalizePlate(value);
  return plateFormatOf(plate) === 'old' ? `${plate.slice(0, 3)}-${plate.slice(3)}` : plate;
}
