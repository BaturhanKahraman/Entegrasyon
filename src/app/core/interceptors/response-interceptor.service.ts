import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, Observable, throwError } from 'rxjs';

@Injectable()
export class ResponseInterceptorService implements HttpInterceptor {

  constructor(private snackBar: MatSnackBar) { }
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError(this.handleError.bind(this))
    )
  }
  
  private handleError(errorRes:HttpErrorResponse){
    let errorMessage = 'Bir hata meydana geldi.';        
    if (errorRes.error.message) {
      errorMessage=errorRes.error.message;
    }
    this.snackBar.open(errorMessage,undefined,{duration:5000});
    return throwError(()=>new Error(errorMessage));
  }
}
