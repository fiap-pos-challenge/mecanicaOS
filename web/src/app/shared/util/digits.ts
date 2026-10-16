export function onlyDigits(value: string): string {
  return value.replace(/\D/g, '');
}

export function hasOnly(value: string, allowed: RegExp): boolean {
  return [...value].every((character) => /\d/.test(character) || allowed.test(character));
}
