import { NgModule } from '@angular/core';
import { LocalizationPipe } from './pipes/localization.pipe';
import { LazyLocalizationPipe } from './pipes';

@NgModule({
  imports: [LazyLocalizationPipe],
  exports: [LocalizationPipe, LazyLocalizationPipe],
  declarations: [LocalizationPipe],
})
export class LocalizationModule {}
