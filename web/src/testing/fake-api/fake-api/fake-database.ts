import { createCatalogSeed, FakeCatalog } from '../catalog/catalog.seed';
import { createSettingsSeed, FakeSettings } from '../settings/settings.fake-api';
import { createAssistantSeed, FakeAssistant } from '../assistant/assistant.fake-api';
import {
  createNotificationsSeed,
  FakeNotifications,
} from '../notifications/notifications.fake-api';
import { createAutomationsSeed, FakeAutomations } from '../automations/automations.fake-api';
import { createReportsSeed, FakeReports } from '../reports/reports.fake-api';
import { createDashboardSeed, FakeDashboard } from '../dashboard/dashboard.fake-api';
import { createInventorySeed, FakeInventory } from '../inventory/inventory.fake-api';
import { createPortalSeed, FakePortal } from '../portal/portal.fake-api';
import { createQuotesSeed, FakeQuotes } from '../quotes/quotes.seed';
import { createServiceOrdersSeed, FakeServiceOrders } from '../service-orders/service-orders.seed';
import { createFakeAuthState, FAKE_TWO_FACTOR_EMAIL, FakeAuthState } from './fake-auth-state';
import { fakeId } from './fake-ids';
import { FakeUserRecord } from './fake-session';

export interface FakeAddress {
  zipCode: string | null;
  street: string | null;
  number: string | null;
  complement: string | null;
  district: string | null;
  city: string | null;
  state: string | null;
}

