import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { documentKindOf } from '../../../app/shared/util/document';
import { onlyDigits } from '../../../app/shared/util/digits';
import { isValidEmail, normalizeEmail } from '../../../app/shared/util/email';
import { maskDocument, maskEmail, maskPhone } from '../../../app/shared/util/mask';
import { isValidPhone } from '../../../app/shared/util/phone';
import { FakeAddress, FakeCustomerRecord } from '../fake-api/fake-database';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  created,
  FakeFieldError,
  fieldError,
  FakeRouter,
  noContent,
  ok,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, isPrincipal } from '../fake-api/fake-session';

export const CUSTOMER_PATHS = {
  collection: '/api/admin/customers',
  query: '/api/admin/customers/query',
  item: '/api/admin/customers/:id',
  reveal: '/api/admin/customers/:id/reveal',
  search: '/api/admin/customers/search',
} as const;

const text = messages.fake;
const staff = ['Admin', 'Manager', 'Attendant'] as const;

function notFound(): ReturnType<typeof problem> {
  return problem(404, 'customer.notFound', text.customerNotFound);
}

export function toCustomerResponse(customer: FakeCustomerRecord): Record<string, unknown> {
  return {
    id: customer.id,
    name: customer.name,
    document: maskDocument(customer.document),
    documentType: customer.documentType,
    email: customer.email ? maskEmail(customer.email) : null,
    phone: customer.phone ? maskPhone(customer.phone) : null,
    address: customer.address,
    messagingConsent: {
      whatsApp: customer.consentWhatsApp,
      email: customer.consentEmail,
      updatedAt: customer.consentUpdatedAt,
    },
    createdAt: customer.createdAt,
    updatedAt: customer.updatedAt,
  };
}

function readAddress(value: unknown): FakeAddress | null {
  if (typeof value !== 'object' || value === null) {
    return null;
  }

  const body = value as Record<string, unknown>;
  const field = (key: string): string | null => readText(body, key) || null;
  return {
    zipCode: field('zipCode'),
    street: field('street'),
    number: field('number'),
    complement: field('complement'),
    district: field('district'),
    city: field('city'),
    state: field('state'),
  };
}

function readConsent(value: unknown): { whatsApp: boolean; email: boolean } | null {
  if (typeof value !== 'object' || value === null) {
    return null;
  }

  const body = value as Record<string, unknown>;
  return { whatsApp: body['whatsApp'] === true, email: body['email'] === true };
}

function contactErrors(email: string, phone: string): FakeFieldError[] {
  return [
    ...(email && !isValidEmail(email)
      ? [fieldError('email', 'common.invalidEmail', text.invalidField)]
      : []),
    ...(isValidPhone(phone)
      ? []
      : [fieldError('phone', 'customer.invalidPhone', text.invalidField)]),
  ];
}

function applyConsent(
  customer: FakeCustomerRecord,
  consent: { whatsApp: boolean; email: boolean },
): ReturnType<typeof problem> | null {
  if (consent.email && !customer.email) {
    return problem(400, 'customer.emailConsentWithoutEmail', text.emailConsentWithoutEmail);
  }

  if (consent.whatsApp !== customer.consentWhatsApp || consent.email !== customer.consentEmail) {
    customer.consentWhatsApp = consent.whatsApp;
    customer.consentEmail = consent.email;
    customer.consentUpdatedAt = fakeNow();
  }

  return null;
}

function page(items: readonly FakeCustomerRecord[], pageNumber: number, pageSize: number) {
  return ok({
    items: items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize).map(toCustomerResponse),
    page: pageNumber,
    pageSize,
    totalItems: items.length,
  });
}

