import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MAX_PAGE_SIZE } from '../../../core/data/page';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { StatusTone, UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatDateTime } from '../../../shared/util/date';
import { CanDirective } from '../../auth/can.directive';
import { InventoryGateway } from '../data/inventory.gateway';
import { countSummary } from '../data/inventory.math';
import { StockCount } from '../data/inventory.models';
import { COUNT_TONES } from './inventory-display';

@Component({
  selector: 'app-counts-page',
  imports: [
    CanDirective,
    ReactiveFormsModule,
    RouterLink,
    UiButton,
    UiCell,
    UiControl,
    UiField,
    UiPendingNotice,
    UiStatusPill,
    UiTable,
  ],
  templateUrl: './counts-page.html',
  styleUrl: './counts-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountsPage {
  private readonly gateway = inject(InventoryGateway);
  private readonly router = inject(Router);

  protected readonly text = messages.inventory.counts;
  protected readonly tones = COUNT_TONES;
  protected readonly pendingText = messages.pending.inventory;
  protected readonly form = new FormGroup({
    location: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected readonly counts = signal<readonly StockCount[]>([]);
  protected readonly locations = signal<readonly string[]>([]);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly pending = signal(false);
  protected readonly opening = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly columns: readonly TableColumn<StockCount>[] = [
    { key: 'location', header: this.text.columns.location, value: (row) => row.location },
    { key: 'status', header: this.text.columns.status },
    {
      key: 'progress',
      header: this.text.columns.progress,
      value: (row) => {
        const summary = countSummary(row.lines);
        return this.text.progress(summary.counted, summary.total);
      },
    },
    { key: 'openedBy', header: this.text.columns.openedBy, value: (row) => row.openedBy },
    {
      key: 'openedAt',
      header: this.text.columns.openedAt,
      value: (row) => formatDateTime(row.openedAt),
    },
    { key: 'actions', header: this.text.columns.actions },
  ];
  protected readonly trackRow = (row: StockCount): string => row.id;

  constructor() {
    void this.load();
  }

  protected statusTone(count: StockCount): StatusTone {
    return COUNT_TONES[count.status];
  }

  protected statusLabel(count: StockCount): string {
    return this.text.statuses[count.status];
  }

  protected actionOf(count: StockCount): string {
    return count.status === 'Open'
      ? this.text.continue
      : count.status === 'PendingApproval'
        ? this.text.review
        : this.text.view;
  }

  protected async open(): Promise<void> {
    const location = this.form.controls.location;
    if (location.invalid) {
      location.markAsTouched();
      this.error.set(this.text.chooseLocation);
      return;
    }

    this.opening.set(true);
    this.error.set(null);
    try {
      const count = await firstValueFrom(this.gateway.openCount(location.value));
      await this.router.navigate(['/inventory/counts', count.id]);
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.opening.set(false);
    }
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);
    this.pending.set(false);
    try {
      const [counts, overview] = await Promise.all([
        firstValueFrom(this.gateway.counts()),
        firstValueFrom(
          this.gateway.overview({
            page: 1,
            pageSize: MAX_PAGE_SIZE,
            term: '',
            situation: null,
            location: null,
          }),
        ),
      ]);
      this.counts.set(counts);
      this.locations.set(overview.locations);
    } catch (error) {
      this.counts.set([]);
      this.pending.set(isFeaturePending(error));
      this.failed.set(!isFeaturePending(error));
    } finally {
      this.loading.set(false);
    }
  }
}
