import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService, TranslationParams } from './language.service';

@Pipe({
  name: 'translate',
  pure: false
})
export class TranslatePipe implements PipeTransform {
  private readonly languageService = inject(LanguageService);

  transform(key: string, params?: TranslationParams): string {
    return this.languageService.translate(key, params);
  }
}
