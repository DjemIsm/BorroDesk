import { Injectable, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { LanguageService } from './language.service';

@Injectable()
export class TranslatedTitleStrategy extends TitleStrategy {
  private readonly i18n = inject(LanguageService);
  private readonly title = inject(Title);
  private readonly titleKey = signal<string | null>(null);

  constructor() {
    super();

    effect(() => {
      const titleKey = this.titleKey();
      const translatedTitle = titleKey ? this.i18n.translate(titleKey) : 'BorroDesk';

      this.title.setTitle(titleKey ? `${translatedTitle} | BorroDesk` : translatedTitle);
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.titleKey.set(this.buildTitle(snapshot) ?? null);
  }
}
