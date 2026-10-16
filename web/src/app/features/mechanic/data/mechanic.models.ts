import { OrderPhoto, ServiceOrderStatus } from '../../service-orders/data/service-order.models';

export type ChecklistState = 'Ok' | 'Attention' | 'Replace';

export const CHECKLIST_STATES: readonly ChecklistState[] = ['Ok', 'Attention', 'Replace'];

export interface ChecklistItem {
  readonly id: string;
  readonly label: string;
  readonly state: ChecklistState | null;
}

export interface AiSuggestedItem {
  readonly kind: 'Service' | 'Part';
  readonly itemId: string;
  readonly name: string;
  readonly confidence: number;
  readonly reason: string;
}

export interface WorkPhoto extends OrderPhoto {
  readonly pending: boolean;
}

export interface MechanicOrder {
  readonly id: string;
  readonly number: number;
  readonly status: ServiceOrderStatus;
  readonly vehicle: string;
  readonly plate: string;
  readonly complaint: string;
  readonly promisedAt: string | null;
  readonly checklist: readonly ChecklistItem[];
  readonly photos: readonly WorkPhoto[];
  readonly diagnosis: string;
  readonly sentToQuote: boolean;
  readonly timerRunning: boolean;
}

export interface PhotoUpload {
  readonly id: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly dataUrl: string;
}

export interface QuoteRequest {
  readonly description: string;
  readonly items: readonly Pick<AiSuggestedItem, 'kind' | 'itemId'>[];
}

export type WorkAction =
  | { readonly kind: 'checklist'; readonly items: readonly ChecklistItem[] }
  | { readonly kind: 'photo'; readonly photo: PhotoUpload }
  | { readonly kind: 'diagnosis'; readonly description: string }
  | { readonly kind: 'sendToQuote'; readonly request: QuoteRequest }
  | { readonly kind: 'start' }
  | { readonly kind: 'finish' };

export interface QueuedAction {
  readonly id: string;
  readonly orderId: string;
  readonly orderNumber: number;
  readonly action: WorkAction;
  readonly createdAt: string;
}
