import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { CATALOG_KINDS } from '../data/catalog.models';

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogPage {
  protected readonly screen = messages.screens.catalog;
  protected readonly text = messages.catalog;
  protected readonly kinds = CATALOG_KINDS;
}
