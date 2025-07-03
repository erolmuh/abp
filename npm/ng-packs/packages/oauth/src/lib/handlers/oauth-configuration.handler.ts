import { Inject, Injectable, inject } from '@angular/core';
import { AuthConfig, OAuthService } from "angular-oauth2-oidc";
import compare from 'just-compare';
import { filter, map } from 'rxjs/operators';
import { ABP, EnvironmentService, CORE_OPTIONS } from '@abp/ng.core';

@Injectable({
  providedIn: 'root',
})
export class OAuthConfigurationHandler {
  private readonly oAuthService = inject(OAuthService);
  private readonly environmentService = inject(EnvironmentService);
  private readonly options = inject(CORE_OPTIONS);

  constructor() {
    this.listenToSetEnvironment();
  }

  private listenToSetEnvironment() {
    this.environmentService
      .createOnUpdateStream(state => state)
      .pipe(
        map(environment => environment.oAuthConfig as AuthConfig),
        filter(config => !compare(config, this.options.environment.oAuthConfig)),
      )
      .subscribe((config) => {
        this.oAuthService.configure(config);
      });
  }
}
