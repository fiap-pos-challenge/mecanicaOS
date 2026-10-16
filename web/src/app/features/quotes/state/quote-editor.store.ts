import { computed, inject, InjectionToken } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { firstValueFrom, Observable } from 'rxjs';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { AuthStore } from '../../auth/state/auth.store';
import { QuoteGateway } from '../data/quote.gateway';
import {
  isValidQuantity,
  priceChanges,
  priceQuote,
  sendBlocks,
  simulateInstallments,
  stockWarnings,
} from '../data/quote.math';
import {
  DiscountMode,
  ManualDiscount,
  PaymentPlan,
  Quote,
  QuoteDelivery,
  QuoteDraft,
  QuoteItemSource,
  QuoteLine,
  QuoteOption,
  QuoteOptions,
  QuoteVersion,
  SendQuoteInput,
  SendQuoteResult,
} from '../data/quote.models';

export const QUOTE_AUTOSAVE_MS = new InjectionToken<number>('QUOTE_AUTOSAVE_MS', {
  providedIn: 'root',
  factory: () => 1200,
});

export const QUOTE_DELIVERY_POLL_MS = new InjectionToken<number>('QUOTE_DELIVERY_POLL_MS', {
  providedIn: 'root',
  factory: () => 4000,
});

export const MAX_DELIVERY_POLLS = 15;

export type EditorLoad = 'loading' | 'ready' | 'pending' | 'notFound' | 'error';

export type SaveState = 'idle' | 'dirty' | 'saving' | 'saved' | 'error';

interface QuoteEditorState {
  readonly loadState: EditorLoad;
  readonly quote: Quote | null;
  readonly options: QuoteOptions | null;
  readonly draft: QuoteDraft | null;
  readonly revision: number;
  readonly saveState: SaveState;
  readonly savedAt: string | null;
  readonly busy: boolean;
  readonly actionError: string | null;
  readonly versions: readonly QuoteVersion[];
}

const initialState: QuoteEditorState = {
  loadState: 'loading',
  quote: null,
  options: null,
  draft: null,
  revision: 0,
  saveState: 'idle',
  savedAt: null,
  busy: false,
  actionError: null,
  versions: [],
};

let nextLine = 0;

export function isDeliveryExpired(delivery: QuoteDelivery, now = Date.now()): boolean {
  return Date.parse(delivery.expiresAt) <= now;
}

export function isQuoteEditable(quote: Quote, now = Date.now()): boolean {
  const delivery = quote.delivery;
  return (
    quote.status === 'Pending' &&
    (delivery === null || delivery.status === 'Failed' || isDeliveryExpired(delivery, now))
  );
}

function draftOf(quote: Quote): QuoteDraft {
  return {
    lines: quote.lines,
    discountMode: quote.discountMode,
    manualDiscount: quote.manualDiscount,
    payment: quote.payment,
  };
}

function errorText(error: unknown): string {
  return isApiError(error) ? error.detail : messages.errors.unknown;
}

