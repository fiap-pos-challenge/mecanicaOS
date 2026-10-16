export interface FakeRequest {
  readonly method: string;
  readonly path: string;
  readonly query: URLSearchParams;
  readonly body: unknown;
  readonly authorization: string | null;
  readonly portalSession?: string | null;
  readonly reauthentication?: string | null;
}

export interface FakeResponse {
  readonly status: number;
  readonly body: unknown;
  readonly headers?: Readonly<Record<string, string>>;
}

export interface FakeTimeout {
  readonly timeout: true;
}

export type FakeResult = FakeResponse | FakeTimeout;

export interface FakeBackendHandler {
  handle(request: FakeRequest): FakeResult | null;
}

export function isFakeTimeout(result: FakeResult): result is FakeTimeout {
  return 'timeout' in result;
}
