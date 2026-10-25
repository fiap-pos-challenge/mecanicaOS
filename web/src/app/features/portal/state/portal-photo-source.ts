import { inject, Injectable } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { dataUrlOf, PhotoContentDto, PhotoSource } from '../../../shared/ui/photo/photo-source';
import { PORTAL_SESSION_HEADER, publicContext } from '../../documents/data/documents.gateway';
import { PortalStore } from './portal.store';

interface PortalPhotoOperation {
  readonly url: string;
}

@Injectable()
export class PortalPhotoSource extends PhotoSource {
  private readonly api = inject(ApiClient);
  private readonly store = inject(PortalStore);

  load(path: string): Observable<string> {
    const session = this.store.session();
    if (!session) {
      return of('');
    }

    return this.api
      .send<PortalPhotoOperation, PhotoContentDto>('GET', path, {
        headers: { [PORTAL_SESSION_HEADER]: session },
        context: publicContext(),
      })
      .pipe(map(dataUrlOf));
  }
}
