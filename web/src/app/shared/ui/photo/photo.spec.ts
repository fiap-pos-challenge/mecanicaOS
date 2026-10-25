import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { render, screen, waitFor } from '@testing-library/angular';
import { Observable, of, Subject } from 'rxjs';
import { provideInterceptorTest } from '../../../../testing/http';
import { UiPhoto } from './photo';
import { HttpPhotoSource, PhotoSource } from './photo-source';

const PICTURE = 'data:image/png;base64,iVBORw0KGgo=';

class StubPhotoSource extends PhotoSource {
  readonly requested: string[] = [];
  readonly pending = new Subject<string>();

  load(path: string): Observable<string> {
    this.requested.push(path);
    return path.endsWith('/slow') ? this.pending : of(PICTURE);
  }
}

async function renderPhoto(path: string, source: StubPhotoSource) {
  return render(`<ui-photo [path]="path" alt="Foto 1" [width]="96" [height]="72" />`, {
    imports: [UiPhoto],
    componentProperties: { path },
    providers: [{ provide: PhotoSource, useValue: source }],
  });
}

describe('UiPhoto', () => {
  it('shows a data URL without asking the API', async () => {
    const source = new StubPhotoSource();
    await renderPhoto(PICTURE, source);

    expect(await screen.findByRole('img', { name: 'Foto 1' })).toHaveAttribute('src', PICTURE);
    expect(source.requested).toEqual([]);
  });

  it('loads an API path on demand and keeps the aspect ratio', async () => {
    const source = new StubPhotoSource();
    const { container } = await renderPhoto('/api/admin/service-orders/1/photos/2', source);

    const image = await screen.findByRole('img', { name: 'Foto 1' });
    expect(image.tagName).toBe('IMG');
    expect(image).toHaveAttribute('src', PICTURE);
    expect(source.requested).toEqual(['/api/admin/service-orders/1/photos/2']);
    expect(container.querySelector('ui-photo')).toHaveStyle({ aspectRatio: '96 / 72' });
  });

  it('shows a busy placeholder while the photo loads', async () => {
    const source = new StubPhotoSource();
    await renderPhoto('/api/admin/service-orders/1/photos/slow', source);

    const placeholder = screen.getByRole('img', { name: 'Foto 1' });
    expect(placeholder.tagName).toBe('SPAN');
    expect(placeholder).toHaveAttribute('aria-busy', 'true');
    source.pending.next(PICTURE);
    await waitFor(() => expect(screen.getByRole('img', { name: 'Foto 1' }).tagName).toBe('IMG'));
  });
});

describe('HttpPhotoSource', () => {
  it('reads the data URL from the photo route', () => {
    TestBed.configureTestingModule({ providers: provideInterceptorTest() });
    const source = TestBed.inject(PhotoSource);
    const http = TestBed.inject(HttpTestingController);
    let result = '';

    expect(source).toBeInstanceOf(HttpPhotoSource);
    source.load('/api/admin/service-orders/1/photos/2').subscribe((value) => (result = value));
    http
      .expectOne('http://localhost/api/admin/service-orders/1/photos/2')
      .flush({ dataUrl: PICTURE });
    expect(result).toBe(PICTURE);

    source.load('/api/admin/service-orders/1/photos/3').subscribe((value) => (result = value));
    http.expectOne('http://localhost/api/admin/service-orders/1/photos/3').flush({});
    expect(result).toBe('');
  });
});
