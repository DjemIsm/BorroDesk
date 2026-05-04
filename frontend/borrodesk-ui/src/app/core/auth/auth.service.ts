import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';

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
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'borrodesk.auth';

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', request).pipe(
      tap((response) => this.saveSession(response))
    );
  }

  getSession(): LoginResponse | null {
    const storage = this.getLocalStorage();
    if (!storage) {
      return null;
    }

    const storedValue = storage.getItem(this.storageKey);
    if (!storedValue) {
      return null;
    }

    try {
      return JSON.parse(storedValue) as LoginResponse;
    } catch {
      storage.removeItem(this.storageKey);
      return null;
    }
  }

  clearSession(): void {
    this.getLocalStorage()?.removeItem(this.storageKey);
  }

  private saveSession(response: LoginResponse): void {
    this.getLocalStorage()?.setItem(this.storageKey, JSON.stringify(response));
  }

  private getLocalStorage(): Storage | null {
    return typeof localStorage === 'undefined' ? null : localStorage;
  }
}
