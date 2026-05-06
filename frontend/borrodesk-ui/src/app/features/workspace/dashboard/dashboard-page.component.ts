import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { LanguageService } from '../../../core/i18n/language.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import {
  TicketPriority,
  TicketSortField,
  TicketStatus,
  TicketSummaryResponse,
  TicketUserResponse,
  TicketsService
} from '../../../core/tickets/tickets.service';

const ticketStatusTranslationKeys: Record<TicketStatus, string> = {
  [TicketStatus.Open]: 'ticketStatus.open',
  [TicketStatus.InProgress]: 'ticketStatus.inProgress',
  [TicketStatus.Resolved]: 'ticketStatus.resolved',
  [TicketStatus.Closed]: 'ticketStatus.closed',
  [TicketStatus.Reopened]: 'ticketStatus.reopened'
};

const ticketPriorityTranslationKeys: Record<TicketPriority, string> = {
  [TicketPriority.Low]: 'ticketPriority.low',
  [TicketPriority.Normal]: 'ticketPriority.normal',
  [TicketPriority.High]: 'ticketPriority.high',
  [TicketPriority.Urgent]: 'ticketPriority.urgent'
};

@Component({
  selector: 'app-dashboard-page',
  imports: [DatePipe, RouterLink, TranslatePipe],
  templateUrl: './dashboard-page.component.html'
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardListPageSize = 5;
  private readonly ticketsService = inject(TicketsService);
  protected readonly i18n = inject(LanguageService);

  protected readonly errorMessage = signal('');
  protected readonly highPriorityTickets = signal<TicketSummaryResponse[]>([]);
  protected readonly highPriorityTicketsCount = signal(0);
  protected readonly inProgressTicketsCount = signal(0);
  protected readonly isLoading = signal(false);
  protected readonly newestOpenTickets = signal<TicketSummaryResponse[]>([]);
  protected readonly openTicketsCount = signal(0);
  protected readonly recentlyUpdatedTickets = signal<TicketSummaryResponse[]>([]);
  protected readonly ticketPriority = TicketPriority;
  protected readonly ticketStatus = TicketStatus;

  ngOnInit(): void {
    this.loadDashboardData();
  }

  protected priorityLabel(priority: TicketPriority): string {
    const translationKey = ticketPriorityTranslationKeys[priority];

    return translationKey ? this.i18n.translate(translationKey) : this.i18n.translate('common.unknown');
  }

  protected statusLabel(status: TicketStatus): string {
    const translationKey = ticketStatusTranslationKeys[status];

    return translationKey ? this.i18n.translate(translationKey) : this.i18n.translate('common.unknown');
  }

  protected priorityClass(priority: TicketPriority): string {
    return `pill-badge priority-${TicketPriority[priority].toLowerCase()}`;
  }

  protected statusClass(status: TicketStatus): string {
    return `pill-badge status-${TicketStatus[status].toLowerCase()}`;
  }

  protected trackTicket(_index: number, ticket: TicketSummaryResponse): number {
    return ticket.id;
  }

  protected userLabel(user: TicketUserResponse | null): string {
    if (!user) {
      return this.i18n.translate('common.unassigned');
    }

    return user.userName || user.email || this.i18n.translate('common.userNumber', { id: user.id });
  }

  private loadDashboardData(): void {
    this.errorMessage.set('');
    this.isLoading.set(true);

    forkJoin({
      highPriorityTickets: this.ticketsService.getTickets({
        priority: TicketPriority.High,
        pageNumber: 1,
        pageSize: this.dashboardListPageSize
      }),
      inProgressTickets: this.ticketsService.getTickets({
        status: TicketStatus.InProgress,
        pageNumber: 1,
        pageSize: 1
      }),
      newestOpenTickets: this.ticketsService.getTickets({
        status: TicketStatus.Open,
        sortBy: TicketSortField.CreatedAt,
        pageNumber: 1,
        pageSize: this.dashboardListPageSize
      }),
      recentlyUpdatedTickets: this.ticketsService.getTickets({
        sortBy: TicketSortField.UpdatedAt,
        pageNumber: 1,
        pageSize: this.dashboardListPageSize
      })
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ highPriorityTickets, inProgressTickets, newestOpenTickets, recentlyUpdatedTickets }) => {
          this.highPriorityTickets.set(highPriorityTickets.items);
          this.newestOpenTickets.set(newestOpenTickets.items);
          this.recentlyUpdatedTickets.set(recentlyUpdatedTickets.items);
          this.highPriorityTicketsCount.set(highPriorityTickets.totalCount);
          this.inProgressTicketsCount.set(inProgressTickets.totalCount);
          this.openTicketsCount.set(newestOpenTickets.totalCount);
        },
        error: (error: unknown) => {
          this.highPriorityTickets.set([]);
          this.newestOpenTickets.set([]);
          this.recentlyUpdatedTickets.set([]);
          this.errorMessage.set(this.resolveDashboardError(error));
        }
      });
  }

  private resolveDashboardError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'common.apiUnavailable';
      }

      if (error.status === 401) {
        return 'tickets.sessionExpired';
      }

      if (error.status === 403) {
        return 'tickets.permissionDenied';
      }
    }

    return 'dashboard.loadFailed';
  }
}
