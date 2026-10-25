import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { messages } from '../../../core/i18n/messages.pt-BR';

export const INVENTORY_SECTIONS = [
  { path: '/inventory', key: 'overview', exact: true },
  { path: '/inventory/counts', key: 'counts', exact: false },
  { path: '/inventory/purchase-orders', key: 'orders', exact: false },
  { path: '/inventory/suppliers', key: 'suppliers', exact: false },
] as const;

@Component({
  selector: 'app-inventory-page',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './inventory-page.html',
  styleUrl: './inventory-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryPage {
  protected readonly screen = messages.screens.inventory;
  protected readonly text = messages.inventory;
  protected readonly sections = INVENTORY_SECTIONS;
}
