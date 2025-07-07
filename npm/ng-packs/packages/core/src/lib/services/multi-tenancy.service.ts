import { Injectable, inject } from '@angular/core';
import { map, switchMap } from 'rxjs/operators';
import { AbpTenantService } from '../proxy/pages/abp/multi-tenancy';
import {
  CurrentTenantDto,
  FindTenantResultDto,
} from '../proxy/volo/abp/asp-net-core/mvc/multi-tenancy/models';
import { TENANT_KEY } from '../tokens/tenant-key.token';
import { ConfigStateService } from './config-state.service';
import { RestService } from './rest.service';
import { SessionStateService } from './session-state.service';

@Injectable({ providedIn: 'root' })
export class MultiTenancyService {
  private restService = inject(RestService);
  private sessionState = inject(SessionStateService);
  private tenantService = inject(AbpTenantService);
  private configStateService = inject(ConfigStateService);
  tenantKey = inject(TENANT_KEY);

  domainTenant: CurrentTenantDto | null = null;

  isTenantBoxVisible = true;

  apiName = 'abp';

  private setTenantToState = (tenant: FindTenantResultDto) => {
    this.sessionState.setTenant({ id: tenant.tenantId, name: tenant.name, isAvailable: true });
    return this.configStateService.refreshAppState().pipe(map(_ => tenant));
  };

  /** Inserted by Angular inject() migration for backwards compatibility */
  constructor(...args: unknown[]);

  constructor() { }

  setTenantByName(tenantName: string) {
    return this.tenantService
      .findTenantByName(tenantName)
      .pipe(switchMap(this.setTenantToState));
  }

  setTenantById(tenantId: string) {
    return this.tenantService
      .findTenantById(tenantId)
      .pipe(switchMap(this.setTenantToState));
  }
}
