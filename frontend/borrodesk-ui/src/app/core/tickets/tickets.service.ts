import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export enum TicketStatus {
  Open = 1,
  InProgress = 2,
  Resolved = 3,
  Closed = 4,
  Reopened = 5
}

export enum TicketPriority {
  Low = 1,
  Normal = 2,
  High = 3,
  Urgent = 4
}

export interface TicketUserResponse {
  id: number;
  userName: string | null;
  email: string | null;
}

export interface TicketSummaryResponse {
  id: number;
  title: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdBy: TicketUserResponse;
  assignedTo: TicketUserResponse | null;
  createdAt: string;
  updatedAt: string | null;
  closedAt: string | null;
  canEdit: boolean;
  canAssign: boolean;
  canDelete: boolean;
}

export interface TicketCommentResponse {
  id: number;
  ticketId: number;
  author: TicketUserResponse;
  text: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface TicketAttachmentResponse {
  id: number;
  ticketId: number;
  uploadedBy: TicketUserResponse;
  fileName: string;
  storedFileName: string;
  contentType: string | null;
  fileSizeBytes: number;
  uploadedAt: string;
}

export interface TicketResponse extends TicketSummaryResponse {
  description: string;
  availableStatusTransitions: TicketStatus[];
  comments: TicketCommentResponse[];
  attachments: TicketAttachmentResponse[];
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
}

export interface UpdateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
}

export interface CreateTicketCommentRequest {
  text: string;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface TicketQuery {
  status?: TicketStatus;
  priority?: TicketPriority;
  assignedToUserId?: number;
  createdByUserId?: number;
  search?: string;
  pageNumber: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class TicketsService {
  private readonly http = inject(HttpClient);

  getTickets(query: TicketQuery): Observable<PagedResponse<TicketSummaryResponse>> {
    return this.http.get<PagedResponse<TicketSummaryResponse>>('/api/tickets', {
      params: this.toHttpParams(query)
    });
  }

  getTicket(id: number): Observable<TicketResponse> {
    return this.http.get<TicketResponse>(`/api/tickets/${id}`);
  }

  createTicket(request: CreateTicketRequest): Observable<TicketResponse> {
    return this.http.post<TicketResponse>('/api/tickets', request);
  }

  updateTicket(id: number, request: UpdateTicketRequest): Observable<TicketResponse> {
    return this.http.put<TicketResponse>(`/api/tickets/${id}`, request);
  }

  addTicketComment(id: number, request: CreateTicketCommentRequest): Observable<TicketCommentResponse> {
    return this.http.post<TicketCommentResponse>(`/api/tickets/${id}/comments`, request);
  }

  uploadTicketAttachment(id: number, file: File): Observable<TicketAttachmentResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<TicketAttachmentResponse>(`/api/tickets/${id}/attachments`, formData);
  }

  getTicketAttachment(id: number, attachmentId: number, download = true): Observable<Blob> {
    return this.http.get(`/api/tickets/${id}/attachments/${attachmentId}`, {
      params: new HttpParams().set('download', download),
      responseType: 'blob'
    });
  }

  private toHttpParams(query: TicketQuery): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.status) {
      params = params.set('status', query.status);
    }

    if (query.priority) {
      params = params.set('priority', query.priority);
    }

    if (query.assignedToUserId) {
      params = params.set('assignedToUserId', query.assignedToUserId);
    }

    if (query.createdByUserId) {
      params = params.set('createdByUserId', query.createdByUserId);
    }

    return params;
  }
}
