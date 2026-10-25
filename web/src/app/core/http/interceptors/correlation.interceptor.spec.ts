import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL, provideInterceptorTest } from '../../../../testing/http';
import {
  correlationInterceptor,
  createTraceparent,
  TRACEPARENT_HEADER,
} from './correlation.interceptor';

describe('correlationInterceptor', () => {
  it('creates W3C traceparent values', () => {
    expect(createTraceparent()).toMatch(/^00-[0-9a-f]{32}-[0-9a-f]{16}-01$/);
    expect(createTraceparent((length) => new Uint8Array(length).fill(171))).toBe(
      `00-${'ab'.repeat(16)}-${'ab'.repeat(8)}-01`,
    );
  });

  it('adds a new trace to each API request only', () => {
    TestBed.configureTestingModule({ providers: provideInterceptorTest(correlationInterceptor) });
    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);

    client.get(API_URL).subscribe();
    client.get(API_URL).subscribe();
    client.get('https://cdn.example.com/file.json').subscribe();
    const [first, second] = http.match(API_URL);

    expect(first?.request.headers.get(TRACEPARENT_HEADER)).toMatch(/^00-/);
    expect(first?.request.headers.get(TRACEPARENT_HEADER)).not.toBe(
      second?.request.headers.get(TRACEPARENT_HEADER),
    );
    expect(
      http.expectOne('https://cdn.example.com/file.json').request.headers.has(TRACEPARENT_HEADER),
    ).toBe(false);
  });
});