export const QuoteEditorStore = signalStore(
  withState(initialState),
  withComputed((store, auth = inject(AuthStore)) => {
    const canManage = computed(() => auth.can('quotes.manage'));
    const editable = computed(() => {
      const quote = store.quote();
      return quote !== null && canManage() && isQuoteEditable(quote);
    });
    const catalog = computed(() => {
      const options = store.options();
      return options ? [...options.packages, ...options.services, ...options.parts] : [];
    });
    const pricing = computed(() => {
      const draft = store.draft();
      const quote = store.quote();
      const options = store.options();
      return priceQuote({
        lines: draft?.lines ?? [],
        rules: options?.rules ?? [],
        facts: quote?.customer.facts ?? { loyal: false, firstService: false, fleet: false },
        mode: draft?.discountMode ?? 'None',
        manual: draft?.manualDiscount ?? null,
        limitBasisPoints: options?.discountLimitBasisPoints ?? 0,
      });
    });
    return {
      canManage,
      canApproveDiscount: computed(() => auth.can('quotes.approveDiscount')),
      editable,
      pricing,
      blocks: computed(() =>
        sendBlocks({
          editable: editable(),
          lines: store.draft()?.lines ?? [],
          mode: store.draft()?.discountMode ?? 'None',
          manual: store.draft()?.manualDiscount ?? null,
          approval: store.quote()?.approval ?? null,
          pricing: pricing(),
        }),
      ),
      stockWarnings: computed(() =>
        stockWarnings(store.draft()?.lines ?? [], store.options()?.parts ?? []),
      ),
      priceChanges: computed(() => priceChanges(store.draft()?.lines ?? [], catalog())),
      installments: computed(() => {
        const options = store.options();
        const plan = store.draft()?.payment;
        if (!options || !plan?.methods.includes('Credit')) {
          return [];
        }

        return simulateInstallments(pricing().total, {
          ...options.payment.credit,
          maxInstallments: Math.min(plan.maxInstallments, options.payment.credit.maxInstallments),
        });
      }),
    };
  }),
  withMethods((store) => {
    const gateway = inject(QuoteGateway);
    const autosaveMs = inject(QUOTE_AUTOSAVE_MS);
    const pollMs = inject(QUOTE_DELIVERY_POLL_MS);
    let saveTimer: ReturnType<typeof setTimeout> | null = null;
    let pollTimer: ReturnType<typeof setTimeout> | null = null;
    let polls = 0;

    const clearSave = (): void => {
      if (saveTimer !== null) {
        clearTimeout(saveTimer);
        saveTimer = null;
      }
    };

    const clearPoll = (): void => {
      if (pollTimer !== null) {
        clearTimeout(pollTimer);
        pollTimer = null;
      }
    };

    const save = async (): Promise<boolean> => {
      clearSave();
      const quote = store.quote();
      const draft = store.draft();
      if (!quote || !draft || !store.editable() || store.saveState() === 'saving') {
        return store.saveState() !== 'error';
      }

      const revision = store.revision();
      patchState(store, { saveState: 'saving' });
      try {
        const saved = await firstValueFrom(gateway.saveDraft(quote.id, draft));
        const current = store.revision() === revision;
        patchState(store, {
          quote: saved,
          draft: current ? draftOf(saved) : store.draft(),
          saveState: current ? 'saved' : 'dirty',
          savedAt: saved.updatedAt,
          actionError: null,
        });
        if (!current) {
          schedule();
        }

        return true;
      } catch (error) {
        patchState(store, { saveState: 'error', actionError: errorText(error) });
        return false;
      }
    };

    const schedule = (): void => {
      clearSave();
      saveTimer = setTimeout(() => void save(), autosaveMs);
    };

    const change = (update: (draft: QuoteDraft) => QuoteDraft): void => {
      const draft = store.draft();
      if (!draft || !store.editable()) {
        return;
      }

      patchState(store, {
        draft: update(draft),
        revision: store.revision() + 1,
        saveState: 'dirty',
      });
      schedule();
    };

    const updateLines = (update: (lines: readonly QuoteLine[]) => readonly QuoteLine[]): void =>
      change((draft) => ({ ...draft, lines: update(draft.lines) }));

    const run = async <T>(source: () => Observable<T>): Promise<T | null> => {
      patchState(store, { busy: true, actionError: null });
      try {
        return await firstValueFrom(source());
      } catch (error) {
        patchState(store, { actionError: errorText(error) });
        return null;
      } finally {
        patchState(store, { busy: false });
      }
    };

    const setDelivery = (delivery: QuoteDelivery): void => {
      const quote = store.quote();
      if (quote) {
        patchState(store, { quote: { ...quote, delivery } });
      }
    };

    const poll = (): void => {
      clearPoll();
      const delivery = store.quote()?.delivery;
      if (
        !delivery ||
        polls >= MAX_DELIVERY_POLLS ||
        delivery.status === 'Opened' ||
        delivery.status === 'Failed' ||
        isDeliveryExpired(delivery)
      ) {
        return;
      }

      pollTimer = setTimeout(() => {
        polls += 1;
        void refreshDelivery().then(poll);
      }, pollMs);
    };

    const refreshDelivery = async (): Promise<void> => {
      const quote = store.quote();
      if (!quote?.delivery) {
        return;
      }

      try {
        setDelivery(await firstValueFrom(gateway.delivery(quote.id)));
      } catch (error) {
        patchState(store, { actionError: errorText(error) });
      }
    };

    const replaceQuote = (quote: Quote): void => {
      clearSave();
      patchState(store, {
        quote,
        draft: draftOf(quote),
        revision: store.revision() + 1,
        saveState: 'idle',
      });
    };

    return {
      async load(id: string): Promise<void> {
        clearSave();
        clearPoll();
        patchState(store, { ...initialState });
        try {
          const [quote, options] = await Promise.all([
            firstValueFrom(gateway.get(id)),
            firstValueFrom(gateway.options(id)),
          ]);
          patchState(store, {
            loadState: 'ready',
            quote,
            options,
            draft: draftOf(quote),
            savedAt: quote.updatedAt,
          });
          polls = 0;
          poll();
        } catch (error) {
          patchState(store, {
            loadState: isFeaturePending(error)
              ? 'pending'
              : isApiError(error) && error.status === 404
                ? 'notFound'
                : 'error',
          });
        }
      },

      addOption(option: QuoteOption, source: QuoteItemSource = 'Manual'): void {
        updateLines((lines) => {
          const existing = lines.find(
            (line) => line.kind === option.kind && line.itemId === option.itemId,
          );
          if (existing) {
            return lines.map((line) =>
              line === existing ? { ...line, quantity: line.quantity + 1 } : line,
            );
          }

          nextLine += 1;
          return [
            ...lines,
            {
              lineId: `new-${nextLine}`,
              kind: option.kind,
              itemId: option.itemId,
              name: option.name,
              unitPrice: option.unitPrice,
              quantity: 1,
              source,
              fractional: option.fractional,
            },
          ];
        });
      },

      setQuantity(lineId: string, quantity: number): void {
        updateLines((lines) =>
          lines.map((line) => (line.lineId === lineId ? { ...line, quantity } : line)),
        );
      },

      removeLine(lineId: string): void {
        updateLines((lines) => lines.filter((line) => line.lineId !== lineId));
      },

      applyCatalogPrices(): void {
        const changes = store.priceChanges();
        updateLines((lines) =>
          lines.map((line) => {
            const change = changes.find((item) => item.lineId === line.lineId);
            return change ? { ...line, unitPrice: change.after } : line;
          }),
        );
      },

      setDiscountMode(discountMode: DiscountMode): void {
        change((draft) => ({ ...draft, discountMode }));
      },

      setManualDiscount(manualDiscount: ManualDiscount | null): void {
        change((draft) => ({ ...draft, manualDiscount }));
      },

      setPayment(payment: PaymentPlan): void {
        change((draft) => ({ ...draft, payment }));
      },

      save,

      hasInvalidQuantity(): boolean {
        return (store.draft()?.lines ?? []).some(
          (line) => !isValidQuantity(line.quantity, line.fractional),
        );
      },

      async requestApproval(): Promise<boolean> {
        const quote = store.quote();
        const manual = store.draft()?.manualDiscount;
        if (!quote || !manual || !(await save())) {
          return false;
        }

        const approval = await run(() => gateway.requestDiscountApproval(quote.id, manual));
        if (approval) {
          patchState(store, { quote: { ...(store.quote() ?? quote), approval } });
        }

        return approval !== null;
      },

      async decideApproval(approve: boolean): Promise<boolean> {
        const quote = store.quote();
        const approval = quote?.approval;
        if (!quote || !approval) {
          return false;
        }

        const decided = await run(() =>
          gateway.decideDiscountApproval(quote.id, approval.id, approve),
        );
        if (decided) {
          patchState(store, { quote: { ...(store.quote() ?? quote), approval: decided } });
        }

        return decided !== null;
      },

      async messageDraft(channel: SendQuoteInput['channel']): Promise<string | null> {
        const quote = store.quote();
        return quote ? run(() => gateway.messageDraft(quote.id, channel)) : null;
      },

      async send(input: SendQuoteInput): Promise<SendQuoteResult | null> {
        const quote = store.quote();
        if (!quote || store.blocks().length > 0 || !(await save())) {
          return null;
        }

        const result = await run(() => gateway.send(quote.id, input));
        if (result) {
          setDelivery(result.delivery);
          polls = 0;
          poll();
        }

        return result;
      },

      refreshDelivery,

      async retryDelivery(): Promise<boolean> {
        const quote = store.quote();
        if (!quote) {
          return false;
        }

        const delivery = await run(() => gateway.retryDelivery(quote.id));
        if (delivery) {
          setDelivery(delivery);
          polls = 0;
          poll();
        }

        return delivery !== null;
      },

      async approve(): Promise<boolean> {
        const quote = store.quote();
        if (!quote || !(await save())) {
          return false;
        }

        const approved = await run(() => gateway.approve(quote.id));
        if (approved) {
          replaceQuote(approved);
        }

        return approved !== null;
      },

      async reject(reason: string): Promise<boolean> {
        const quote = store.quote();
        if (!quote) {
          return false;
        }

        clearSave();
        const rejected = await run(() => gateway.reject(quote.id, reason));
        if (rejected) {
          clearPoll();
          replaceQuote(rejected);
        }

        return rejected !== null;
      },

      async createVersion(): Promise<string | null> {
        const quote = store.quote();
        const next = quote ? await run(() => gateway.createVersion(quote.id)) : null;
        return next?.id ?? null;
      },

      async loadVersions(): Promise<readonly QuoteVersion[]> {
        const quote = store.quote();
        const versions = quote ? await run(() => gateway.versions(quote.id)) : null;
        patchState(store, { versions: versions ?? [] });
        return versions ?? [];
      },

      dismissError(): void {
        patchState(store, { actionError: null });
      },

      stop(): void {
        clearSave();
        clearPoll();
      },
    };
  }),
  withHooks({
    onDestroy(store) {
      store.stop();
      if (store.saveState() === 'dirty') {
        void store.save();
      }
    },
  }),
);

export type QuoteEditorStore = InstanceType<typeof QuoteEditorStore>;
