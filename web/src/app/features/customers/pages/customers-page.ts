import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Plus as PlusIcon, Search as SearchIcon, Upload as UploadIcon } from 'lucide';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiChip } from '../../../shared/ui/chip/chip';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { SegmentedOption, UiSegmented } from '../../../shared/ui/segmented/segmented';
import { StatusTone, UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatDate } from '../../../shared/util/date';
import { formatBrl } from '../../../shared/util/money';
import { CanDirective } from '../../auth/can.directive';
import { ImportCustomersDialog } from '../dialogs/import-customers-dialog';
import { CustomerEmptyPage } from './customer-empty-page';
import { SITUATION_TONES } from './customer-display';
import { CustomerListSort, CustomerRow, CustomersListStore } from '../state/customers-list.store';

export const SEARCH_DEBOUNCE_MS = 300;

type TypeFilter = 'all' | 'Cpf' | 'Cnpj';

@Component({
  selector: 'app-customers-page',
  imports: [
    CanDirective,
    ReactiveFormsModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    UiButton,
    UiCell,
    UiChip,
    UiControl,
    UiField,
    UiIcon,
    UiPendingNotice,
    UiSegmented,
    UiStatusPill,
    UiTable,
  ],
  providers: [CustomersListStore],
  templateUrl: './customers-page.html',
  styleUrl: './customers-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomersPage {
  protected readonly store = inject(CustomersListStore);
  private readonly dialogs = inject(UiDialogService);

  protected readonly screen = messages.screens.customers;
  protected readonly text = messages.customers.list;
  protected readonly situations = messages.customers.situations;
  protected readonly pending = messages.pending;
  protected readonly tones = SITUATION_TONES;
  protected readonly icons = { plus: PlusIcon, search: SearchIcon, upload: UploadIcon };
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly detailOpen = signal(false);

  protected readonly typeOptions: readonly SegmentedOption<TypeFilter>[] = [
    { value: 'all', label: this.text.typeAll },
    { value: 'Cpf', label: this.text.typePerson },
    { value: 'Cnpj', label: this.text.typeCompany },
  ];
  protected readonly sortOptions: readonly { value: CustomerListSort; label: string }[] = [
    { value: 'nameAsc', label: this.text.sortNameAsc },
    { value: 'nameDesc', label: this.text.sortNameDesc },
    { value: 'newest', label: this.text.sortNewest },
    { value: 'oldest', label: this.text.sortOldest },
  ];
  protected readonly columns: readonly TableColumn<CustomerRow>[] = [
    { key: 'name', header: this.text.columns.name },
    { key: 'document', header: this.text.columns.document },
    { key: 'vehicles', header: this.text.columns.vehicles },
    { key: 'lastVisit', header: this.text.columns.lastVisit },
    { key: 'total', header: this.text.columns.total, align: 'end' },
    { key: 'situation', header: this.text.columns.situation },
  ];

  protected readonly typeFilter = computed<TypeFilter>(
    () => this.store.filters().documentType ?? 'all',
  );
  protected readonly trackRow = (row: CustomerRow): string => row.customer.id;

  constructor() {
    this.search.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => void this.store.update({ term }));
    void this.store.load();
  }

  protected setType(value: TypeFilter): void {
    void this.store.update({ documentType: value === 'all' ? null : value });
  }

  protected setSituation(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    void this.store.update({
      situation: value === 'Active' || value === 'OpenServiceOrder' ? value : null,
    });
  }

  protected setSort(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const option = this.sortOptions.find((candidate) => candidate.value === value);
    if (option) {
      void this.store.update({ sort: option.value });
    }
  }

  protected clear(): void {
    this.search.setValue('', { emitEvent: false });
    void this.store.clear();
  }

  protected onActivate(page: unknown): void {
    this.detailOpen.set(!(page instanceof CustomerEmptyPage));
  }

  protected openImport(): void {
    this.dialogs.open(ImportCustomersDialog);
  }

  protected lastVisit(row: CustomerRow): string {
    const date = row.summary?.lastVisitAt;
    return date ? formatDate(date) : this.text.never;
  }

  protected total(row: CustomerRow): string {
    return row.summary ? formatBrl(row.summary.totalSpent) : '—';
  }

  protected situation(
    row: CustomerRow,
  ): { readonly tone: StatusTone; readonly label: string; readonly fleet: boolean } | null {
    const summary = row.summary;
    return summary
      ? {
          tone: this.tones[summary.situation],
          label: this.situations[summary.situation],
          fleet: summary.fleet,
        }
      : null;
  }

  protected vehicles(row: CustomerRow): string {
    return row.summary ? this.text.vehicleCount(row.summary.vehicleCount) : '—';
  }
}
