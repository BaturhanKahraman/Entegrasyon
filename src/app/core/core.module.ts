import { NgModule } from '@angular/core';
import { AuthInterceptorService } from './interceptors/auth-interceptor.service';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthService } from './services/auth.service';
import { ResponseInterceptorService } from './interceptors/response-interceptor.service';
import { UserService } from './services/user.service';
import { StoreService } from './services/store.service';
import { BranchOfficeService } from './services/branch-office.service';
import { RoleService } from './services/role.service';
import { ApplicationLogService } from './services/application-log.service';
@NgModule({
  declarations: [],
  imports: [],
  providers: [
    AuthService,
    UserService,
    StoreService,
    BranchOfficeService,
    RoleService,
    ApplicationLogService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptorService,
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ResponseInterceptorService,
      multi: true,
    },
  ],
})
export class CoreModule {}
