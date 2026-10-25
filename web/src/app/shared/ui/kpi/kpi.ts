import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TrendingDown as TrendingDownIcon, TrendingUp as TrendingUpIcon } from 'lucide';
import { UiIcon } from '../icon/icon';
import { messages } from '../../../core/i18n/messages.pt-BR';

export type KpiTrend = 'up' | 'down' | 'neutral';

@Component({
  selector: 'ui-kpi',
  imports: [UiIcon],
  templateUrl: './kpi.html',
  styleUrl: './kpi.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiKpi {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly delta = input<string | null>(null);
  readonly trend = input<KpiTrend>('neutral');

  protected readonly text = messages.ui.kpi;
  protected readonly upIcon = TrendingUpIcon;
  protected readonly downIcon = TrendingDownIcon;
}
