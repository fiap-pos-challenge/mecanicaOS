import { required } from '../../../core/data/contract-error';
import { centsFromDecimal } from '../../../shared/util/money';
import { ServiceOrderCardDto, ServiceOrderDetailDto } from './service-order.contracts';
import { ServiceOrderCard, ServiceOrderDetail } from './service-order.models';

export function toServiceOrderCard(dto: ServiceOrderCardDto): ServiceOrderCard {
  return {
    id: required(dto.id, 'id'),
    number: required(dto.number, 'number'),
    status: required(dto.status, 'status'),
    version: required(dto.version, 'version'),
    customerName: required(dto.customerName, 'customerName'),
    plate: required(dto.licensePlate, 'licensePlate'),
    vehicle: dto.vehicle ?? '',
    summary: dto.summary ?? '',
    mechanicName: dto.mechanicName ?? null,
    promisedAt: dto.promisedAt ?? null,
    total: centsFromDecimal(dto.total ?? 0),
    hasQuoteItems: dto.hasQuoteItems ?? false,
    tags: dto.tags ?? [],
    updatedAt: required(dto.updatedAt, 'updatedAt'),
  };
}

export function toServiceOrderDetail(dto: ServiceOrderDetailDto): ServiceOrderDetail {
  const quote = dto.quote ?? null;
  return {
    ...toServiceOrderCard(dto),
    problemDescription: dto.problemDescription ?? '',
    mileage: dto.mileage ?? 0,
    history: (dto.statusHistory ?? []).map((entry) => ({
      from: entry.previousStatus ?? null,
      to: required(entry.newStatus, 'statusHistory.newStatus'),
      changedBy: entry.changedBy ?? null,
      note: entry.note ?? null,
      changedAt: required(entry.changedAt, 'statusHistory.changedAt'),
    })),
    quote: quote
      ? {
          id: required(quote.id, 'quote.id'),
          number: required(quote.number, 'quote.number'),
          version: required(quote.version, 'quote.version'),
          status: required(quote.status, 'quote.status'),
          total: centsFromDecimal(quote.total ?? 0),
          items: (quote.items ?? []).map((item) => ({
            name: required(item.name, 'quote.items.name'),
            quantity: item.quantity ?? 1,
            total: centsFromDecimal(item.total ?? 0),
          })),
        }
      : null,
    parts: (dto.parts ?? []).map((part) => ({
      name: required(part.name, 'parts.name'),
      quantity: part.quantity ?? 0,
      unit: part.unit ?? 'un',
      reserved: part.reserved ?? false,
      leadTimeDays: part.leadTimeDays ?? null,
    })),
    photos: (dto.photos ?? []).map((photo) => ({
      id: required(photo.id, 'photos.id'),
      url: required(photo.url, 'photos.url'),
      caption: photo.caption ?? '',
      takenAt: required(photo.takenAt, 'photos.takenAt'),
    })),
    timeEntries: (dto.timeEntries ?? []).map((entry) => ({
      id: required(entry.id, 'timeEntries.id'),
      mechanicName: required(entry.mechanicName, 'timeEntries.mechanicName'),
      startedAt: required(entry.startedAt, 'timeEntries.startedAt'),
      endedAt: entry.endedAt ?? null,
      minutes: entry.minutes ?? 0,
    })),
    documents: (dto.documents ?? []).map((document) => ({
      kind: required(document.kind, 'documents.kind'),
      code: required(document.code, 'documents.code'),
      issuedAt: required(document.issuedAt, 'documents.issuedAt'),
    })),
  };
}
