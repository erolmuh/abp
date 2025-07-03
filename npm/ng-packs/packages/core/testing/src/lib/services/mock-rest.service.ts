import {
  ABP,
  CORE_OPTIONS,
  EnvironmentService,
  ExternalHttpClient,
  HttpErrorReporterService,
  RestService,
} from '@abp/ng.core';
import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class MockRestService extends RestService {
  protected readonly options = inject(CORE_OPTIONS);
  protected readonly http = inject(HttpClient);
  protected readonly externalhttp = inject(ExternalHttpClient);
  protected readonly environment = inject(EnvironmentService);

  constructor() {
    super();
  }

  handleError(err: any): Observable<any> {
    return throwError(err);
  }
}
