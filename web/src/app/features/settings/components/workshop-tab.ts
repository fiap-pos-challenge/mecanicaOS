import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDate } from '../../../shared/util/date';
import {
  BusinessHours,
  validateWorkshop,
  Workshop,
  WorkshopAddress,
} from '../data/settings.models';
import { SectionLoad, SettingsStore } from '../state/settings.store';

export const LOGO_MAX_BYTES = 512 * 1024;

export function validLogo(file: Pick<File, 'type' | 'size'>): boolean {
  return ['image/png', 'image/jpeg'].includes(file.type) && file.size <= LOGO_MAX_BYTES;
}

@Component({
  selector: 'app-workshop-tab',
  imports: [UiButton, UiControl, UiField, UiSkeleton],
  templateUrl: './workshop-tab.html',
  styleUrl: './settings-tab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkshopTab {
  protected readonly store = inject(SettingsStore);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.settings.workshop;
  protected readonly common = messages.settings;
  protected readonly state = signal<SectionLoad>('loading');
  protected readonly draft = signal<Workshop | null>(null);
  protected readonly submitted = signal(false);
  protected readonly logoError = signal(false);
  protected readonly problems = computed(() => {
    const draft = this.draft();
    return draft ? validateWorkshop(draft) : [];
  });
  protected readonly plan = computed(() => {
    const plan = this.draft()?.plan;
    return plan
      ? this.text.planDetail(plan.name, plan.users, plan.maxUsers, formatDate(plan.renewsAt))
      : '';
  });

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    const workshop = await this.store.load(this.store.gateway.workshop(), this.state);
    if (workshop) {
      this.draft.set(workshop);
      this.store.workshopName.set(workshop.name);
      this.store.planName.set(workshop.plan.name);
    }
  }

  protected set(key: 'name' | 'document', event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.draft.update((draft) => (draft ? { ...draft, [key]: value } : draft));
  }

  protected setAddress(key: keyof WorkshopAddress, event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const value = key === 'state' ? raw.toUpperCase() : raw;
    this.draft.update((draft) =>
      draft ? { ...draft, address: { ...draft.address, [key]: value } } : draft,
    );
  }

  protected setHours(index: number, key: keyof BusinessHours, event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = key === 'closed' ? input.checked : input.value;
    this.draft.update((draft) =>
      draft
        ? {
            ...draft,
            hours: draft.hours.map((day, position) =>
              position === index ? { ...day, [key]: value } : day,
            ),
          }
        : draft,
    );
  }

  protected async save(): Promise<void> {
    const draft = this.draft();
    this.submitted.set(true);
    if (!draft || this.problems().length > 0) {
      return;
    }

    const saved = await this.store.change(this.text.change, (token) =>
      this.store.gateway.saveWorkshop(draft, token),
    );
    if (saved) {
      this.draft.set(saved);
      this.store.workshopName.set(saved.name);
      this.toast.show(this.common.saved, { tone: 'success' });
    }
  }

  protected async pickLogo(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    if (!validLogo(file)) {
      this.logoError.set(true);
      return;
    }

    this.logoError.set(false);
    const dataUrl = await readAsDataUrl(file);
    const logoUrl = await this.store.change(this.text.logoChange, (token) =>
      this.store.gateway.uploadLogo(
        { fileName: file.name, contentType: file.type, dataUrl },
        token,
      ),
    );
    if (logoUrl) {
      this.draft.update((draft) => (draft ? { ...draft, logoUrl } : draft));
      this.toast.show(this.text.logoSaved, { tone: 'success' });
    }
  }
}

export function readAsDataUrl(file: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}
