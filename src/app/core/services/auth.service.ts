import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { LoginModel } from 'src/app/shared/models/login.model'; 
import { Result } from 'src/app/shared/models/result.model'; 
import { User } from 'src/app/shared/models/user.model'; 
import { LoginFirstPasswordModel } from 'src/app/shared/models/login-first-password.model';
import { LoginSetPasswordModel } from 'src/app/shared/models/login-set-password.model';

class TokenResult {
  token: string;
  expiration: Date;
}
@Injectable()
export class AuthService {

  private url = environment.url+'auth/';
  private userData = "userData";
  private userSubject = new BehaviorSubject<User|null>(null);
  public user =this.userSubject.asObservable();
  constructor(private http: HttpClient, private router: Router) {
  }


  autoLogin(){
    const jwtToken =localStorage.getItem(this.userData);
    if(!jwtToken)
      return;
    this.handleAuth(jwtToken);
  }
  isAuth(){
    if(this.userSubject.getValue())
      return true;
    return false;
  }

  login(model: LoginModel) {
    return this.http.post<Result<TokenResult> | Result<LoginFirstPasswordModel>>(this.url + 'login', model)
    .pipe(
      tap((x) => {
        if((<LoginFirstPasswordModel>x.data).needsToTakePassword){
          this.router.navigate(["auth","set-password"],{queryParams:{userId:(<LoginFirstPasswordModel>x.data).userId}})
        }
      }),
      tap(x=>{
        if((<TokenResult>x.data).token){
            this.handleAuth((<TokenResult>x.data).token);
            localStorage.setItem(this.userData,(<TokenResult>x.data).token);
            this.router.navigateByUrl("/");
        }
      })
    );
  }
  setFirstPassword(loginSetPassword: LoginSetPasswordModel) {
    return this.http.post<Result<null>>(this.url + 'AssignFirstPassword',loginSetPassword);
  }
  logout(){
    localStorage.removeItem(this.userData);
    return this.router.navigate(['auth','login']);
  }
  handleAuth(token:string) {
    let jwt = this.parseJwt(token);
    let user = this.getUserFromJwt(jwt);
    this.userSubject.next(user);
  }

  getUserFromJwt(jwt:any){
    
    return new User(
      jwt["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email"],
      jwt["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"],
      jwt["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
      jwt["http://schemas.microsoft.com/ws/2008/06/identity/claims/name"] + " " +jwt["http://schemas.microsoft.com/ws/2008/06/identity/claims/surname"]
    )
  }

  parseJwt(token: string) {
    var base64Url = token.split('.')[1];
    var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    var jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(function (c) {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        })
        .join('')
    );
    return JSON.parse(jsonPayload);
  }
}
