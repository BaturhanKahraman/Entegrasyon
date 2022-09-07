import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { exhaustMap, Observable, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptorService implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return this.authService.user.pipe(
      take(1),
      exhaustMap((x) => {
        if (!x) {
          return next.handle(req);
        }
        const modifiedReq = req.clone({
          setHeaders: { Authorization: 'Bearer ' + x.token },
        });
          return next.handle(modifiedReq);
      })
    );
  }
}