export interface FakeCustomerRecord {
  id: string;
  name: string;
  document: string;
  documentType: 'Cpf' | 'Cnpj';
  email: string | null;
  phone: string | null;
  address: FakeAddress | null;
  consentWhatsApp: boolean;
  consentEmail: boolean;
  consentUpdatedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface FakeVehicleRecord {
  id: string;
  customerId: string;
  licensePlate: string;
  brand: string;
  model: string;
  year: number;
  mileage: number;
  createdAt: string;
  updatedAt: string | null;
}

export type FakeJobKind = 'CustomerDataExport' | 'CustomerImport';

export type FakeJobStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed';

export interface FakeImportError {
  readonly line: number;
  readonly field: string;
  readonly message: string;
}

export interface FakeJobRecord {
  id: string;
  kind: FakeJobKind;
  status: FakeJobStatus;
  polls: number;
  createdAt: string;
  completedAt: string | null;
  fileName: string | null;
  content: string | null;
  totalLines: number | null;
  validLines: number | null;
  errors: FakeImportError[];
}

export interface FakeSensitiveAccess {
  readonly userId: string;
  readonly customerId: string;
  readonly field: string;
  readonly reason: string;
  readonly occurredAt: string;
}

export interface FakeInvitationRecord {
  token: string;
  userId: string;
  expiresAt: string;
  acceptedAt: string | null;
  revokedAt: string | null;
}

export interface FakeShift {
  day: FakeWeekDay;
  start: string;
  end: string;
}

export type FakeWeekDay =
  'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';

export interface FakeTeamProfile {
  userId: string;
  specialties: string[];
  commissionPercent: number;
  discountLimitPercent: number;
  schedule: FakeShift[];
  updatedAt: string | null;
}

export interface FakeDatabase {
  users: FakeUserRecord[];
  invitations: FakeInvitationRecord[];
  teamProfiles: FakeTeamProfile[];
  catalog: FakeCatalog;
  inventory: FakeInventory;
  quotes: FakeQuotes;
  serviceOrders: FakeServiceOrders;
  portal: FakePortal;
  settings: FakeSettings;
  assistant: FakeAssistant;
  notifications: FakeNotifications;
  automations: FakeAutomations;
  reports: FakeReports;
  dashboard: FakeDashboard;
  customers: FakeCustomerRecord[];
  vehicles: FakeVehicleRecord[];
  jobs: FakeJobRecord[];
  sensitiveAccess: FakeSensitiveAccess[];
  auth: FakeAuthState;
}

const seededAt = '2026-09-01T12:00:00Z';

export const FAKE_ADMIN_EMAIL = 'admin@oficina.local';
export const FAKE_ADMIN_PASSWORD = 'Admin@123';
export const FAKE_INVITATION_TOKEN = 'convite-demo';
export const FAKE_INVITATION_LIFETIME_MS = 72 * 60 * 60 * 1000;

const weekdays: readonly FakeWeekDay[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

export function defaultShifts(): FakeShift[] {
  return [
    ...weekdays.map((day) => ({ day, start: '08:00', end: '17:00' })),
    { day: 'Saturday', start: '08:00', end: '12:00' },
  ];
}

function profile(
  userId: string,
  specialties: string[],
  role: FakeUserRecord['role'],
): FakeTeamProfile {
  return {
    userId,
    specialties,
    commissionPercent: role === 'Mechanic' ? 10 : 0,
    discountLimitPercent: { Admin: 30, Manager: 20, Attendant: 10, Mechanic: 0 }[role],
    schedule: defaultShifts(),
    updatedAt: null,
  };
}

function user(
  name: string,
  email: string,
  role: FakeUserRecord['role'],
  isActive = true,
): FakeUserRecord {
  return {
    id: fakeId(),
    name,
    email,
    role,
    password: FAKE_ADMIN_PASSWORD,
    isActive,
    createdAt: seededAt,
    updatedAt: null,
  };
}

function customer(
  name: string,
  document: string,
  documentType: 'Cpf' | 'Cnpj',
  email: string,
  phone: string,
): FakeCustomerRecord {
  return {
    id: fakeId(),
    name,
    document,
    documentType,
    email,
    phone,
    address: {
      zipCode: '01310100',
      street: 'Avenida Paulista',
      number: '1000',
      complement: null,
      district: 'Bela Vista',
      city: 'São Paulo',
      state: 'SP',
    },
    consentWhatsApp: true,
    consentEmail: documentType === 'Cpf',
    consentUpdatedAt: seededAt,
    createdAt: seededAt,
    updatedAt: null,
  };
}

function vehicle(
  customerId: string,
  licensePlate: string,
  brand: string,
  model: string,
  year: number,
  mileage: number,
): FakeVehicleRecord {
  return {
    id: fakeId(),
    customerId,
    licensePlate,
    brand,
    model,
    year,
    mileage,
    createdAt: seededAt,
    updatedAt: null,
  };
}

export function createFakeDatabase(): FakeDatabase {
  const catalog = createCatalogSeed();
  const customers = [
    customer('Joana Prado', '52998224725', 'Cpf', 'joana@example.com', '11987654321'),
    customer('Carlos Mendes', '11144477735', 'Cpf', 'carlos@example.com', '1133224455'),
    customer(
      'Transportes Rota Sul Ltda',
      '11222333000181',
      'Cnpj',
      'frota@example.com',
      '51999887766',
    ),
  ];
  const [joana, , fleet] = customers;
  const serviceOrders = createServiceOrdersSeed(catalog, customers);
  const vehicles = [
    ...(joana ? [vehicle(joana.id, 'FJK4E21', 'Volkswagen', 'Polo 1.0 TSI', 2021, 41_230)] : []),
    ...(fleet
      ? [
          vehicle(fleet.id, 'RSL1A23', 'Fiat', 'Fiorino', 2022, 88_400),
          vehicle(fleet.id, 'RSL2B34', 'Fiat', 'Fiorino', 2022, 92_150),
          vehicle(fleet.id, 'ABC1234', 'Renault', 'Master', 2019, 154_900),
        ]
      : []),
  ];
  const users = [
    user('Administrador', FAKE_ADMIN_EMAIL, 'Admin'),
    user('Ana Atendente', 'ana@oficina.local', 'Attendant'),
    user('Marcos Mecânico', 'marcos@oficina.local', 'Mechanic'),
    user('Rita Inativa', 'rita@oficina.local', 'Mechanic', false),
    user('Bia Duas Etapas', FAKE_TWO_FACTOR_EMAIL, 'Attendant'),
    user('Gabi Gestora', 'gestor@oficina.local', 'Manager'),
    { ...user('Pedro Convidado', 'pedro@oficina.local', 'Mechanic'), password: null },
  ];
  const specialties: Readonly<Record<string, string[]>> = {
    'marcos@oficina.local': ['Motor', 'Transmissão'],
    'rita@oficina.local': ['Elétrica'],
    'pedro@oficina.local': ['Suspensão', 'Freios'],
    'gestor@oficina.local': ['Orçamentos', 'Garantia'],
    'ana@oficina.local': ['Atendimento', 'Caixa'],
    [FAKE_ADMIN_EMAIL]: ['Dono', 'Acesso total'],
  };
  const pending = users.find((candidate) => candidate.password === null);
  return {
    users,
    invitations: pending
      ? [
          {
            token: FAKE_INVITATION_TOKEN,
            userId: pending.id,
            expiresAt: new Date(Date.now() + FAKE_INVITATION_LIFETIME_MS).toISOString(),
            acceptedAt: null,
            revokedAt: null,
          },
        ]
      : [],
    catalog,
    inventory: createInventorySeed(catalog.parts),
    teamProfiles: users.map((member) =>
      profile(member.id, specialties[member.email] ?? [], member.role),
    ),
    customers,
    vehicles,
    quotes: createQuotesSeed(catalog, customers),
    settings: createSettingsSeed(),
    assistant: createAssistantSeed(),
    notifications: createNotificationsSeed(),
    automations: createAutomationsSeed(),
    reports: createReportsSeed(),
    dashboard: createDashboardSeed(
      serviceOrders.orders.find((order) => order.number === 3318)?.id ?? null,
    ),
    serviceOrders,
    portal: createPortalSeed(serviceOrders),
    jobs: [],
    sensitiveAccess: [],
    auth: { ...createFakeAuthState(), twoFactorUsers: new Set([FAKE_TWO_FACTOR_EMAIL]) },
  };
}
