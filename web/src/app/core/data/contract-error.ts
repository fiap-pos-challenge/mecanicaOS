export class ContractError extends Error {
  constructor(readonly field: string) {
    super(`API response is missing the required field "${field}".`);
    this.name = 'ContractError';
  }
}

export function required<T>(value: T | null | undefined, field: string): T {
  if (value === null || value === undefined) {
    throw new ContractError(field);
  }

  return value;
}
