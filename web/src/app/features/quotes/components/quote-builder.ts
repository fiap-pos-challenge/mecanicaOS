import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { Plus as PlusIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiCard } from '../../../shared/ui/card/card';
import { UiIcon } from '../../../shared/ui/icon/icon';
import { UiIconButton } from '../../../shared/ui/icon-button/icon-button';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiStatusPill } from '../../../shared/ui/status-pill/status-pill';
import { TabItem, UiTabs } from '../../../shared/ui/tabs/tabs';
import { formatBrl } from '../../../shared/util/money';
import { AiSuggestion, QuoteItemSource, QuoteOption, QuoteOptions } from '../data/quote.models';
import { optionDetail, stockLabel } from '../quote-display';
import { UiAiBadge } from '../../../shared/ui/ai-badge/ai-badge';

export type BuilderSection = 'packages' | 'services' | 'parts' | 'suggestions';

export interface BuilderAdd {
  readonly option: QuoteOption;
  readonly source: QuoteItemSource;
}

interface BuilderRow {
  readonly option: QuoteOption;
  readonly detail: string | null;
  readonly stock: string | null;
  readonly outOfStock: boolean;
  readonly reason: string | null;
  readonly price: string;
}

function normalize(value: string): string {
  return value.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

@Component({
  selector: 'app-quote-builder',
  imports: [UiAiBadge, UiCard, UiControl, UiField, UiIcon, UiIconButton, UiStatusPill, UiTabs],
  templateUrl: './quote-builder.html',
  styleUrl: './quote-builder.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuoteBuilder {
  readonly options = input.required<QuoteOptions>();
  readonly editable = input(true);
  readonly added = output<BuilderAdd>();

  protected readonly text = messages.quotes.builder;
  protected readonly icons = { plus: PlusIcon };
  protected readonly section = signal<BuilderSection>('packages');
  protected readonly term = signal('');
  protected readonly tabs: readonly TabItem[] = (
    ['packages', 'services', 'parts', 'suggestions'] as const
  ).map((id) => ({ id, label: this.text.sections[id] }));
  protected readonly rows = computed<readonly BuilderRow[]>(() => {
    const options = this.options();
    const section = this.section();
    const items: readonly (QuoteOption | AiSuggestion)[] = options[section];
    const term = normalize(this.term().trim());
    return items
      .filter((item) => term === '' || normalize(item.name).includes(term))
      .map((option) => ({
        option,
        detail: optionDetail(option),
        stock: stockLabel(option),
        outOfStock: option.stock !== null && option.stock.available <= 0,
        reason: 'reason' in option ? option.reason : null,
        price: formatBrl(option.unitPrice),
      }));
  });

  protected selectSection(id: string): void {
    this.section.set(id as BuilderSection);
  }

  protected search(event: Event): void {
    this.term.set((event.target as HTMLInputElement).value);
  }

  protected add(row: BuilderRow): void {
    this.added.emit({
      option: row.option,
      source: this.section() === 'suggestions' ? 'Ai' : 'Manual',
    });
  }
}
