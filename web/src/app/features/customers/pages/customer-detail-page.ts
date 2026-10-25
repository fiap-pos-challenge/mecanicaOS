import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  ArrowLeft as ArrowLeftIcon,
  Eye as EyeIcon,
  Mail as MailIcon,
  MessageCircle as MessageIcon,
  Pencil as PencilIcon,
  Plus as PlusIcon,
  Repeat as RepeatIcon,
  Trash2 as TrashIcon,
} from 'lucide';
import { firstValueFrom, Subscription } from 'rxjs';
import { ApiError, isApiError, isFeaturePending, toApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiAvatar } from '../../../shared/ui/avatar/avatar';
import { UiButton } from '../../../shared/ui/button/button';
import { UiConfirmDialog, ConfirmDialogData } from '../../../shared/ui/dialog/confirm-dialog';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiIconButton } from '../../../shared/ui/icon-button/icon-button';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { UiPlate } from '../../../shared/ui/plate/plate';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDate, formatDateTime } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { AuthStore } from '../../auth/state/auth.store';
import { CanDirective } from '../../auth/can.directive';
import { CustomerInsightsGateway } from '../data/customer-insights.gateway';
import { CustomerJobsGateway, Job } from '../data/customer-jobs.gateway';
import { Customer, SensitiveField } from '../data/customer.models';
import { CustomersGateway } from '../data/customers.gateway';
import { CustomerSummary, ServiceHistoryEntry } from '../data/insights.models';
import { downloadText, pollJob } from '../data/job-polling';
import { Vehicle } from '../data/vehicle.models';
import { VehiclesGateway } from '../data/vehicles.gateway';
import { TransferVehicleDialog } from '../dialogs/transfer-vehicle-dialog';
import { VehicleFormData, VehicleFormDialog } from '../dialogs/vehicle-form-dialog';
import { CustomersListStore } from '../state/customers-list.store';
import { SITUATION_TONES } from './customer-display';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error';

const mileageFormatter = new Intl.NumberFormat('pt-BR');

