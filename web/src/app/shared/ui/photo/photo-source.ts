import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';

export interface PhotoContentDto {
  readonly dataUrl?: string;
}

interface PhotoOperation {
  readonly url: string;
}

export function dataUrlOf(dto: PhotoContentDto): string {
  return dto.dataUrl ?? '';
}

@Injectable({ providedIn: 'root', useFactory: () => inject(HttpPhotoSource) })
export abstract class PhotoSource {
  abstract load(path: string): Observable<string>;
}

@Injectable({ providedIn: 'root' })
export class HttpPhotoSource extends PhotoSource {
  private readonly api = inject(ApiClient);

  load(path: string): Observable<string> {
    return this.api.send<PhotoOperation, PhotoContentDto>('GET', path).pipe(map(dataUrlOf));
  }
}