export function createCustomersFakeApi(state: FakeBackendState): FakeFeatureApi {
  const customers = (): FakeCustomerRecord[] => state.database.customers;
  const users = (): FakeBackendState['database']['users'] => state.database.users;
  const find = (id: string): FakeCustomerRecord | undefined =>
    customers().find((customer) => customer.id === id);

  const router = new FakeRouter()
    .on('POST', CUSTOMER_PATHS.search, (request) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const document = onlyDigits(readText(readBody(request), 'document'));
      if (!document) {
        return validation([fieldError('document', 'common.required', text.required)]);
      }

      const customer = customers().find((candidate) => candidate.document === document);
      return customer ? ok(toCustomerResponse(customer)) : notFound();
    })
    .on('POST', CUSTOMER_PATHS.query, (request) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const pageNumber = typeof body['page'] === 'number' ? body['page'] : 1;
      const pageSize = typeof body['pageSize'] === 'number' ? body['pageSize'] : 20;
      if (pageNumber < 1 || pageSize < 1 || pageSize > 100) {
        return validation([fieldError('pageSize', 'common.invalidPagination', text.invalidField)]);
      }

      const term = readText(body, 'term');
      const document = term && documentKindOf(term) ? onlyDigits(term) : null;
      const documentType = body['documentType'];
      const direction = body['direction'] === 'Desc' ? -1 : 1;
      const byCreatedAt = body['sort'] === 'CreatedAt';
      const items = customers()
        .filter((customer) =>
          document
            ? customer.document === document
            : !term || customer.name.toLowerCase().includes(term.toLowerCase()),
        )
        .filter((customer) => !documentType || customer.documentType === documentType)
        .sort(
          (a, b) =>
            direction *
            (byCreatedAt ? a.createdAt.localeCompare(b.createdAt) : a.name.localeCompare(b.name)),
        );
      return page(items, pageNumber, pageSize);
    })
    .on('GET', CUSTOMER_PATHS.collection, (request) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const pageNumber = Number(request.query.get('page') ?? '1');
      const pageSize = Number(request.query.get('pageSize') ?? '20');
      if (!Number.isInteger(pageNumber) || pageNumber < 1 || pageSize < 1 || pageSize > 100) {
        return validation([fieldError('pageSize', 'common.invalidPagination', text.invalidField)]);
      }

      return page(
        [...customers()].sort((a, b) => a.name.localeCompare(b.name)),
        pageNumber,
        pageSize,
      );
    })
    .on('POST', CUSTOMER_PATHS.collection, (request) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const body = readBody(request);
      const name = readText(body, 'name');
      const rawDocument = readText(body, 'document');
      const email = readText(body, 'email');
      const phone = readText(body, 'phone');
      const documentType = documentKindOf(rawDocument);
      const errors = [
        ...(name ? [] : [fieldError('name', 'common.required', text.required)]),
        ...(documentType
          ? []
          : [fieldError('document', 'customer.invalidDocument', text.invalidField)]),
        ...contactErrors(email, phone),
      ];
      if (errors.length > 0 || !documentType) {
        return validation(errors);
      }

      const document = onlyDigits(rawDocument);
      if (customers().some((customer) => customer.document === document)) {
        return problem(409, 'customer.duplicateDocument', text.documentInUse);
      }

      const customer: FakeCustomerRecord = {
        id: fakeId(),
        name,
        document,
        documentType,
        email: email ? normalizeEmail(email) : null,
        phone: onlyDigits(phone),
        address: readAddress(body['address']),
        consentWhatsApp: false,
        consentEmail: false,
        consentUpdatedAt: null,
        createdAt: fakeNow(),
        updatedAt: null,
      };
      const consentError = applyConsent(
        customer,
        readConsent(body['messagingConsent']) ?? { whatsApp: false, email: false },
      );
      if (consentError) {
        return consentError;
      }

      customers().push(customer);
      return created(toCustomerResponse(customer));
    })
    .on('POST', CUSTOMER_PATHS.reveal, (request, params) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const customer = find(params['id'] ?? '');
      if (!customer) {
        return notFound();
      }

      const body = readBody(request);
      const field = body['field'];
      const reason = body['reason'];
      if (
        (field !== 'Document' && field !== 'Email' && field !== 'Phone') ||
        (reason !== 'View' && reason !== 'Edit')
      ) {
        return validation([fieldError('field', 'common.validationFailed', text.invalidField)]);
      }

      state.database.sensitiveAccess.push({
        userId: principal.id,
        customerId: customer.id,
        field,
        reason,
        occurredAt: fakeNow(),
      });
      const value =
        field === 'Document'
          ? customer.document
          : field === 'Email'
            ? customer.email
            : customer.phone;
      return ok({ field, value });
    })
    .on('GET', CUSTOMER_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const customer = find(params['id'] ?? '');
      return customer ? ok(toCustomerResponse(customer)) : notFound();
    })
    .on('PUT', CUSTOMER_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const customer = find(params['id'] ?? '');
      if (!customer) {
        return notFound();
      }

      const body = readBody(request);
      const name = readText(body, 'name');
      const email = readText(body, 'email');
      const phone = readText(body, 'phone');
      const errors = [
        ...(name ? [] : [fieldError('name', 'common.required', text.required)]),
        ...contactErrors(email, phone),
      ];
      if (errors.length > 0) {
        return validation(errors);
      }

      const next = { ...customer, email: email ? normalizeEmail(email) : null };
      const consent = readConsent(body['messagingConsent']) ?? {
        whatsApp: customer.consentWhatsApp,
        email: customer.consentEmail && next.email !== null,
      };
      const consentError = applyConsent(next, consent);
      if (consentError) {
        return consentError;
      }

      Object.assign(customer, next, {
        name,
        phone: onlyDigits(phone),
        address: readAddress(body['address']),
        updatedAt: fakeNow(),
      });
      return ok(toCustomerResponse(customer));
    })
    .on('DELETE', CUSTOMER_PATHS.item, (request, params) => {
      const principal = authorize(request, users(), ['Admin']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const index = customers().findIndex((customer) => customer.id === params['id']);
      if (index < 0) {
        return notFound();
      }

      if (state.database.vehicles.some((vehicle) => vehicle.customerId === params['id'])) {
        return problem(409, 'customer.hasActiveVehicles', text.customerHasVehicles);
      }

      customers().splice(index, 1);
      return noContent();
    });

  return new FakeFeatureApi('customers', router, state.scenarios);
}
