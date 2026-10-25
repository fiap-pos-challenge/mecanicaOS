import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { patchState } from '@ngrx/signals';
import { unprotected } from '@ngrx/signals/testing';
import { provideInterceptorTest } from '../../../../testing/http';
import { providePortalData } from '../data/portal.gateway';
import { PortalPhotoSource } from './portal-photo-source';
import { PortalStore } from './portal.store';

describe('PortalPhotoSource', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [...provideInterceptorTest(), providePortalData(), PortalStore, PortalPhotoSource],
    });
    return {
      source: TestBed.inject(PortalPhotoSource),
      store: TestBed.inject(PortalStore),
      http: TestBed.inject(HttpTestingController),
    };
  }

  it('sends the portal session with the photo request', () => {
    const { source, store, http } = setup();
    let result = '';
    patchState(unprotected(store), { session: 'sessao-1' });

    source.load('/api/public/portal/photos/9').subscribe((value) => (result = value));
    const request = http.expectOne('http://localhost/api/public/portal/photos/9');
    request.flush({ dataUrl: 'data:image/png;base64,AAAA' });

    expect(request.request.headers.get('X-Portal-Session')).toBe('sessao-1');
    expect(result).toBe('data:image/png;base64,AAAA');
  });

  it('does not call the API without a session', () => {
    const { source, http } = setup();
    let result = 'x';

    source.load('/api/public/portal/photos/9').subscribe((value) => (result = value));

    http.expectNone('http://localhost/api/public/portal/photos/9');
    expect(result).toBe('');
  });
});
