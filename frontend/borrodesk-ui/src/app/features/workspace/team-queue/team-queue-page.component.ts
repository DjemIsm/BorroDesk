import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { LanguageService } from '../../../core/i18n/language.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import {
  PagedResponse,
  TicketPriority,
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
  selector: 'app-team-queue-page',
  imports: [DatePipe, RouterLink, TranslatePipe],
  templateUrl: './team-queue-page.component.html'
})
export class TeamQueuePageComponent implements OnInit {
  protected readonly i18n = inject(LanguageService);
  private readonly ticketsService = inject(TicketsService);
  private readonly pageSize = 25;

  protected readonly errorMessage = signal('');
  protected readonly isLoading = signal(false);
  protected readonly ticketResponse = signal<PagedResponse<TicketSummaryResponse> | null>(null);

  protected readonly tickets = computed(() => this.ticketResponse()?.items ?? []);
  protected readonly totalCount = computed(() => this.ticketResponse()?.totalCount ?? 0);

  ngOnInit(): void {
    this.loadTeamQueue();
  }

  protected statusLabel(status: TicketStatus): string {
    const translationKey = ticketStatusTranslationKeys[status];

    return translationKey ? this.i18n.translate(translationKey) : this.i18n.translate('common.unknown');
  }

  protected priorityLabel(priority: TicketPriority): string {
    const translationKey = ticketPriorityTranslationKeys[priority];

    return translationKey ? this.i18n.translate(translationKey) : this.i18n.translate('common.unknown');
  }

  protected statusClass(status: TicketStatus): string {
    return `pill-badge status-${TicketStatus[status].toLowerCase()}`;
  }

  protected priorityClass(priority: TicketPriority): string {
    return `pill-badge priority-${TicketPriority[priority].toLowerCase()}`;
  }

  protected userLabel(user: TicketUserResponse | null): string {
    if (!user) {
      return this.i18n.translate('common.unassigned');
    }

    return user.userName || user.email || this.i18n.translate('common.userNumber', { id: user.id });
  }

  protected trackTicket(_index: number, ticket: TicketSummaryResponse): number {
    return ticket.id;
  }

  private loadTeamQueue(): void {
    this.errorMessage.set('');
    this.isLoading.set(true);

    this.ticketsService
      .getTickets({
        status: TicketStatus.Open,
        pageNumber: 1,
        pageSize: this.pageSize
      })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.ticketResponse.set(response);
        },
        error: (error: unknown) => {
          this.ticketResponse.set(null);
          this.errorMessage.set(this.resolveTeamQueueError(error));
        }
      });
  }

  private resolveTeamQueueError(error: unknown): string {
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

    return 'teamQueue.loadFailed';
  }
}
