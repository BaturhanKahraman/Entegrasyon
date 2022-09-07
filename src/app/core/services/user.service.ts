import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, shareReplay, tap } from 'rxjs';
import { Result } from 'src/app/shared/models/result.model';
import { UserDetailModel } from 'src/app/shared/models/user-detail.model';
import { environment } from 'src/environments/environment';

@Injectable()
export class UserService {
  private url = environment.url + 'users/';

  constructor(private http: HttpClient) {}

 
  getUserDetail():Observable<UserDetailModel[]> {
    const fullUrl = this.url + 'GetUserDetailList';
    return this.http.get<Result<UserDetailModel[]>>(fullUrl)
    .pipe(map((x) => x.data))
  }
  getUserCount():Observable<number> {
    const fullUrl = this.url + 'GetUserCount';
    return this.http.get<Result<number>>(fullUrl).pipe(
      map((x) => x.data))
  }
}
