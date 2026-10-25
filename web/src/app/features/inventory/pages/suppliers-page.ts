import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Plus as PlusIcon } from 'lucide';
import { firstValueFrom } from 'rxjs';
import { isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogService } from '../../../shared/ui/dialog/dialog';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiPendingNotice } from '../../../shared/ui/pending-notice/pending-notice';
import { TableColumn, UiCell, UiTable } from '../../../shared/ui/table/table';
import { formatDocument } from '../../../shared/util/document';
import { formatPhone } from '../../../shared/util/phone';
import { CanDirective } from '../../auth/can.directive';
import { InventoryGateway } from '../data/inventory.gateway';
import { Supplier } from '../data/inventory.models';
import { SupplierDialog } from '../dialogs/supplier-dialog';

@Component({
  selector: 'app-suppliers-page',
  imports: [CanDirective, UiButton, UiCell, UiIcon, UiPendingNotice, UiTable],
  templateUrl: './suppliers-page.html',
  styleUrl: './suppliers-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuppliersPage {
  private readonly gateway = inject(InventoryGateway);
  private readonly dialogs = inject(UiDialogService);

  protected readonly text = messages.inventory.suppliers;
  protected readonly pendingText = messages.pending.inventory;
  protected readonly icons = { plus: PlusIcon };
  protected readonly suppliers = signal<readonly Supplier[]>([]);
  protected readonly loading = signal(true);
  protected readonly pending = signal(false);
  protected readonly failed = signal(false);
  protected readonly columns: readonly TableColumn<Supplier>[] = [
    { key: 'name', header: this.text.columns.name, value: (row) => row.name },
    {
      key: 'document',
      header: this.text.columns.document,
      value: (row) => (row.document ? formatDocument(row.document) : '—'),
    },
    {
      key: 'contact',
      header: this.text.columns.contact,
      value: (row) =>
        [row.email, row.phone ? formatPhone(row.phone) : null].filter(Boolean).join(' · ') || '—',
    },
    {
      key: 'leadTime',
      header: this.text.columns.leadTime,
      value: (row) => this.text.leadTimeValue(row.leadTimeDays),
    },
    { key: 'actions', header: this.text.columns.actions },
  ];
  protected readonly trackRow = (row: Supplier): string => row.id;

  constructor() {
    void this.load();
  }

  protected open(supplier: Supplier | null): void {
    this.dialogs
      .open<SupplierDialog, Supplier | null, Supplier>(SupplierDialog, supplier)
      .closed.subscribe((saved) => {
        if (saved) {
          void this.load();
        }
      });
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.pending.set(false);
    this.failed.set(false);
    try {
      this.suppliers.set(await firstValueFrom(this.gateway.suppliers()));
    } catch (error) {
      this.suppliers.set([]);
      this.pending.set(isFeaturePending(error));
      this.failed.set(!isFeaturePending(error));
    } finally {
      this.loading.set(false);
    }
  }
}
