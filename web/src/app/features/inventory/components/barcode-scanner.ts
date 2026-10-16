import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  InjectionToken,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { Camera as CameraIcon, CameraOff as CameraOffIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiIcon } from '../../../shared/ui/icon/icon';

export interface ScannerControls {
  stop(): void;
}

export interface BarcodeReader {
  decodeFromVideoDevice(
    deviceId: string | undefined,
    video: HTMLVideoElement,
    callback: (result: { getText(): string } | undefined) => void,
  ): Promise<ScannerControls>;
}

export type BarcodeReaderLoader = () => Promise<BarcodeReader>;

export const BARCODE_READER_LOADER = new InjectionToken<BarcodeReaderLoader>(
  'BARCODE_READER_LOADER',
  {
    providedIn: 'root',
    factory: () => async () => {
      const { BrowserMultiFormatReader } = await import('@zxing/browser');
      return new BrowserMultiFormatReader();
    },
  },
);

type ScannerState = 'idle' | 'starting' | 'scanning' | 'unavailable' | 'denied';

@Component({
  selector: 'app-barcode-scanner',
  imports: [UiButton, UiIcon],
  template: `
    <div class="controls">
      @if (state() === 'scanning' || state() === 'starting') {
        <button uiButton size="sm" type="button" (click)="stop()">
          <ui-icon [icon]="icons.off" [size]="16" />
          {{ text.stopCamera }}
        </button>
        <span class="status" role="status">{{ text.scanning }}</span>
      } @else {
        <button uiButton size="sm" type="button" (click)="start()">
          <ui-icon [icon]="icons.on" [size]="16" />
          {{ text.camera }}
        </button>
      }
    </div>
    @if (state() === 'unavailable') {
      <p class="message" role="status">{{ text.cameraUnavailable }}</p>
    }
    @if (state() === 'denied') {
      <p class="message" role="status">{{ text.cameraDenied }}</p>
    }
    <video
      #video
      class="video"
      [class.video--on]="state() === 'scanning'"
      muted
      playsinline
      [attr.aria-label]="text.cameraPreview"
    ></video>
  `,
  styleUrl: './barcode-scanner.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarcodeScanner {
  readonly scanned = output<string>();

  private readonly loader = inject(BARCODE_READER_LOADER);
  private readonly document = inject(DOCUMENT);
  private readonly video = viewChild.required<ElementRef<HTMLVideoElement>>('video');
  private controls: ScannerControls | null = null;

  protected readonly text = messages.inventory.entry;
  protected readonly icons = { on: CameraIcon, off: CameraOffIcon };
  protected readonly state = signal<ScannerState>('idle');

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stop());
  }

  async start(): Promise<void> {
    if (!this.document.defaultView?.navigator.mediaDevices?.getUserMedia) {
      this.state.set('unavailable');
      return;
    }

    this.state.set('starting');
    try {
      const reader = await this.loader();
      this.controls = await reader.decodeFromVideoDevice(
        undefined,
        this.video().nativeElement,
        (result) => {
          if (result) {
            this.stop();
            this.scanned.emit(result.getText());
          }
        },
      );
      if (this.state() === 'starting') {
        this.state.set('scanning');
      }
    } catch (error) {
      this.state.set(
        error instanceof Error && error.name === 'NotAllowedError' ? 'denied' : 'unavailable',
      );
    }
  }

  stop(): void {
    this.controls?.stop();
    this.controls = null;
    if (this.state() === 'scanning' || this.state() === 'starting') {
      this.state.set('idle');
    }
  }
}
