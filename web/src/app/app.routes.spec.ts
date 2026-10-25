import { Route } from '@angular/router';
import { routes } from './app.routes';
import { PERSONAL_DATA_FIELDS } from './core/data/search';

const forbiddenParameters = [
  'document',
  'plate',
  'licensePlate',
  'email',
  'phone',
  ...PERSONAL_DATA_FIELDS,
];

function allPaths(list: readonly Route[], parent = ''): string[] {
  return list.flatMap((route) => {
    const path = [parent, route.path ?? ''].filter(Boolean).join('/');
    return [path, ...allPaths(route.children ?? [], path)];
  });
}

describe('routes', () => {
  it('never use personal data as route parameters', () => {
    const parameters = allPaths(routes).flatMap((path) =>
      [...path.matchAll(/:(\w+)/g)].map((match) => match[1] ?? ''),
    );

    expect(parameters.filter((name) => forbiddenParameters.includes(name))).toEqual([]);
  });

  it('expose the login and access denied screens', () => {
    expect(allPaths(routes)).toEqual(expect.arrayContaining(['login', 'forbidden']));
  });
});
