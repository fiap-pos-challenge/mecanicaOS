import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Trash2 as TrashIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiIconButton } from '../../../shared/ui/icon-button/icon-button';
import { formatBrl } from '../../../shared/util/money';
import { isValidQuantity, lineTotal, PriceChange, StockWarning } from '../data/quote.math';
import { QuoteLine } from '../data/quote.models';
import { formatQuantity, parseDecimal } from '../quote-display';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';

export interface QuantityChange {
  readonly lineId: string;
  readonly quantity: number;
}

@Component({
  selector: 'app-quote-items',
  imports: [UiAiBadge, UiButton, UiCard, UiIcon, UiIconButton],
  templateUrl: './quote-items.html',
  styleUrl: './quote-items.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuoteItems {
  readonly lines = input.required<readonly QuoteLine[]>();
  readonly editable = input(true);
  readonly stockWarnings = input<readonly StockWarning[]>([]);
  readonly priceChanges = input<readonly PriceChange[]>([]);
  readonly quantityChange = output<QuantityChange>();
  readonly removed = output<string>();
  readonly pricesUpdated = output();

  protected readonly text = messages.quotes.items;
  protected readonly icons = { trash: TrashIcon };
  protected readonly rows = computed(() =>
    this.lines().map((line) => ({
      line,
      quantity: formatQuantity(line.quantity),
      unit: this.text.unit(formatBrl(line.unitPrice)),
      total: formatBrl(lineTotal(line)),
      invalid: !isValidQuantity(line.quantity, line.fractional),
    })),
  );
  protected readonly warnings = computed(() =>
    this.stockWarnings().map((warning) =>
      this.text.stockWarning(
        warning.name,
        formatQuantity(warning.missing),
        warning.leadTimeDays,
        warning.supplier,
      ),
    ),
  );
  protected readonly changes = computed(() =>
    this.priceChanges().map((change) =>
      this.text.priceChange(change.name, formatBrl(change.before), formatBrl(change.after)),
    ),
  );

  protected setQuantity(lineId: string, event: Event): void {
    const value = parseDecimal((event.target as HTMLInputElement).value);
    this.quantityChange.emit({ lineId, quantity: value ?? 0 });
  }
}
