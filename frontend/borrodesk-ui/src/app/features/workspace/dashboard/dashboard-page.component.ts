import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { TicketPriority, TicketStatus, TicketsService } from '../../../core/tickets/tickets.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './dashboard-page.component.html'
})
export class DashboardPageComponent implements OnInit {
  private readonly ticketsService = inject(TicketsService);

  protected readonly errorMessage = signal('');
  protected readonly highPriorityTicketsCount = signal(0);
  protected readonly inProgressTicketsCount = signal(0);
  protected readonly isLoading = signal(false);
  protected readonly openTicketsCount = signal(0);
  protected readonly ticketPriority = TicketPriority;
  protected readonly ticketStatus = TicketStatus;

  ngOnInit(): void {
    this.loadDashboardCounts();
  }

  private loadDashboardCounts(): void {
    this.errorMessage.set('');
    this.isLoading.set(true);

    forkJoin({
      highPriorityTickets: this.ticketsService.getTickets({
        priority: TicketPriority.High,
        pageNumber: 1,
        pageSize: 1
      }),
      inProgressTickets: this.ticketsService.getTickets({
        status: TicketStatus.InProgress,
        pageNumber: 1,
        pageSize: 1
      }),
      openTickets: this.ticketsService.getTickets({
        status: TicketStatus.Open,
        pageNumber: 1,
        pageSize: 1
      })
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ highPriorityTickets, inProgressTickets, openTickets }) => {
          this.highPriorityTicketsCount.set(highPriorityTickets.totalCount);
          this.inProgressTicketsCount.set(inProgressTickets.totalCount);
          this.openTicketsCount.set(openTickets.totalCount);
        },
        error: (error: unknown) => {
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
