import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import {
  PagedResponse,
  TicketPriority,
  TicketQuery,
  TicketStatus,
  TicketSummaryResponse,
  TicketUserResponse,
  TicketsService
} from '../../../core/tickets/tickets.service';

type TicketOwnershipFilter = 'all' | 'assignedToMe' | 'createdByMe';

const ticketStatusLabels: Record<TicketStatus, string> = {
  [TicketStatus.Open]: 'Open',
  [TicketStatus.InProgress]: 'In progress',
  [TicketStatus.Resolved]: 'Resolved',
  [TicketStatus.Closed]: 'Closed',
  [TicketStatus.Reopened]: 'Reopened'
};

const ticketPriorityLabels: Record<TicketPriority, string> = {
  [TicketPriority.Low]: 'Low',
  [TicketPriority.Normal]: 'Normal',
  [TicketPriority.High]: 'High',
  [TicketPriority.Urgent]: 'Urgent'
};

@Component({
  selector: 'app-tickets-page',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './tickets-page.component.html'
})
export class TicketsPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ticketsService = inject(TicketsService);

  protected readonly pageSizeOptions = [10, 25, 50, 100] as const;
  protected readonly priorityOptions = [
    { value: TicketPriority.Low, label: ticketPriorityLabels[TicketPriority.Low] },
    { value: TicketPriority.Normal, label: ticketPriorityLabels[TicketPriority.Normal] },
    { value: TicketPriority.High, label: ticketPriorityLabels[TicketPriority.High] },
    { value: TicketPriority.Urgent, label: ticketPriorityLabels[TicketPriority.Urgent] }
  ];
  protected readonly statusOptions = [
    { value: TicketStatus.Open, label: ticketStatusLabels[TicketStatus.Open] },
    { value: TicketStatus.InProgress, label: ticketStatusLabels[TicketStatus.InProgress] },
    { value: TicketStatus.Resolved, label: ticketStatusLabels[TicketStatus.Resolved] },
    { value: TicketStatus.Closed, label: ticketStatusLabels[TicketStatus.Closed] },
    { value: TicketStatus.Reopened, label: ticketStatusLabels[TicketStatus.Reopened] }
  ];

  protected readonly errorMessage = signal('');
  protected readonly isLoading = signal(false);
  protected readonly pageNumber = signal(1);
  protected readonly ticketResponse = signal<PagedResponse<TicketSummaryResponse> | null>(null);

  protected readonly filterForm = this.formBuilder.group({
    search: '',
    status: '',
    priority: '',
    ownership: 'all' as TicketOwnershipFilter,
    pageSize: 25
  });

  protected readonly tickets = computed(() => this.ticketResponse()?.items ?? []);
  protected readonly totalCount = computed(() => this.ticketResponse()?.totalCount ?? 0);
  protected readonly totalPages = computed(() => this.ticketResponse()?.totalPages ?? 0);
  protected readonly hasPreviousPage = computed(() => this.pageNumber() > 1);
  protected readonly hasNextPage = computed(() => {
    const totalPages = this.totalPages();

    return totalPages > 0 && this.pageNumber() < totalPages;
  });
  protected readonly resultSummary = computed(() => {
    const response = this.ticketResponse();
    if (!response || response.totalCount === 0) {
      return '0 tickets';
    }

    const start = (response.pageNumber - 1) * response.pageSize + 1;
    const end = Math.min(response.pageNumber * response.pageSize, response.totalCount);

    return `${start}-${end} of ${response.totalCount} tickets`;
  });

  ngOnInit(): void {
    this.restoreQueryParams();
    this.loadTickets();
  }

  protected applyFilters(): void {
    this.pageNumber.set(1);
    this.loadTickets();
  }

  protected resetFilters(): void {
    this.filterForm.reset({
      search: '',
      status: '',
      priority: '',
      ownership: 'all',
      pageSize: 25
    });
    this.pageNumber.set(1);
    this.loadTickets();
  }

  protected goToPage(pageNumber: number): void {
    const totalPages = this.totalPages();
    const nextPage = Math.max(1, totalPages > 0 ? Math.min(pageNumber, totalPages) : 1);

    if (nextPage === this.pageNumber()) {
      return;
    }

    this.pageNumber.set(nextPage);
    this.loadTickets();
  }

  protected statusLabel(status: TicketStatus): string {
    return ticketStatusLabels[status] ?? 'Unknown';
  }

  protected priorityLabel(priority: TicketPriority): string {
    return ticketPriorityLabels[priority] ?? 'Unknown';
  }

  protected statusClass(status: TicketStatus): string {
    return `pill-badge status-${TicketStatus[status].toLowerCase()}`;
  }

  protected priorityClass(priority: TicketPriority): string {
    return `pill-badge priority-${TicketPriority[priority].toLowerCase()}`;
  }

  protected userLabel(user: TicketUserResponse | null): string {
    if (!user) {
      return 'Unassigned';
    }

    return user.userName || user.email || `User #${user.id}`;
  }

  protected trackTicket(_index: number, ticket: TicketSummaryResponse): number {
    return ticket.id;
  }

  private loadTickets(): void {
    const query = this.buildQuery();

    this.syncQueryParams(query);
    this.errorMessage.set('');
    this.isLoading.set(true);

    this.ticketsService
      .getTickets(query)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.ticketResponse.set(response);

          if (response.totalPages > 0 && response.pageNumber > response.totalPages) {
            this.pageNumber.set(response.totalPages);
            this.loadTickets();
          }
        },
        error: (error: unknown) => {
          this.ticketResponse.set(null);
          this.errorMessage.set(this.resolveTicketsError(error));
        }
      });
  }

  private buildQuery(): TicketQuery {
    const rawFilters = this.filterForm.getRawValue();
    const currentSession = this.authService.getSession();
    const search = rawFilters.search.trim();
    const query: TicketQuery = {
      pageNumber: this.pageNumber(),
      pageSize: Number(rawFilters.pageSize)
    };

    if (search) {
      query.search = search;
    }

    if (rawFilters.status) {
      query.status = Number(rawFilters.status) as TicketStatus;
    }

    if (rawFilters.priority) {
      query.priority = Number(rawFilters.priority) as TicketPriority;
    }

    if (rawFilters.ownership === 'assignedToMe' && currentSession) {
      query.assignedToUserId = currentSession.userId;
    }

    if (rawFilters.ownership === 'createdByMe' && currentSession) {
      query.createdByUserId = currentSession.userId;
    }

    return query;
  }

  private restoreQueryParams(): void {
    const queryParams = this.route.snapshot.queryParamMap;
    const pageSize = this.parsePositiveInteger(queryParams.get('pageSize'), 25);
    const pageNumber = this.parsePositiveInteger(queryParams.get('pageNumber'), 1);
    const ownership = queryParams.get('ownership');

    this.filterForm.patchValue({
      search: queryParams.get('search') ?? '',
      status: queryParams.get('status') ?? '',
      priority: queryParams.get('priority') ?? '',
      ownership: this.isOwnershipFilter(ownership) ? ownership : 'all',
      pageSize: this.pageSizeOptions.includes(pageSize as (typeof this.pageSizeOptions)[number])
        ? pageSize
        : 25
    });
    this.pageNumber.set(pageNumber);
  }

  private syncQueryParams(query: TicketQuery): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: query.search || null,
        status: query.status || null,
        priority: query.priority || null,
        ownership: this.filterForm.controls.ownership.value === 'all'
          ? null
          : this.filterForm.controls.ownership.value,
        pageNumber: query.pageNumber === 1 ? null : query.pageNumber,
        pageSize: query.pageSize === 25 ? null : query.pageSize
      },
      replaceUrl: true
    });
  }

  private parsePositiveInteger(value: string | null, fallback: number): number {
    const parsedValue = Number(value);

    return Number.isInteger(parsedValue) && parsedValue > 0 ? parsedValue : fallback;
  }

  private isOwnershipFilter(value: string | null): value is TicketOwnershipFilter {
    return value === 'all' || value === 'assignedToMe' || value === 'createdByMe';
  }

  private resolveTicketsError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'Cannot reach the BorroDesk API. Start the backend and try again.';
      }

      if (error.status === 401) {
        return 'Your session expired. Sign in again to load tickets.';
      }

      if (error.status === 403) {
        return 'Your role does not have access to this ticket list.';
      }

      return error.error?.detail || error.error?.title || 'Tickets could not be loaded.';
    }

    return 'Tickets could not be loaded.';
  }
}
