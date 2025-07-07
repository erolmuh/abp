import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ActivatedRoute } from '@angular/router';
import { ConfigStateService, MultiTenancyService } from '@abp/ng.core';

@Injectable()
export class AuthWrapperService {
  public readonly multiTenancy = inject(MultiTenancyService);
  private configState = inject(ConfigStateService);
  private route = inject(ActivatedRoute);

  isMultiTenancyEnabled$ = this.configState.getDeep$('multiTenancy.isEnabled');

  get enableLocalLogin$(): Observable<boolean> {
    return this.configState
      .getSetting$('Abp.Account.EnableLocalLogin')
      .pipe(map(value => value?.toLowerCase() !== 'false'));
  }

  tenantBoxKey = 'Account.TenantBoxComponent';

  get isTenantBoxVisibleForCurrentRoute() {
    return this.getMostInnerChild().data.tenantBoxVisible ?? true;
  }

  get isTenantBoxVisible() {
    return this.isTenantBoxVisibleForCurrentRoute && this.multiTenancy.isTenantBoxVisible;
  }

  private getMostInnerChild() {
    let child = this.route.snapshot;
    let depth = 0;
    const depthLimit = 10;
    while (child.firstChild && depth < depthLimit) {
      child = child.firstChild;
      depth++;
    }
    return child;
  }
}
