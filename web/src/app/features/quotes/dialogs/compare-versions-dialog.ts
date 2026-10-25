import { DIALOG_DATA } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { UiSkeleton } from '../../../shared/ui/skeleton/skeleton';
import { cents, formatBrl } from '../../../shared/util/money';
import { compareVersions, LineChange } from '../data/quote.math';
import { PricedLine, QuoteVersion } from '../data/quote.models';
import { formatQuantity } from '../quote-display';
import { QuoteEditorStore } from '../state/quote-editor.store';

const text = messages.quotes.versions;

export function describeChange(change: LineChange): {
  readonly kind: 'added' | 'removed' | 'changed';
  readonly before: string;
  readonly after: string;
} {
  const describe = (line: PricedLine | null): string =>
    line ? text.line(formatQuantity(line.quantity), formatBrl(line.unitPrice)) : '—';
  return {
    kind: change.before === null ? 'added' : change.after === null ? 'removed' : 'changed',
    before: describe(change.before),
    after: describe(change.after),
  };
}

@Component({
  selector: 'app-compare-versions-dialog',
  imports: [UiControl, UiDialogLayout, UiField, UiSkeleton],
  templateUrl: './compare-versions-dialog.html',
  styleUrl: './quote-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompareVersionsDialog {
  private readonly store = inject<QuoteEditorStore>(DIALOG_DATA);

  protected readonly text = text;
  protected readonly statuses = messages.quotes.statuses;
  protected readonly loading = signal(true);
  protected readonly versions = signal<readonly QuoteVersion[]>([]);
  protected readonly fromId = signal<string | null>(null);
  protected readonly toId = signal<string | null>(null);
  protected readonly comparison = computed(() => {
    const versions = this.versions();
    const from = versions.find((version) => version.id === this.fromId());
    const to = versions.find((version) => version.id === this.toId());
    if (!from || !to) {
      return null;
    }

    const result = compareVersions(from, to);
    const delta = result.totalDelta;
    return {
      rows: result.changes.map((change) => ({ name: change.name, ...describeChange(change) })),
      delta: text.delta(
        `${delta > 0 ? '+' : delta < 0 ? '−' : ''}${formatBrl(cents(Math.abs(delta)))}`,
      ),
    };
  });
  protected readonly reasons = computed(() =>
    this.versions().filter((version) => version.rejectionReason !== null),
  );

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    const versions = await this.store.loadVersions();
    this.versions.set(versions);
    this.toId.set(versions[0]?.id ?? null);
    this.fromId.set(versions[1]?.id ?? null);
    this.loading.set(false);
  }

  protected label(version: QuoteVersion): string {
    return text.option(version.version, this.statuses[version.status]);
  }

  protected pick(target: 'from' | 'to', event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    (target === 'from' ? this.fromId : this.toId).set(id);
  }
}
