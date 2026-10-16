import { DocumentKind } from '../../../shared/util/document';

export interface Address {
  readonly zipCode: string | null;
  readonly street: string | null;
  readonly number: string | null;
  readonly complement: string | null;
  readonly district: string | null;
  readonly city: string | null;
  readonly state: string | null;
}

export interface MessagingConsent {
  readonly whatsApp: boolean;
  readonly email: boolean;
  readonly updatedAt: string | null;
}

export interface Customer {
  readonly id: string;
  readonly name: string;
  readonly maskedDocument: string;
  readonly documentType: DocumentKind;
  readonly maskedEmail: string | null;
  readonly maskedPhone: string | null;
  readonly address: Address | null;
  readonly messagingConsent: MessagingConsent;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface ConsentInput {
  readonly whatsApp: boolean;
  readonly email: boolean;
}

export interface CustomerInput {
  readonly name: string;
  readonly document: string;
  readonly email: string | null;
  readonly phone: string | null;
  readonly address: Address | null;
  readonly messagingConsent: ConsentInput;
}

export type CustomerUpdate = Omit<CustomerInput, 'document'>;

export type CustomerSort = 'Name' | 'CreatedAt';

export type SortDirection = 'Asc' | 'Desc';

export interface CustomerQuery {
  readonly term: string;
  readonly documentType: DocumentKind | null;
  readonly sort: CustomerSort;
  readonly direction: SortDirection;
  readonly page: number;
  readonly pageSize: number;
}

export type SensitiveField = 'Document' | 'Email' | 'Phone';

export type RevealReason = 'View' | 'Edit';