@Component({
  selector: 'app-customer-detail-page',
  imports: [
    CanDirective,
    RouterLink,
    UiAvatar,
    UiButton,
    UiIcon,
    UiIconButton,
    UiPendingNotice,
    UiPlate,
    UiSkeleton,
    UiStatusPill,
  ],
  templateUrl: './customer-detail-page.html',
  styleUrl: './customer-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomerDetailPage {
  readonly id = input.required<string>();

  private readonly customers = inject(CustomersGateway);
  private readonly vehiclesGateway = inject(VehiclesGateway);
  private readonly insights = inject(CustomerInsightsGateway);
  private readonly jobs = inject(CustomerJobsGateway);
  private readonly dialogs = inject(UiDialogService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly list = inject(CustomersListStore, { optional: true });
  protected readonly auth = inject(AuthStore);
  private exportPolling: Subscription | null = null;

  protected readonly text = messages.customers.detail;
  protected readonly vehicleText = messages.customers.vehicle;
  protected readonly situations = messages.customers.situations;
  protected readonly documentTypes = messages.customers.documentTypes;
  protected readonly tones = SITUATION_TONES;
  protected readonly icons = {
    back: ArrowLeftIcon,
    eye: EyeIcon,
    mail: MailIcon,
    message: MessageIcon,
    edit: PencilIcon,
    plus: PlusIcon,
    transfer: RepeatIcon,
    trash: TrashIcon,
  };

  protected readonly state = signal<LoadState>('loading');
  protected readonly customer = signal<Customer | null>(null);
  protected readonly vehicles = signal<readonly Vehicle[]>([]);
  protected readonly summary = signal<CustomerSummary | null>(null);
  protected readonly history = signal<readonly ServiceHistoryEntry[]>([]);
  protected readonly vehiclesPending = signal(false);
  protected readonly insightsPending = signal(false);
  protected readonly pending = messages.pending;
  protected readonly revealed = signal<Partial<Record<SensitiveField, string | null>>>({});
  protected readonly revealing = signal<SensitiveField | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly exportJob = signal<Job | null>(null);

  protected readonly canDelete = computed(() => this.vehicles().length === 0);
  protected readonly kpis = computed(() => {
    const summary = this.summary();
    return {
      total: summary ? formatBrl(summary.totalSpent) : '—',
      average: summary ? formatBrl(summary.averageTicket) : '—',
      lastVisit: summary?.lastVisitAt ? formatDate(summary.lastVisitAt) : '—',
      orders: summary?.serviceOrderCount ?? 0,
    };
  });
  protected readonly consentDate = computed(() => {
    const date = this.customer()?.messagingConsent.updatedAt;
    return date ? this.text.consentAt(formatDateTime(date)) : this.text.consentNever;
  });
  protected readonly whatsAppBlocked = computed(() =>
    this.customer()?.messagingConsent.whatsApp ? null : this.text.noWhatsAppConsent,
  );
  protected readonly emailBlocked = computed(() => {
    const customer = this.customer();
    if (!customer?.maskedEmail) {
      return this.text.noEmailAddress;
    }

    return customer.messagingConsent.email ? null : this.text.noEmailConsent;
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
    inject(DestroyRef).onDestroy(() => this.exportPolling?.unsubscribe());
  }

  protected value(field: SensitiveField, masked: string | null): string | null {
    const revealed = this.revealed();
    return field in revealed ? (revealed[field] ?? null) : masked;
  }

  protected isRevealed(field: SensitiveField): boolean {
    return field in this.revealed();
  }

  protected async reveal(field: SensitiveField): Promise<void> {
    const customer = this.customer();
    if (!customer) {
      return;
    }

    this.revealing.set(field);
    try {
      const value = await firstValueFrom(this.customers.reveal(customer.id, field, 'View'));
      this.revealed.update((current) => ({ ...current, [field]: value }));
    } catch (error) {
      this.actionError.set(this.messageOf(error));
    } finally {
      this.revealing.set(null);
    }
  }

  protected date(value: string): string {
    return formatDate(value);
  }

  protected money(value: CustomerSummary['totalSpent']): string {
    return formatBrl(value);
  }

  protected mileage(vehicle: Vehicle): string {
    return this.vehicleText.mileageValue(mileageFormatter.format(vehicle.mileage));
  }

  protected newQuote(): void {
    const customer = this.customer();
    if (customer) {
      void this.router.navigate(['/quotes'], { state: { customerId: customer.id } });
    }
  }

  protected sendMessage(): void {
    this.toast.show(this.text.messagingSoon);
  }

  protected openVehicle(vehicle: Vehicle | null): void {
    const customer = this.customer();
    if (!customer) {
      return;
    }

    const data: VehicleFormData = { customerId: customer.id, vehicle };
    this.dialogs
      .open<VehicleFormDialog, VehicleFormData, Vehicle>(VehicleFormDialog, data)
      .closed.subscribe((saved) => {
        if (saved) {
          void this.reloadVehicles();
        }
      });
  }

  protected transfer(vehicle: Vehicle): void {
    this.dialogs
      .open<TransferVehicleDialog, Vehicle, Vehicle>(TransferVehicleDialog, vehicle)
      .closed.subscribe((moved) => {
        if (moved) {
          void this.reloadVehicles();
        }
      });
  }

  protected removeVehicle(vehicle: Vehicle): void {
    this.confirm({
      title: this.vehicleText.removeTitle,
      body: this.vehicleText.removeBody(vehicle.licensePlate),
      confirm: this.vehicleText.removeTitle,
      danger: true,
    }).subscribe(async (confirmed) => {
      if (!confirmed) {
        return;
      }

      try {
        await firstValueFrom(this.vehiclesGateway.remove(vehicle.id));
        this.toast.show(this.vehicleText.removed, { tone: 'success' });
        await this.reloadVehicles();
      } catch (error) {
        this.actionError.set(this.messageOf(error));
      }
    });
  }

  protected deleteCustomer(): void {
    const customer = this.customer();
    if (!customer) {
      return;
    }

    this.confirm({
      title: this.text.deleteTitle,
      body: this.text.deleteBody(customer.name),
      confirm: this.text.deleteConfirm,
      danger: true,
    }).subscribe(async (confirmed) => {
      if (!confirmed) {
        return;
      }

      try {
        await firstValueFrom(this.customers.remove(customer.id));
        this.toast.show(this.text.deleted, { tone: 'success' });
        void this.list?.load();
        await this.router.navigateByUrl('/customers');
      } catch (error) {
        this.actionError.set(this.messageOf(error));
      }
    });
  }

  protected async requestExport(): Promise<void> {
    const customer = this.customer();
    if (!customer) {
      return;
    }

    this.actionError.set(null);
    try {
      const job = await firstValueFrom(this.jobs.requestExport(customer.id));
      this.exportJob.set(job);
      this.toast.show(this.text.exportQueued);
      this.exportPolling?.unsubscribe();
      this.exportPolling = pollJob((id) => this.jobs.job(id), job.id).subscribe({
        next: (current) => this.exportJob.set(current),
        error: (error: unknown) => this.actionError.set(this.messageOf(error)),
      });
    } catch (error) {
      this.actionError.set(this.messageOf(error));
    }
  }

  protected downloadExport(): void {
    const job = this.exportJob();
    if (job?.content && job.fileName) {
      downloadText(this.document, job.fileName, job.content, 'application/json');
    }
  }

  private confirm(data: ConfirmDialogData) {
    return this.dialogs.open<UiConfirmDialog, ConfirmDialogData, boolean>(UiConfirmDialog, data)
      .closed;
  }

  private async load(id: string): Promise<void> {
    this.state.set('loading');
    this.revealed.set({});
    this.actionError.set(null);
    this.exportJob.set(null);
    try {
      const customer = await firstValueFrom(this.customers.get(id));
      this.customer.set(customer);
      this.state.set('ready');
      await Promise.all([this.reloadVehicles(false), this.loadInsights(id)]);
    } catch (error) {
      this.customer.set(null);
      this.state.set(isApiError(error) && error.status === 404 ? 'notFound' : 'error');
    }
  }

  private async loadInsights(id: string): Promise<void> {
    let pending = false;
    const flag = (error: unknown): void => {
      pending ||= isFeaturePending(error);
    };
    const [summaries, history] = await Promise.all([
      firstValueFrom(this.insights.summaries([id])).catch((error: unknown) => {
        flag(error);
        return new Map<string, CustomerSummary>();
      }),
      firstValueFrom(this.insights.history(id)).catch((error: unknown) => {
        flag(error);
        return [] as readonly ServiceHistoryEntry[];
      }),
    ]);
    this.insightsPending.set(pending);
    this.summary.set(summaries.get(id) ?? null);
    this.history.set(history);
  }

  private async reloadVehicles(refreshList = true): Promise<void> {
    const customer = this.customer();
    if (!customer) {
      return;
    }

    try {
      this.vehicles.set(await firstValueFrom(this.vehiclesGateway.listByCustomer(customer.id)));
      this.vehiclesPending.set(false);
    } catch (error) {
      this.vehicles.set([]);
      this.vehiclesPending.set(isFeaturePending(error));
    }

    if (refreshList) {
      void this.list?.load();
    }
  }

  private messageOf(error: unknown): string {
    const apiError: ApiError = isApiError(error) ? error : toApiError(error, null);
    return apiError.detail;
  }
}
