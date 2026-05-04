import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

export type ApplicationRole = 'User' | 'Support' | 'Admin';

export const applicationRoles = {
  user: 'User',
  support: 'Support',
  admin: 'Admin'
} as const;

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  userId: number;
  userName: string | null;
  email: string | null;
  roles: ApplicationRole[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'borrodesk.auth';
  private readonly sessionState = signal<LoginResponse | null>(this.readStoredSession());

  readonly session = this.sessionState.asReadonly();
  readonly isAuthenticated = computed(() => this.isSessionActive(this.sessionState()));

  login(request: LoginRequest, persistSession = true): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', request).pipe(
      tap((response) => this.setSession(response, persistSession))
    );
  }

  getSession(): LoginResponse | null {
    const currentSession = this.sessionState();
    if (!this.isSessionActive(currentSession)) {
      this.clearSession();
      return null;
    }

    return currentSession;
  }

  hasAnyRole(roles: readonly ApplicationRole[] = []): boolean {
    if (roles.length === 0) {
      return true;
    }

    const currentSession = this.getSession();
    if (!currentSession) {
      return false;
    }

    return roles.some((role) => currentSession.roles.includes(role));
  }

  clearSession(): void {
    this.sessionState.set(null);
    this.getLocalStorage()?.removeItem(this.storageKey);
  }

  private setSession(response: LoginResponse, persistSession: boolean): void {
    this.sessionState.set(response);

    const storage = this.getLocalStorage();
    if (!storage) {
      return;
    }

    if (persistSession) {
      storage.setItem(this.storageKey, JSON.stringify(response));
    } else {
      storage.removeItem(this.storageKey);
    }
  }

  private readStoredSession(): LoginResponse | null {
    const storage = this.getLocalStorage();
    if (!storage) {
      return null;
    }

    const storedValue = storage.getItem(this.storageKey);
    if (!storedValue) {
      return null;
    }

    try {
      const parsedSession = JSON.parse(storedValue) as LoginResponse;
      if (!this.isSessionActive(parsedSession)) {
        storage.removeItem(this.storageKey);
        return null;
      }

      return parsedSession;
    } catch {
      storage.removeItem(this.storageKey);
      return null;
    }
  }

  private isSessionActive(session: LoginResponse | null): boolean {
    if (!session) {
      return false;
    }

    return Date.parse(session.expiresAt) > Date.now();
  }

  private getLocalStorage(): Storage | null {
    return typeof localStorage === 'undefined' ? null : localStorage;
  }
}
