import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { BARCODE_READER_LOADER, BarcodeReader, BarcodeScanner } from './barcode-scanner';

const text = messages.inventory.entry;

describe('BarcodeScanner', () => {
  const originalDevices = Object.getOwnPropertyDescriptor(navigator, 'mediaDevices');

  beforeEach(() => {
    Object.defineProperty(navigator, 'mediaDevices', {
      configurable: true,
      value: { getUserMedia: vi.fn() },
    });
  });

  afterEach(() => {
    if (originalDevices) {
      Object.defineProperty(navigator, 'mediaDevices', originalDevices);
    } else {
      Reflect.deleteProperty(navigator, 'mediaDevices');
    }
  });

  async function setup(reader: () => Promise<BarcodeReader>) {
    const scanned = vi.fn();
    await render(BarcodeScanner, {
      providers: [{ provide: BARCODE_READER_LOADER, useValue: reader }],
      on: { scanned },
    });
    return { scanned, user: userEvent.setup() };
  }

  it('reads a code from the camera and stops', async () => {
    const stop = vi.fn();
    const holder: { emit?: (result: { getText(): string } | undefined) => void } = {};
    const { scanned, user } = await setup(async () => ({
      decodeFromVideoDevice: async (_device, _video, callback) => {
        holder.emit = callback;
        return { stop };
      },
    }));

    await user.click(screen.getByRole('button', { name: text.camera }));
    expect(await screen.findByText(text.scanning)).toBeInTheDocument();
    holder.emit?.(undefined);
    holder.emit?.({ getText: () => '7891234567895' });

    expect(scanned).toHaveBeenCalledWith('7891234567895');
    expect(stop).toHaveBeenCalled();
    expect(await screen.findByRole('button', { name: text.camera })).toBeVisible();
  });

  it('stops on demand', async () => {
    const stop = vi.fn();
    const { user } = await setup(async () => ({
      decodeFromVideoDevice: async () => ({ stop }),
    }));

    await user.click(screen.getByRole('button', { name: text.camera }));
    await user.click(await screen.findByRole('button', { name: text.stopCamera }));

    expect(stop).toHaveBeenCalled();
  });

  it('explains a denied permission and a missing camera', async () => {
    const { user } = await setup(async () => ({
      decodeFromVideoDevice: async () => {
        const error = new Error('denied');
        error.name = 'NotAllowedError';
        throw error;
      },
    }));

    await user.click(screen.getByRole('button', { name: text.camera }));
    expect(await screen.findByText(text.cameraDenied)).toBeInTheDocument();
  });

  it('falls back when the reader cannot load', async () => {
    const { user } = await setup(async () => {
      throw new Error('offline');
    });

    await user.click(screen.getByRole('button', { name: text.camera }));
    expect(await screen.findByText(text.cameraUnavailable)).toBeInTheDocument();
  });
});
