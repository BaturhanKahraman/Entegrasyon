import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, shareReplay, tap } from 'rxjs';
import { ListResult } from 'src/app/shared/models/list-result.model';
import { Result } from 'src/app/shared/models/result.model';
import { SingleResult } from 'src/app/shared/models/single-result.model';
import { UserAddModel } from 'src/app/shared/models/user-add.model';
import { UserDetailModel } from 'src/app/shared/models/user-detail.model';
import { environment } from 'src/environments/environment';

@Injectable()
export class UserService {
  private url = environment.url + 'users/';
  private usersSubject = new BehaviorSubject<UserDetailModel[]>([]);
  usersDetails$ = this.usersSubject.asObservable();
  private userCount: BehaviorSubject<number> = new BehaviorSubject(0);
  userCount$ = this.userCount.asObservable();
  constructor(private http: HttpClient) {}

  init() {
    this.getUserDetail().subscribe((x) => this.usersSubject.next(x));
    this.getUserCount().subscribe((x) => this.userCount.next(x));
  }
  getUserDetail(): Observable<UserDetailModel[]> {
    const fullUrl = this.url + 'GetUserDetailList';
    return this.http
      .get<ListResult<UserDetailModel>>(fullUrl)
      .pipe(map((x) => x.data));
  }
  getUserCount(): Observable<number> {
    const fullUrl = this.url + 'GetUserCount';
    return this.http
      .get<SingleResult<number>>(fullUrl)
      .pipe(map((x) => x.data));
  }
  addUser(userAddDto:UserAddModel):Observable<Result>{
    const fullUrl=this.url + 'AddUser';
    return this.http.post<Result>(fullUrl,userAddDto);
  }
}
