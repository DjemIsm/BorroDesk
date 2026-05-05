import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { ApplicationRole } from '../auth/auth.service';

export interface AdminUserResponse {
  id: number;
  userName: string | null;
  email: string | null;
  emailConfirmed: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  roles: ApplicationRole[];
}

export interface AdminUsersPagedResponse {
  items: AdminUserResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AdminUserQueryParameters {
  search?: string;
  role?: ApplicationRole;
  isActive?: boolean;
  pageNumber: number;
  pageSize: number;
}

export interface AdminCreateUserRequest {
  userName: string;
  email: string;
  password: string;
  isActive: boolean;
  roles: ApplicationRole[];
}

export interface AdminUpdateUserRequest {
  userName: string;
  email: string;
  isActive: boolean;
  roles: ApplicationRole[];
}

export interface AdminResetUserPasswordRequest {
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminUsersService {
  private readonly http = inject(HttpClient);

  getUsers(query: AdminUserQueryParameters): Observable<AdminUsersPagedResponse> {
    return this.http.get<AdminUsersPagedResponse>('/api/admin/users', {
      params: this.toHttpParams(query)
    });
  }

  getUser(id: number): Observable<AdminUserResponse> {
    return this.http.get<AdminUserResponse>(`/api/admin/users/${id}`);
  }

  createUser(request: AdminCreateUserRequest): Observable<AdminUserResponse> {
    return this.http.post<AdminUserResponse>('/api/admin/users', request);
  }

  updateUser(id: number, request: AdminUpdateUserRequest): Observable<AdminUserResponse> {
    return this.http.put<AdminUserResponse>(`/api/admin/users/${id}`, request);
  }

  resetUserPassword(id: number, request: AdminResetUserPasswordRequest): Observable<void> {
    return this.http.patch<void>(`/api/admin/users/${id}/password`, request);
  }

  deactivateUser(id: number): Observable<void> {
    return this.http.delete<void>(`/api/admin/users/${id}`);
  }

  private toHttpParams(query: AdminUserQueryParameters): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.role) {
      params = params.set('role', query.role);
    }

    if (query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }

    return params;
  }
}
