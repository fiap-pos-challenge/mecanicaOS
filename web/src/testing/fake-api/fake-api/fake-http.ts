import { FakeRequest, FakeResponse, FakeResult } from '../fake-request';
import { messages } from '../../../app/core/i18n/messages.pt-BR';

export interface FakeFieldError {
  readonly field: string;
  readonly code: string;
  readonly message: string;
}

const titles: Readonly<Record<number, string>> = {
  400: 'Bad Request',
  401: 'Unauthorized',
  403: 'Forbidden',
  404: 'Not Found',
  409: 'Conflict',
  422: 'Unprocessable Entity',
  429: 'Too Many Requests',
  500: 'Internal Server Error',
};

let traceCounter = 0;

export function fakeTraceId(): string {
  traceCounter += 1;
  return `00-${traceCounter.toString(16).padStart(32, '0')}-${'0'.repeat(15)}1-01`;
}

export function ok(body: unknown): FakeResponse {
  return { status: 200, body };
}

export function created(body: unknown): FakeResponse {
  return { status: 201, body };
}

export function accepted(body: unknown): FakeResponse {
  return { status: 202, body };
}

export function noContent(): FakeResponse {
  return { status: 204, body: null };
}

export function problem(
  status: number,
  code: string,
  detail: string,
  errors: readonly FakeFieldError[] = [],
  headers?: Readonly<Record<string, string>>,
  extensions: Readonly<Record<string, unknown>> = {},
): FakeResponse {
  return {
    status,
    body: {
      type: `https://httpstatuses.io/${status}`,
      title: titles[status] ?? 'Error',
      status,
      detail,
      code,
      traceId: fakeTraceId(),
      ...(errors.length > 0 ? { errors } : {}),
      ...extensions,
    },
    ...(headers ? { headers } : {}),
  };
}

export function validation(errors: readonly FakeFieldError[]): FakeResponse {
  return problem(400, 'common.validationFailed', messages.errors.validation, errors);
}

export function fieldError(field: string, code: string, message: string): FakeFieldError {
  return { field, code, message };
}

export function readBody(request: FakeRequest): Readonly<Record<string, unknown>> {
  const body =
    typeof request.body === 'string' ? (JSON.parse(request.body) as unknown) : request.body;
  return typeof body === 'object' && body !== null ? (body as Record<string, unknown>) : {};
}

export function readText(body: Readonly<Record<string, unknown>>, key: string): string {
  const value = body[key];
  return typeof value === 'string' ? value.trim() : '';
}

export function paginate<T>(items: readonly T[], query: URLSearchParams): FakeResult {
  const page = Number(query.get('page') ?? '1');
  const pageSize = Number(query.get('pageSize') ?? '20');
  if (
    !Number.isInteger(page) ||
    page < 1 ||
    !Number.isInteger(pageSize) ||
    pageSize < 1 ||
    pageSize > 100
  ) {
    return validation([
      fieldError('pageSize', 'common.invalidPagination', messages.fake.invalidField),
    ]);
  }

  return ok({
    items: items.slice((page - 1) * pageSize, page * pageSize),
    page,
    pageSize,
    totalItems: items.length,
  });
}

export type RouteParams = Readonly<Record<string, string>>;

type RouteHandler = (request: FakeRequest, params: RouteParams) => FakeResult;

interface Route {
  readonly method: string;
  readonly pattern: RegExp;
  readonly names: readonly string[];
  readonly handler: RouteHandler;
}

export class FakeRouter {
  private readonly routes: Route[] = [];

  on(method: string, path: string, handler: RouteHandler): this {
    const names = [...path.matchAll(/:(\w+)/g)].map((match) => match[1] ?? '');
    const pattern = new RegExp(`^${path.replace(/:\w+/g, '([^/]+)')}$`);
    this.routes.push({ method, pattern, names, handler });
    return this;
  }

  handles(path: string, method?: string): boolean {
    return this.routes.some(
      (route) => route.pattern.test(path) && (method === undefined || route.method === method),
    );
  }

  dispatch(request: FakeRequest): FakeResult | null {
    for (const route of this.routes) {
      const match = route.pattern.exec(request.path);
      if (match && route.method === request.method) {
        const params = Object.fromEntries(
          route.names.map((name, index) => [name, decodeURIComponent(match[index + 1] ?? '')]),
        );
        return route.handler(request, params);
      }
    }

    return null;
  }
}
