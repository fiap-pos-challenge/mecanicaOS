import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { ArrowLeft as ArrowLeftIcon } from 'lucide';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TabItem, UiTabs } from '../../../shared/ui/tabs/tabs';
import { ToastService } from '../../../shared/ui/toast/toast';
import { STATUS_TONES } from '../../service-orders/service-order-display';
import { SyncBanner } from '../components/sync-banner';
import {
  AiSuggestedItem,
  CHECKLIST_STATES,
  ChecklistState,
  MechanicOrder,
} from '../data/mechanic.models';
import { MechanicStore } from '../state/mechanic.store';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';

export const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
export const MAX_PHOTO_BYTES = 5 * 1024 * 1024;

type Section = 'checklist' | 'photos' | 'diagnosis';
type PageLoad = 'loading' | 'ready' | 'pending' | 'error';

let nextPhoto = 0;

export function readAsDataUrl(file: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error ?? new Error('read failed'));
    reader.readAsDataURL(file);
  });
}

@Component({
  selector: 'app-mechanic-order-page',
  imports: [
    UiAiBadge,
    RouterLink,
    SyncBanner,
    UiButton,
    UiControl,
    UiField,
    UiIcon,
    UiPendingNotice,
    UiPlate,
    UiSkeleton,
    UiStatusPill,
    UiTabs,
  ],
  templateUrl: './mechanic-order-page.html',
  styleUrl: './mechanic-order-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MechanicOrderPage {
  readonly id = input<string | null | undefined>(null);

  protected readonly store = inject(MechanicStore);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.mechanic;
  protected readonly orderText = messages.serviceOrders;
  protected readonly pendingText = messages.pending.serviceOrders;
  protected readonly icons = { back: ArrowLeftIcon };
  protected readonly tones = STATUS_TONES;
  protected readonly states = CHECKLIST_STATES;
  protected readonly state = signal<PageLoad>('loading');
  protected readonly section = signal<Section>('checklist');
  protected readonly tabs: readonly TabItem[] = (['checklist', 'photos', 'diagnosis'] as const).map(
    (id) => ({ id, label: this.text.sections[id] }),
  );
  protected readonly description = signal('');
  protected readonly suggestions = signal<readonly AiSuggestedItem[] | null>(null);
  protected readonly selected = signal<ReadonlySet<string>>(new Set());
  protected readonly suggesting = signal(false);
  protected readonly photoError = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly order = computed<MechanicOrder | null>(
    () => this.store.orders()[this.id() ?? ''] ?? null,
  );

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id ?? ''));
    });
  }

  protected async load(id: string): Promise<void> {
    this.state.set('loading');
    try {
      const order = await this.store.loadOrder(id);
      this.description.set(order.diagnosis);
      this.state.set('ready');
    } catch (error) {
      this.state.set(isFeaturePending(error) ? 'pending' : 'error');
    }
  }

  protected selectSection(id: string): void {
    this.section.set(id as Section);
  }

  protected async setState(itemId: string, state: ChecklistState): Promise<void> {
    const order = this.order();
    if (!order) {
      return;
    }

    await this.store.perform(order.id, {
      kind: 'checklist',
      items: order.checklist.map((item) => (item.id === itemId ? { ...item, state } : item)),
    });
  }

  protected async addPhotos(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const order = this.order();
    const files = [...(input.files ?? [])];
    input.value = '';
    this.photoError.set(null);
    if (!order) {
      return;
    }

    for (const file of files) {
      if (!PHOTO_TYPES.includes(file.type)) {
        this.photoError.set(this.text.photos.invalidType);
        continue;
      }

      if (file.size > MAX_PHOTO_BYTES) {
        this.photoError.set(this.text.photos.tooLarge);
        continue;
      }

      await this.store.perform(order.id, {
        kind: 'photo',
        photo: {
          id: `photo-${Date.now()}-${++nextPhoto}`,
          fileName: file.name,
          contentType: file.type,
          dataUrl: await readAsDataUrl(file),
        },
      });
    }
  }

  protected setDescription(event: Event): void {
    this.description.set((event.target as HTMLTextAreaElement).value);
  }

  protected async saveDescription(): Promise<void> {
    const order = this.order();
    if (
      order &&
      (await this.store.perform(order.id, {
        kind: 'diagnosis',
        description: this.description(),
      })) !== 'failed'
    ) {
      this.toast.show(this.text.diagnosis.saved, { tone: 'success' });
    }
  }

  protected async suggest(): Promise<void> {
    const order = this.order();
    if (!order) {
      return;
    }

    this.suggesting.set(true);
    this.error.set(null);
    try {
      const items = await this.store.suggest(order.id, this.description());
      this.suggestions.set(items);
      this.selected.set(new Set(items.map((item) => item.itemId)));
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.suggesting.set(false);
    }
  }

  protected toggle(itemId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(itemId);
      } else {
        next.delete(itemId);
      }

      return next;
    });
  }

  protected async sendToQuote(): Promise<void> {
    const order = this.order();
    if (!order) {
      return;
    }

    const items = (this.suggestions() ?? [])
      .filter((item) => this.selected().has(item.itemId))
      .map((item) => ({ kind: item.kind, itemId: item.itemId }));
    const result = await this.store.perform(order.id, {
      kind: 'sendToQuote',
      request: { description: this.description(), items },
    });
    if (result !== 'failed') {
      this.toast.show(this.text.actions.sent, { tone: 'success' });
    }
  }

  protected async start(): Promise<void> {
    const order = this.order();
    if (order && (await this.store.perform(order.id, { kind: 'start' })) !== 'failed') {
      this.toast.show(this.text.actions.started, { tone: 'success' });
    }
  }

  protected async finish(): Promise<void> {
    const order = this.order();
    if (order && (await this.store.perform(order.id, { kind: 'finish' })) !== 'failed') {
      this.toast.show(this.text.actions.finished, { tone: 'success' });
    }
  }
}
