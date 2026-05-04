import { ApplicationRole, applicationRoles } from '../../core/auth/auth.service';

export interface AppNavigationItem {
  label: string;
  route: string;
  description: string;
  roles?: readonly ApplicationRole[];
}

export const appNavigation: readonly AppNavigationItem[] = [
  {
    label: 'Dashboard',
    route: '/app/dashboard',
    description: 'Workspace overview'
  },
  {
    label: 'Tickets',
    route: '/app/tickets',
    description: 'All visible requests',
    roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
  },
  {
    label: 'My tickets',
    route: '/app/my-tickets',
    description: 'Requests assigned to you',
    roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
  },
  {
    label: 'Team queue',
    route: '/app/team-queue',
    description: 'Support workload',
    roles: [applicationRoles.support, applicationRoles.admin]
  },
  {
    label: 'User admin',
    route: '/app/admin/users',
    description: 'Accounts and roles',
    roles: [applicationRoles.admin]
  }
];
