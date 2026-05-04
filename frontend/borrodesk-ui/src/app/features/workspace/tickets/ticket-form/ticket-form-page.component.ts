import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  TicketPriority,
  TicketResponse,
  TicketStatus,
  TicketsService
} from '../../../../core/tickets/tickets.service';

const ticketPriorityLabels: Record<TicketPriority, string> = {
  [TicketPriority.Low]: 'Low',
  [TicketPriority.Normal]: 'Normal',
  [TicketPriority.High]: 'High',
  [TicketPriority.Urgent]: 'Urgent'
};

const ticketStatusLabels: Record<TicketStatus, string> = {
  [TicketStatus.Open]: 'Open',
  [TicketStatus.InProgress]: 'In progress',
  [TicketStatus.Resolved]: 'Resolved',
  [TicketStatus.Closed]: 'Closed',
  [TicketStatus.Reopened]: 'Reopened'
};

@Component({
  selector: 'app-ticket-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-form-page.component.html'
})
export class TicketFormPageComponent implements OnInit {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ticketsService = inject(TicketsService);

  protected readonly errorMessage = signal('');
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly ticket = signal<TicketResponse | null>(null);
  protected readonly ticketId = signal<number | null>(null);

  protected readonly priorityOptions = [
    { value: TicketPriority.Low, label: ticketPriorityLabels[TicketPriority.Low] },
    { value: TicketPriority.Normal, label: ticketPriorityLabels[TicketPriority.Normal] },
    { value: TicketPriority.High, label: ticketPriorityLabels[TicketPriority.High] },
    { value: TicketPriority.Urgent, label: ticketPriorityLabels[TicketPriority.Urgent] }
  ];

  protected readonly ticketForm = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    priority: [TicketPriority.Normal, [Validators.required]]
  });

  protected readonly isEditMode = computed(() => this.ticketId() !== null);
  protected readonly pageTitle = computed(() => this.isEditMode() ? 'Edit ticket' : 'Create ticket');
  protected readonly submitLabel = computed(() => {
    if (this.isSaving()) {
      return this.isEditMode() ? 'Saving changes' : 'Creating ticket';
    }

    return this.isEditMode() ? 'Save changes' : 'Create ticket';
  });

  ngOnInit(): void {
    const routeId = this.route.snapshot.paramMap.get('id');
    if (!routeId) {
      return;
    }

    const parsedId = Number(routeId);
    if (!Number.isInteger(parsedId) || parsedId <= 0) {
      this.errorMessage.set('Ticket id is invalid.');
      this.ticketForm.disable();
      return;
    }

    this.ticketId.set(parsedId);
    this.loadTicket(parsedId);
  }

  protected submit(): void {
    this.errorMessage.set('');

    if (this.ticketForm.invalid) {
      this.ticketForm.markAllAsTouched();
      return;
    }

    const rawFormValue = this.ticketForm.getRawValue();
    const request = {
      title: rawFormValue.title.trim(),
      description: rawFormValue.description.trim(),
      priority: Number(rawFormValue.priority) as TicketPriority
    };
    const ticketId = this.ticketId();
    this.isSaving.set(true);

    const saveRequest = ticketId
      ? this.ticketsService.updateTicket(ticketId, request)
      : this.ticketsService.createTicket(request);

    saveRequest.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: () => {
        void this.router.navigate(['/app/tickets']);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.resolveTicketError(error));
      }
    });
  }

  protected titleError(): string {
    const title = this.ticketForm.controls.title;
    if (title.hasError('required')) {
      return 'Enter a ticket title.';
    }

    if (title.hasError('maxlength')) {
      return 'Keep the title at 200 characters or less.';
    }

    return '';
  }

  protected statusLabel(status: TicketStatus): string {
    return ticketStatusLabels[status] ?? 'Unknown';
  }

  private loadTicket(id: number): void {
    this.isLoading.set(true);
    this.ticketForm.disable();

    this.ticketsService
      .getTicket(id)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (ticket) => {
          this.ticket.set(ticket);
          this.ticketForm.reset({
            title: ticket.title,
            description: ticket.description,
            priority: ticket.priority
          });

          if (ticket.canEdit) {
            this.ticketForm.enable();
          } else {
            this.errorMessage.set('You can view this ticket, but your role cannot edit it.');
          }
        },
        error: (error: unknown) => {
          this.errorMessage.set(this.resolveTicketError(error));
        }
      });
  }

  private resolveTicketError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'Cannot reach the BorroDesk API. Start the backend and try again.';
      }

      if (error.status === 401) {
        return 'Your session expired. Sign in again to continue.';
      }

      if (error.status === 403) {
        return 'Your role does not have permission to change this ticket.';
      }

      if (error.status === 404) {
        return 'Ticket was not found.';
      }

      return error.error?.detail || error.error?.title || 'Ticket could not be saved.';
    }

    return 'Ticket could not be saved.';
  }
}
