import { Cents } from '../../../shared/util/money';
import { PaymentMethod } from '../../quotes/data/quote.models';

export type DocumentKind = 'ServiceOrder' | 'Quote' | 'Receipt';

export interface DocumentLine {
  readonly name: string;
  readonly quantity: number;
  readonly unitPrice: Cents;
  readonly total: Cents;
}

export interface DocumentModel {
  readonly kind: DocumentKind;
  readonly code: string | null;
  readonly number: number;
  readonly version: number | null;
  readonly issuedAt: string;
  readonly shopName: string;
  readonly customerName: string;
  readonly vehicle: string;
  readonly lines: readonly DocumentLine[];
  readonly services: Cents;
  readonly parts: Cents;
  readonly discount: Cents;
  readonly total: Cents;
  readonly paidAt: string | null;
  readonly paymentMethods: readonly PaymentMethod[];
}

export interface DocumentDownload {
  readonly url: string;
  readonly fileName: string;
}

export interface DocumentVerification {
  readonly valid: boolean;
  readonly kind: DocumentKind | null;
  readonly number: number | null;
  readonly issuedAt: string | null;
  readonly total: Cents | null;
  readonly shopName: string | null;
}

export type DocumentChannel = 'WhatsApp' | 'Email';
