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
import { SingleResult } from 'src/app/shared/models/single-result.model';

class TokenResult {
  token: string;
  expiresAt: Date;
}
@Injectable()
export class AuthService {
  private url = environment.url + 'auth/';
  private userData = 'userData';
  user: BehaviorSubject<User | null>= new BehaviorSubject<User | null>(null);
  private tokenExpirationTimer: any;
  constructor(private http: HttpClient, private router: Router) {}

  autoLogin() {
    const savedUserData: {
      email: string;
      id: string;
      name: string;
      surname: string;
      userName: string;
      _token: string;
      _tokenExpirationDate: Date;
    } = JSON.parse(localStorage.getItem(this.userData)!);
    if (!savedUserData) return;
    const loadedUser: User = new User(
      savedUserData.email,
      savedUserData.id,
      savedUserData.name,
      savedUserData.surname,
      savedUserData.userName,
      savedUserData._token,
      savedUserData._tokenExpirationDate
    );
    if (savedUserData._token) {
      this.user.next(loadedUser);
      
      this.autoLogout(savedUserData._tokenExpirationDate);
    }
  }

  login(model: LoginModel) {
    return this.http
      .post<SingleResult<TokenResult> | SingleResult<LoginFirstPasswordModel>>(
        this.url + 'login',
        model
      )
      .pipe(
        tap((x) => {
          if ((<LoginFirstPasswordModel>x.data).needsToTakePassword) {
            this.router.navigate(['auth', 'set-password'], {
              queryParams: { userId: (<LoginFirstPasswordModel>x.data).userId },
            });
          }
        }),
        tap((x) => {
          if ((<TokenResult>x.data).token) {
            const resultToken = (<TokenResult>x.data);
            this.handleAuth(resultToken);
            this.router.navigateByUrl('/');
          }
        })
      );
  }
  setFirstPassword(loginSetPassword: LoginSetPasswordModel) {
    return this.http.post<Result>(
      this.url + 'AssignFirstPassword',
      loginSetPassword
    );
  }
  autoLogout(expiration: Date) {
    const diffDate = new Date(expiration).getTime() - Date.now();
    const diff = diffDate > 2147483647 ? 2147483647 :diffDate
    this.tokenExpirationTimer = setTimeout(() => {
      this.logout();
    },diff);
  }
  logout() {
    this.user.next(null);
    localStorage.removeItem(this.userData);
    if (this.tokenExpirationTimer) {
     clearTimeout(this.tokenExpirationTimer);
    }
    this.tokenExpirationTimer = null;
    this.router.navigate(['auth', 'login']);
  }

  handleAuth(token: TokenResult) {
    let jwt = this.parseJwt(token.token);
    let user = this.getUserFromJwt(jwt,token.token, token.expiresAt);
    this.user.next(user);
    this.autoLogout(token.expiresAt)
    localStorage.setItem(this.userData, JSON.stringify(user));
  }
  getUserFromJwt(decodedJwt: any,token:string, expiration: Date): User {
    const keys = Object.keys(decodedJwt);
    const name = decodedJwt[keys.find((jwtKey) => jwtKey.endsWith('name'))!];
    const surname = decodedJwt[keys.find((jwtKey) => jwtKey.endsWith('surname'))!];
    const id = decodedJwt[keys.find((jwtKey) => jwtKey.endsWith('nameidentifier'))!];
    const email = decodedJwt[keys.find((jwtKey) => jwtKey.endsWith('emailaddress'))!];
    const givenName = decodedJwt[keys.find((jwtKey) => jwtKey.endsWith('givenname'))!];
    const user = new User(email, id, name, surname, givenName, token, expiration);
    return user;
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
