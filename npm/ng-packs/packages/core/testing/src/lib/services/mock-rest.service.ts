import {
  ABP,
  CORE_OPTIONS,
  EnvironmentService,
  ExternalHttpClient,
  HttpErrorReporterService,
  RestService,
} from '@abp/ng.core';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class MockRestService extends RestService {
  protected options: ABP.Root;
  protected http: HttpClient;
  protected externalhttp: ExternalHttpClient;
  protected environment: EnvironmentService;

  /** Inserted by Angular inject() migration for backwards compatibility */
  constructor(...args: unknown[]);

  constructor() {
    const options = inject<ABP.Root>(CORE_OPTIONS);
    const http = inject(HttpClient);
    const externalhttp = inject(ExternalHttpClient);
    const environment = inject(EnvironmentService);

    super(options, http,externalhttp, environment, null as unknown as HttpErrorReporterService);
  
    this.options = options;
    this.http = http;
    this.externalhttp = externalhttp;
    this.environment = environment;
  }

  handleError(err: any): Observable<any> {
    return throwError(err);
  }
}
