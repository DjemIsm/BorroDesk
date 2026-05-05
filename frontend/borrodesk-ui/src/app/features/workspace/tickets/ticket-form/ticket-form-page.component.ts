import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { LanguageService } from '../../../../core/i18n/language.service';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import {
  TicketPriority,
  TicketResponse,
  TicketStatus,
  TicketUserResponse,
  TicketsService
} from '../../../../core/tickets/tickets.service';

const ticketPriorityTranslationKeys: Record<TicketPriority, string> = {
  [TicketPriority.Low]: 'ticketPriority.low',
  [TicketPriority.Normal]: 'ticketPriority.normal',
  [TicketPriority.High]: 'ticketPriority.high',
  [TicketPriority.Urgent]: 'ticketPriority.urgent'
};

const ticketStatusTranslationKeys: Record<TicketStatus, string> = {
  [TicketStatus.Open]: 'ticketStatus.open',
  [TicketStatus.InProgress]: 'ticketStatus.inProgress',
  [TicketStatus.Resolved]: 'ticketStatus.resolved',
  [TicketStatus.Closed]: 'ticketStatus.closed',
  [TicketStatus.Reopened]: 'ticketStatus.reopened'
};

@Component({
  selector: 'app-ticket-form-page',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './ticket-form-page.component.html'
})
export class TicketFormPageComponent implements OnInit {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ticketsService = inject(TicketsService);
  protected readonly i18n = inject(LanguageService);

  protected readonly errorMessage = signal('');
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly ticket = signal<TicketResponse | null>(null);
  protected readonly ticketId = signal<number | null>(null);

  protected readonly priorityOptions = computed(() => [
    { value: TicketPriority.Low, label: this.priorityLabel(TicketPriority.Low) },
    { value: TicketPriority.Normal, label: this.priorityLabel(TicketPriority.Normal) },
    { value: TicketPriority.High, label: this.priorityLabel(TicketPriority.High) },
    { value: TicketPriority.Urgent, label: this.priorityLabel(TicketPriority.Urgent) }
  ]);

  protected readonly ticketForm = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    priority: [TicketPriority.Normal, [Validators.required]]
  });

  protected readonly isEditMode = computed(() => this.ticketId() !== null);
  protected readonly pageEyebrow = computed(() => this.isEditMode()
    ? this.i18n.translate('ticketForm.editEyebrow')
    : this.i18n.translate('ticketForm.createEyebrow'));
  protected readonly pageTitle = computed(() => this.isEditMode()
    ? this.i18n.translate('ticketForm.editTitle')
    : this.i18n.translate('ticketForm.createTitle'));
  protected readonly submitLabel = computed(() => {
    if (this.isSaving()) {
      return this.isEditMode()
        ? this.i18n.translate('ticketForm.saveSubmitBusy')
        : this.i18n.translate('ticketForm.createSubmitBusy');
    }

    return this.isEditMode()
      ? this.i18n.translate('ticketForm.saveSubmit')
      : this.i18n.translate('ticketForm.createSubmit');
  });

  ngOnInit(): void {
    const routeId = this.route.snapshot.paramMap.get('id');
    if (!routeId) {
      return;
    }

    const parsedId = Number(routeId);
    if (!Number.isInteger(parsedId) || parsedId <= 0) {
      this.errorMessage.set(this.i18n.translate('ticketForm.ticketIdInvalid'));
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
      return this.i18n.translate('ticketForm.titleRequired');
    }

    if (title.hasError('maxlength')) {
      return this.i18n.translate('ticketForm.titleMaxLength');
    }

    return '';
  }

  protected statusLabel(status: TicketStatus): string {
    const translationKey = ticketStatusTranslationKeys[status];

    return translationKey ? this.i18n.translate(translationKey) : this.i18n.translate('common.unknown');
  }

  protected priorityLabel(priority: TicketPriority): string {
    const translationKey = ticketPriorityTranslationKeys[priority];

    return translationKey ? this.i18n.translate(translationKey) : this.i18n.translate('common.unknown');
  }

  protected userLabel(user: TicketUserResponse | null): string {
    if (!user) {
      return this.i18n.translate('common.unassigned');
    }

    return user.userName || user.email || this.i18n.translate('common.userNumber', { id: user.id });
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
            this.errorMessage.set(this.i18n.translate('ticketForm.editDenied'));
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
        return this.i18n.translate('common.apiUnavailable');
      }

      if (error.status === 401) {
        return this.i18n.translate('ticketForm.sessionExpired');
      }

      if (error.status === 403) {
        return this.i18n.translate('ticketForm.permissionDenied');
      }

      if (error.status === 404) {
        return this.i18n.translate('ticketForm.ticketNotFound');
      }

      return error.error?.detail || error.error?.title || this.i18n.translate('ticketForm.saveFailed');
    }

    return this.i18n.translate('ticketForm.saveFailed');
  }
}
