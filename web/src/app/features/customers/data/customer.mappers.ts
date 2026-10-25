import type {
  AddressDto,
  CreateCustomerRequest,
  CustomerDto,
  CustomerDtoPagedResult,
  QueryCustomersRequest,
  UpdateCustomerRequest,
} from '../../../core/api/generated/index';
import { required } from '../../../core/data/contract-error';
import { Page } from '../../../core/data/page';
import { Address, Customer, CustomerInput, CustomerQuery, CustomerUpdate } from './customer.models';

export function toAddress(dto: AddressDto | undefined | null): Address | null {
  if (!dto) {
    return null;
  }

  return {
    zipCode: dto.zipCode ?? null,
    street: dto.street ?? null,
    number: dto.number ?? null,
    complement: dto.complement ?? null,
    district: dto.district ?? null,
    city: dto.city ?? null,
    state: dto.state ?? null,
  };
}

export function toCustomer(dto: CustomerDto): Customer {
  return {
    id: required(dto.id, 'id'),
    name: required(dto.name, 'name'),
    maskedDocument: required(dto.document, 'document'),
    documentType: required(dto.documentType, 'documentType'),
    maskedEmail: dto.email ?? null,
    maskedPhone: dto.phone ?? null,
    address: toAddress(dto.address),
    messagingConsent: {
      whatsApp: dto.messagingConsent?.whatsApp ?? false,
      email: dto.messagingConsent?.email ?? false,
      updatedAt: dto.messagingConsent?.updatedAt ?? null,
    },
    createdAt: required(dto.createdAt, 'createdAt'),
    updatedAt: dto.updatedAt ?? null,
  };
}

export function toCustomerPage(dto: CustomerDtoPagedResult): Page<Customer> {
  return {
    items: (dto.items ?? []).map(toCustomer),
    page: required(dto.page, 'page'),
    pageSize: required(dto.pageSize, 'pageSize'),
    totalItems: required(dto.totalItems, 'totalItems'),
  };
}

export function toUpdateCustomerRequest(input: CustomerUpdate): UpdateCustomerRequest {
  return {
    name: input.name,
    email: input.email,
    phone: input.phone,
    messagingConsent: {
      whatsApp: input.messagingConsent.whatsApp,
      email: input.messagingConsent.email,
    },
    ...(input.address ? { address: input.address } : {}),
  };
}

export function toCreateCustomerRequest(input: CustomerInput): CreateCustomerRequest {
  return { ...toUpdateCustomerRequest(input), document: input.document };
}

export function toQueryCustomersRequest(query: CustomerQuery): QueryCustomersRequest {
  return {
    term: query.term.trim() || null,
    sort: query.sort,
    direction: query.direction,
    page: query.page,
    pageSize: query.pageSize,
    ...(query.documentType ? { documentType: query.documentType } : {}),
  };
}
