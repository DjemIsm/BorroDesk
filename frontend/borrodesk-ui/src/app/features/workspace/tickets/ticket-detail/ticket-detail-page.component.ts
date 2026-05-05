import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { LanguageService } from '../../../../core/i18n/language.service';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import {
  TicketAttachmentResponse,
  TicketCommentResponse,
  TicketPriority,
  TicketResponse,
  TicketStatus,
  TicketUserResponse,
  TicketsService
} from '../../../../core/tickets/tickets.service';

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
  selector: 'app-ticket-detail-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './ticket-detail-page.component.html'
})
export class TicketDetailPageComponent implements OnInit, OnDestroy {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly ticketsService = inject(TicketsService);
  private readonly objectUrls: string[] = [];
  protected readonly i18n = inject(LanguageService);

  protected readonly attachmentError = signal('');
  protected readonly commentError = signal('');
  protected readonly detailError = signal('');
  protected readonly isDownloadingAttachmentId = signal<number | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isPostingComment = signal(false);
  protected readonly isUploadingAttachment = signal(false);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly ticket = signal<TicketResponse | null>(null);
  protected readonly ticketId = signal<number | null>(null);

  protected readonly commentForm = this.formBuilder.group({
    text: ['', [Validators.required]]
  });

  protected readonly selectedFileLabel = computed(() => (
    this.selectedFile()?.name || this.i18n.translate('ticketDetail.noFileSelected')
  ));

  ngOnInit(): void {
    const routeId = this.route.snapshot.paramMap.get('id');
    const parsedId = Number(routeId);

    if (!Number.isInteger(parsedId) || parsedId <= 0) {
      this.detailError.set(this.i18n.translate('ticketDetail.ticketIdInvalid'));
      return;
    }

    this.ticketId.set(parsedId);
    this.loadTicket(parsedId);
  }

  ngOnDestroy(): void {
    this.objectUrls.forEach((url) => URL.revokeObjectURL(url));
  }

  protected submitComment(): void {
    const ticketId = this.ticketId();
    this.commentError.set('');

    if (!ticketId) {
      return;
    }

    if (this.commentForm.invalid) {
      this.commentForm.markAllAsTouched();
      return;
    }

    const text = this.commentForm.controls.text.value.trim();
    if (!text) {
      this.commentForm.controls.text.setErrors({ required: true });
      return;
    }

    this.isPostingComment.set(true);
    this.ticketsService
      .addTicketComment(ticketId, { text })
      .pipe(finalize(() => this.isPostingComment.set(false)))
      .subscribe({
        next: (comment) => {
          this.commentForm.reset({ text: '' });
          this.ticket.update((ticket) => ticket
            ? { ...ticket, comments: [...ticket.comments, comment] }
            : ticket);
        },
        error: (error: unknown) => {
          this.commentError.set(this.resolveTicketError(
            error,
            this.i18n.translate('ticketDetail.commentAddFailed')
          ));
        }
      });
  }

  protected selectAttachment(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.attachmentError.set('');
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  protected uploadAttachment(): void {
    const ticketId = this.ticketId();
    const file = this.selectedFile();
    this.attachmentError.set('');

    if (!ticketId || !file) {
      this.attachmentError.set(this.i18n.translate('ticketDetail.chooseFile'));
      return;
    }

    this.isUploadingAttachment.set(true);
    this.ticketsService
      .uploadTicketAttachment(ticketId, file)
      .pipe(finalize(() => this.isUploadingAttachment.set(false)))
      .subscribe({
        next: (attachment) => {
          this.selectedFile.set(null);
          this.ticket.update((ticket) => ticket
            ? { ...ticket, attachments: [...ticket.attachments, attachment] }
            : ticket);
        },
        error: (error: unknown) => {
          this.attachmentError.set(this.resolveTicketError(
            error,
            this.i18n.translate('ticketDetail.attachmentUploadFailed')
          ));
        }
      });
  }

  protected downloadAttachment(attachment: TicketAttachmentResponse): void {
    const ticketId = this.ticketId();
    if (!ticketId) {
      return;
    }

    this.attachmentError.set('');
    this.isDownloadingAttachmentId.set(attachment.id);
    this.ticketsService
      .getTicketAttachment(ticketId, attachment.id)
      .pipe(finalize(() => this.isDownloadingAttachmentId.set(null)))
      .subscribe({
        next: (file) => {
          const objectUrl = URL.createObjectURL(file);
          this.objectUrls.push(objectUrl);

          const link = document.createElement('a');
          link.href = objectUrl;
          link.download = attachment.fileName;
          link.click();
        },
        error: (error: unknown) => {
          this.attachmentError.set(this.resolveTicketError(
            error,
            this.i18n.translate('ticketDetail.attachmentDownloadFailed')
          ));
        }
      });
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

  protected fileSizeLabel(fileSizeBytes: number): string {
    if (fileSizeBytes < 1024) {
      return `${fileSizeBytes} B`;
    }

    if (fileSizeBytes < 1024 * 1024) {
      return `${this.formatFileNumber(fileSizeBytes / 1024)} KB`;
    }

    return `${this.formatFileNumber(fileSizeBytes / 1024 / 1024)} MB`;
  }

  protected trackAttachment(_index: number, attachment: TicketAttachmentResponse): number {
    return attachment.id;
  }

  protected trackComment(_index: number, comment: TicketCommentResponse): number {
    return comment.id;
  }

  private loadTicket(id: number): void {
    this.detailError.set('');
    this.isLoading.set(true);

    this.ticketsService
      .getTicket(id)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (ticket) => {
          this.ticket.set(ticket);
        },
        error: (error: unknown) => {
          this.detailError.set(this.resolveTicketError(
            error,
            this.i18n.translate('ticketDetail.detailLoadFailed')
          ));
        }
      });
  }

  private resolveTicketError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return this.i18n.translate('common.apiUnavailable');
      }

      if (error.status === 401) {
        return this.i18n.translate('ticketForm.sessionExpired');
      }

      if (error.status === 403) {
        return this.i18n.translate('ticketDetail.permissionDenied');
      }

      if (error.status === 404) {
        return this.i18n.translate('ticketDetail.ticketAttachmentNotFound');
      }

      return error.error?.detail || error.error?.title || fallback;
    }

    return fallback;
  }

  private formatFileNumber(value: number): string {
    return new Intl.NumberFormat(this.i18n.dateLocale(), {
      maximumFractionDigits: 1,
      minimumFractionDigits: 1
    }).format(value);
  }
}
