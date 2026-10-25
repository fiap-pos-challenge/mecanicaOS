import {
  Building as BuildingIcon,
  ChartLine as ChartLineIcon,
  Files as FilesIcon,
  FileText as FileTextIcon,
  LayoutDashboard as LayoutDashboardIcon,
  LayoutGrid as LayoutGridIcon,
  Package as PackageIcon,
  Settings as SettingsIcon,
  Smartphone as SmartphoneIcon,
  SquareKanban as SquareKanbanIcon,
  UserCog as UserCogIcon,
  Users as UsersIcon,
  Wallet as WalletIcon,
  Wrench as WrenchIcon,
  Zap as ZapIcon,
} from 'lucide';
import type { IconNode } from 'lucide';
import { Permission } from '../core/auth/permissions';
import { messages } from '../core/i18n/messages.pt-BR';
import { NavGroup, Screen, SCREENS } from './screens';

export const SCREEN_ICONS: Readonly<Record<Screen['id'], IconNode>> = {
  dashboard: LayoutDashboardIcon,
  serviceOrders: SquareKanbanIcon,
  quotes: FileTextIcon,
  mechanic: WrenchIcon,
  customers: UsersIcon,
  catalog: LayoutGridIcon,
  team: UserCogIcon,
  inventory: PackageIcon,
  reports: ChartLineIcon,
  finance: WalletIcon,
  documents: FilesIcon,
  automations: ZapIcon,
  portal: SmartphoneIcon,
  settings: SettingsIcon,
  platform: BuildingIcon,
};

export interface NavItem {
  readonly path: string;
  readonly label: string;
  readonly icon: IconNode;
  readonly permission: Permission;
}

export interface NavSection {
  readonly group: NavGroup;
  readonly label: string;
  readonly items: readonly NavItem[];
}

const groupOrder: readonly NavGroup[] = ['operation', 'registry', 'warehouse', 'management'];

export const NAV_SECTIONS: readonly NavSection[] = groupOrder.map((group) => ({
  group,
  label: messages.navGroups[group],
  items: SCREENS.filter((screen) => screen.group === group).map((screen) => ({
    path: screen.path,
    label: screen.label,
    icon: SCREEN_ICONS[screen.id],
    permission: screen.permission,
  })),
}));

export function sectionsFor(can: (permission: Permission) => boolean): readonly NavSection[] {
  return NAV_SECTIONS.map((section) => ({
    ...section,
    items: section.items.filter((item) => can(item.permission)),
  })).filter((section) => section.items.length > 0);
}
