import { ApplicationRole, applicationRoles } from '../../core/auth/auth.service';

export interface AppNavigationItem {
  labelKey: string;
  route: string;
  descriptionKey: string;
  roles?: readonly ApplicationRole[];
}

export const appNavigation: readonly AppNavigationItem[] = [
  {
    labelKey: 'nav.dashboard.label',
    route: '/app/dashboard',
    descriptionKey: 'nav.dashboard.description'
  },
  {
    labelKey: 'nav.tickets.label',
    route: '/app/tickets',
    descriptionKey: 'nav.tickets.description',
    roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
  },
  {
    labelKey: 'nav.myTickets.label',
    route: '/app/my-tickets',
    descriptionKey: 'nav.myTickets.description',
    roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
  },
  {
    labelKey: 'nav.teamQueue.label',
    route: '/app/team-queue',
    descriptionKey: 'nav.teamQueue.description',
    roles: [applicationRoles.support, applicationRoles.admin]
  },
  {
    labelKey: 'nav.adminUsers.label',
    route: '/app/admin/users',
    descriptionKey: 'nav.adminUsers.description',
    roles: [applicationRoles.admin]
  }
];
