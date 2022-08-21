import { NgModule } from '@angular/core';
import { AuthInterceptorService } from './interceptors/auth-interceptor.service';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthService } from './services/auth.service';
import { ResponseInterceptorService } from './interceptors/response-interceptor.service';
@NgModule({
  declarations: [],
  imports: [],
  providers: [
    AuthService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptorService,
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ResponseInterceptorService,
      multi: true,
    }
  ],
})
export class CoreModule {}
