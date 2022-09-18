import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, Observable, tap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class ResponseInterceptorService implements HttpInterceptor {

  constructor(private snackBar: MatSnackBar,private authService:AuthService) { }
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      //tap(x=>this.checkLogOut(x)),
      catchError(this.handleError.bind(this))
    )
  }
  
  private handleError(errorRes:HttpErrorResponse){
    let errorMessage = 'Bir hata meydana geldi.';
    console.log(errorRes);
    const logOutStatus =errorRes.headers.get('MustLogOut');
    if(logOutStatus)
      this.authService.logout();

    if (errorRes.error) {
      if(errorRes.statusText ==='Unknown Error')
        errorMessage = 'Sunucuya bağlanılamadı!';
      else{
        errorMessage=errorRes.error;
      }
    }
    this.snackBar.open(errorMessage,"Tamam",{duration:5000});
    return throwError(()=>new Error(errorMessage));
  }
  // private checkLogOut(httpEvent:HttpEvent<any>){
  //   console.log('evnt');
  //   console.log(httpEvent);
  //   if(httpEvent.type === 0){
  //     return;
  //   }   
  //   console.log('evnt');
  //   console.log(httpEvent);
  //   if(httpEvent instanceof HttpResponse){
  //     console.log(httpEvent.headers.get('MustLogOut'));
  //   }
  //   // const mustLogOut =req.headers.get('MustLogOut');
  //   // console.log(mustLogOut);
  //   // if(mustLogOut){ 
  //   //     console.log('içeri girdi');
  //   //     this.authService.logout();
  //   // }
  // }
}
