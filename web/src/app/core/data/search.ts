import { PageRequest } from './page';

export type PersonalDataField = 'document' | 'licensePlate' | 'email' | 'phone';

export const PERSONAL_DATA_FIELDS: readonly PersonalDataField[] = [
  'document',
  'licensePlate',
  'email',
  'phone',
];

export type PersonalDataSearch<TField extends PersonalDataField> = Readonly<Record<TField, string>>;

export interface TermSearch extends PageRequest {
  readonly term: string;
}
