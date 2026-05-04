import { Routes } from '@angular/router';
import { applicationRoles } from './core/auth/auth.service';
import { authChildGuard, authGuard, loginRedirectGuard, roleGuard } from './core/auth/auth.guards';
import { DashboardPageComponent } from './features/workspace/dashboard/dashboard-page.component';
import { ForbiddenPageComponent } from './features/workspace/forbidden/forbidden-page.component';
import { AdminUsersPageComponent } from './features/workspace/admin-users/admin-users-page.component';
import { TeamQueuePageComponent } from './features/workspace/team-queue/team-queue-page.component';
import { TicketDetailPageComponent } from './features/workspace/tickets/ticket-detail/ticket-detail-page.component';
import { TicketFormPageComponent } from './features/workspace/tickets/ticket-form/ticket-form-page.component';
import { MyTicketsPageComponent } from './features/workspace/tickets/my-tickets-page.component';
import { TicketsPageComponent } from './features/workspace/tickets/tickets-page.component';
import { LoginComponent } from './features/login/login.component';
import { AppLayoutComponent } from './layout/app-layout/app-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [loginRedirectGuard],
    component: LoginComponent,
    title: 'Sign in | BorroDesk'
  },
  {
    path: 'app',
    component: AppLayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [authChildGuard],
    children: [
      {
        path: 'dashboard',
        component: DashboardPageComponent,
        title: 'Dashboard | BorroDesk'
      },
      {
        path: 'tickets',
        canActivate: [roleGuard],
        component: TicketsPageComponent,
        data: {
          roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
        },
        title: 'Tickets | BorroDesk'
      },
      {
        path: 'tickets/new',
        canActivate: [roleGuard],
        component: TicketFormPageComponent,
        data: {
          roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
        },
        title: 'Create ticket | BorroDesk'
      },
      {
        path: 'tickets/:id/edit',
        canActivate: [roleGuard],
        component: TicketFormPageComponent,
        data: {
          roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
        },
        title: 'Edit ticket | BorroDesk'
      },
      {
        path: 'tickets/:id',
        canActivate: [roleGuard],
        component: TicketDetailPageComponent,
        data: {
          roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
        },
        title: 'Ticket detail | BorroDesk'
      },
      {
        path: 'my-tickets',
        canActivate: [roleGuard],
        component: MyTicketsPageComponent,
        data: {
          roles: [applicationRoles.user, applicationRoles.support, applicationRoles.admin]
        },
        title: 'My tickets | BorroDesk'
      },
      {
        path: 'team-queue',
        canActivate: [roleGuard],
        component: TeamQueuePageComponent,
        data: {
          roles: [applicationRoles.support, applicationRoles.admin]
        },
        title: 'Team queue | BorroDesk'
      },
      {
        path: 'admin/users',
        canActivate: [roleGuard],
        component: AdminUsersPageComponent,
        data: {
          roles: [applicationRoles.admin]
        },
        title: 'User admin | BorroDesk'
      },
      {
        path: 'forbidden',
        component: ForbiddenPageComponent,
        title: 'Access denied | BorroDesk'
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      }
    ]
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'app/dashboard'
  },
  {
    path: '**',
    redirectTo: 'app/dashboard'
  }
];
