import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, map, of, switchMap } from 'rxjs';
import {
  AdminUserQueryParameters,
  AdminUserResponse,
  AdminUsersPagedResponse,
  AdminUsersService
} from '../../../core/admin/admin-users.service';
import { ApplicationRole, applicationRoles } from '../../../core/auth/auth.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

type AdminUserStatusFilter = 'all' | 'active' | 'inactive';

const roleTranslationKeys: Record<ApplicationRole, string> = {
  [applicationRoles.user]: 'roles.user',
  [applicationRoles.support]: 'roles.support',
  [applicationRoles.admin]: 'roles.admin'
};

@Component({
  selector: 'app-admin-users-page',
  imports: [DatePipe, ReactiveFormsModule, TranslatePipe],
  templateUrl: './admin-users-page.component.html'
})
export class AdminUsersPageComponent implements OnInit {
  private readonly adminUsersService = inject(AdminUsersService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly i18n = inject(LanguageService);

  protected readonly pageSizeOptions = [10, 25, 50, 100] as const;
  protected readonly roleOptions = computed(() => [
    { value: applicationRoles.user, label: this.roleLabel(applicationRoles.user) },
    { value: applicationRoles.support, label: this.roleLabel(applicationRoles.support) },
    { value: applicationRoles.admin, label: this.roleLabel(applicationRoles.admin) }
  ]);

  protected readonly deactivatingUserId = signal<number | null>(null);
  protected readonly editingUserId = signal<number | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly formError = signal('');
  protected readonly isEditorOpen = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly pageNumber = signal(1);
  protected readonly usersResponse = signal<AdminUsersPagedResponse | null>(null);

  protected readonly filterForm = this.formBuilder.group({
    search: '',
    role: '',
    status: 'all' as AdminUserStatusFilter,
    pageSize: 25
  });

  protected readonly userForm = this.formBuilder.group({
    userName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.minLength(6)]],
    isActive: true,
    roleUser: true,
    roleSupport: false,
    roleAdmin: false
  });

  protected readonly users = computed(() => this.usersResponse()?.items ?? []);
  protected readonly totalPages = computed(() => this.usersResponse()?.totalPages ?? 0);
  protected readonly hasPreviousPage = computed(() => this.pageNumber() > 1);
  protected readonly hasNextPage = computed(() => {
    const totalPages = this.totalPages();

    return totalPages > 0 && this.pageNumber() < totalPages;
  });
  protected readonly isEditMode = computed(() => this.editingUserId() !== null);
  protected readonly editorTitle = computed(() => this.isEditMode()
    ? this.i18n.translate('adminUsers.editUser')
    : this.i18n.translate('adminUsers.createUser'));
  protected readonly saveLabel = computed(() => {
    if (this.isSaving()) {
      return this.i18n.translate('adminUsers.saving');
    }

    return this.isEditMode()
      ? this.i18n.translate('adminUsers.saveChanges')
      : this.i18n.translate('adminUsers.createUser');
  });
  protected readonly resultSummary = computed(() => {
    const response = this.usersResponse();
    if (!response || response.totalCount === 0) {
      return this.i18n.translate('adminUsers.resultZero');
    }

    const start = (response.pageNumber - 1) * response.pageSize + 1;
    const end = Math.min(response.pageNumber * response.pageSize, response.totalCount);

    return this.i18n.translate('adminUsers.resultRange', {
      end,
      start,
      total: response.totalCount
    });
  });

  ngOnInit(): void {
    this.restoreQueryParams();
    this.loadUsers();
  }

  protected applyFilters(): void {
    this.pageNumber.set(1);
    this.loadUsers();
  }

  protected resetFilters(): void {
    this.filterForm.reset({
      search: '',
      role: '',
      status: 'all',
      pageSize: 25
    });
    this.pageNumber.set(1);
    this.loadUsers();
  }

  protected goToPage(pageNumber: number): void {
    const totalPages = this.totalPages();
    const nextPage = Math.max(1, totalPages > 0 ? Math.min(pageNumber, totalPages) : 1);

    if (nextPage === this.pageNumber()) {
      return;
    }

    this.pageNumber.set(nextPage);
    this.loadUsers();
  }

  protected openCreateForm(): void {
    this.editingUserId.set(null);
    this.formError.set('');
    this.userForm.reset({
      userName: '',
      email: '',
      password: '',
      isActive: true,
      roleUser: true,
      roleSupport: false,
      roleAdmin: false
    });
    this.isEditorOpen.set(true);
  }

  protected editUser(user: AdminUserResponse): void {
    this.editingUserId.set(user.id);
    this.formError.set('');
    this.userForm.reset({
      userName: user.userName ?? '',
      email: user.email ?? '',
      password: '',
      isActive: user.isActive,
      roleUser: user.roles.includes(applicationRoles.user),
      roleSupport: user.roles.includes(applicationRoles.support),
      roleAdmin: user.roles.includes(applicationRoles.admin)
    });
    this.isEditorOpen.set(true);
  }

  protected cancelEditor(): void {
    this.isEditorOpen.set(false);
    this.editingUserId.set(null);
    this.formError.set('');
  }

  protected submitUser(): void {
    this.formError.set('');
    const password = this.userForm.controls.password.value.trim();

    if (!this.isEditMode() && !password) {
      this.userForm.controls.password.setErrors({ required: true });
    }

    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    const rawUser = this.userForm.getRawValue();
    const roles = this.selectedRoles();
    const userId = this.editingUserId();

    this.isSaving.set(true);

    const saveRequest = userId
      ? this.adminUsersService
        .updateUser(userId, {
          userName: rawUser.userName.trim(),
          email: rawUser.email.trim(),
          isActive: rawUser.isActive,
          roles
        })
        .pipe(
          switchMap((savedUser) => password
            ? this.adminUsersService
              .resetUserPassword(savedUser.id, { password })
              .pipe(map(() => savedUser))
            : of(savedUser))
        )
      : this.adminUsersService.createUser({
        userName: rawUser.userName.trim(),
        email: rawUser.email.trim(),
        password,
        isActive: rawUser.isActive,
        roles
      });

    saveRequest.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: () => {
        this.cancelEditor();
        this.loadUsers();
      },
      error: (error: unknown) => {
        this.formError.set(this.resolveAdminUserError(error, this.i18n.translate('adminUsers.saveFailed')));
      }
    });
  }

  protected deactivateUser(user: AdminUserResponse): void {
    const label = this.userLabel(user);
    if (!confirm(this.i18n.translate('adminUsers.deactivateConfirm', { user: label }))) {
      return;
    }

    this.errorMessage.set('');
    this.deactivatingUserId.set(user.id);
    this.adminUsersService
      .deactivateUser(user.id)
      .pipe(finalize(() => this.deactivatingUserId.set(null)))
      .subscribe({
        next: () => {
          if (this.editingUserId() === user.id) {
            this.cancelEditor();
          }

          this.loadUsers();
        },
        error: (error: unknown) => {
          this.errorMessage.set(this.resolveAdminUserError(
            error,
            this.i18n.translate('adminUsers.deactivateFailed')
          ));
        }
      });
  }

  protected userLabel(user: AdminUserResponse): string {
    return user.userName || user.email || this.i18n.translate('common.userNumber', { id: user.id });
  }

  protected roleLabel(role: ApplicationRole): string {
    const translationKey = roleTranslationKeys[role];

    return translationKey ? this.i18n.translate(translationKey) : role;
  }

  protected rolesLabel(user: AdminUserResponse): string {
    if (user.roles.length === 0) {
      return this.i18n.translate('common.noRole');
    }

    return user.roles.map((role) => this.roleLabel(role)).join(', ');
  }

  protected statusLabel(isActive: boolean): string {
    return isActive
      ? this.i18n.translate('adminUsers.active')
      : this.i18n.translate('adminUsers.inactive');
  }

  protected statusClass(isActive: boolean): string {
    return isActive ? 'pill-badge user-active' : 'pill-badge user-inactive';
  }

  protected lastActivity(user: AdminUserResponse): string {
    return user.updatedAt ?? user.createdAt;
  }

  protected trackUser(_index: number, user: AdminUserResponse): number {
    return user.id;
  }

  private loadUsers(): void {
    const query = this.buildQuery();

    this.syncQueryParams(query);
    this.errorMessage.set('');
    this.isLoading.set(true);

    this.adminUsersService
      .getUsers(query)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.usersResponse.set(response);

          if (response.totalPages > 0 && response.pageNumber > response.totalPages) {
            this.pageNumber.set(response.totalPages);
            this.loadUsers();
          }
        },
        error: (error: unknown) => {
          this.usersResponse.set(null);
          this.errorMessage.set(this.resolveAdminUserError(error, this.i18n.translate('adminUsers.loadFailed')));
        }
      });
  }

  private buildQuery(): AdminUserQueryParameters {
    const rawFilters = this.filterForm.getRawValue();
    const search = rawFilters.search.trim();
    const query: AdminUserQueryParameters = {
      pageNumber: this.pageNumber(),
      pageSize: Number(rawFilters.pageSize)
    };

    if (search) {
      query.search = search;
    }

    if (this.isApplicationRole(rawFilters.role)) {
      query.role = rawFilters.role;
    }

    if (rawFilters.status === 'active') {
      query.isActive = true;
    }

    if (rawFilters.status === 'inactive') {
      query.isActive = false;
    }

    return query;
  }

  private selectedRoles(): ApplicationRole[] {
    const rawUser = this.userForm.getRawValue();
    const roles: ApplicationRole[] = [];

    if (rawUser.roleUser) {
      roles.push(applicationRoles.user);
    }

    if (rawUser.roleSupport) {
      roles.push(applicationRoles.support);
    }

    if (rawUser.roleAdmin) {
      roles.push(applicationRoles.admin);
    }

    return roles.length > 0 ? roles : [applicationRoles.user];
  }

  private restoreQueryParams(): void {
    const queryParams = this.route.snapshot.queryParamMap;
    const pageSize = this.parsePositiveInteger(queryParams.get('pageSize'), 25);
    const pageNumber = this.parsePositiveInteger(queryParams.get('pageNumber'), 1);
    const role = queryParams.get('role');
    const status = queryParams.get('status');

    this.filterForm.patchValue({
      search: queryParams.get('search') ?? '',
      role: this.isApplicationRole(role) ? role : '',
      status: this.isStatusFilter(status) ? status : 'all',
      pageSize: this.pageSizeOptions.includes(pageSize as (typeof this.pageSizeOptions)[number])
        ? pageSize
        : 25
    });
    this.pageNumber.set(pageNumber);
  }

  private syncQueryParams(query: AdminUserQueryParameters): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: query.search || null,
        role: query.role || null,
        status: this.filterForm.controls.status.value === 'all'
          ? null
          : this.filterForm.controls.status.value,
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

  private isApplicationRole(value: string | null): value is ApplicationRole {
    return value === applicationRoles.user
      || value === applicationRoles.support
      || value === applicationRoles.admin;
  }

  private isStatusFilter(value: string | null): value is AdminUserStatusFilter {
    return value === 'all' || value === 'active' || value === 'inactive';
  }

  private resolveAdminUserError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return this.i18n.translate('common.apiUnavailable');
      }

      if (error.status === 401) {
        return this.i18n.translate('adminUsers.sessionExpired');
      }

      if (error.status === 403) {
        return this.i18n.translate('adminUsers.permissionDenied');
      }

      if (error.status === 404) {
        return this.i18n.translate('adminUsers.userNotFound');
      }

      return error.error?.detail || error.error?.title || fallback;
    }

    return fallback;
  }
}
