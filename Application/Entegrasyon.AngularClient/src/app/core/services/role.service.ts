import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, tap } from 'rxjs';
import { ListResult } from 'src/app/shared/models/list-result.model';
import { Result } from 'src/app/shared/models/result.model';
import { RoleAddDto } from 'src/app/shared/models/role-add.model';
import { RoleClaimModel } from 'src/app/shared/models/role-claim.model';
import { RoleEditDto } from 'src/app/shared/models/role-edit.model';
import { RoleModel } from 'src/app/shared/models/role.model';
import { environment } from 'src/environments/environment';
import { AuthService } from './auth.service';

@Injectable()
export class RoleService {
  private roleSubject = new BehaviorSubject<RoleModel[]>([]);
  private claimSubject= new BehaviorSubject<RoleClaimModel[]>([]);
  claims$ = this.claimSubject.asObservable();
  roles$ = this.roleSubject.asObservable();
  url = environment.url + 'roles/';
  constructor(private http: HttpClient, private authService: AuthService) {}

  init() {
    this.getRoles().subscribe((x) => this.roleSubject.next(x));
    this.getAllClaims().subscribe(x=>this.claimSubject.next(x));
  }
  getClaims() {}

  getRoles(): Observable<RoleModel[]> {
    const fullUrl = this.url + 'GetRoles';
    return this.http
      .get<ListResult<RoleModel>>(fullUrl)
      .pipe(map((x) => x.data));
  }

  addRole(roleAddDto:RoleAddDto):Observable<Result> {
    const fullUrl = this.url + 'AddRole';
    return this.http.post<Result>(fullUrl,roleAddDto);
  }
  updateRole(roleEditDto:RoleEditDto):Observable<Result> {
    const fullUrl = this.url + 'UpdateRole';
    return this.http.post<Result>(fullUrl,roleEditDto);
  }
  deleteRole(id:number){
    const fullUrl = this.url + 'DeleteRole';
    const param =new HttpParams().set('id',id);
    return this.http.delete<Result>(fullUrl,{params:param});

  }

  getAllClaims():Observable<RoleClaimModel[]>{
    const fullUrl = this.url + 'GetRoleClaims';
    return this.http.get<ListResult<RoleClaimModel>>(fullUrl).pipe(map(x=>x.data));
  }
  getClaimsFromToken():string[] {
    const token = this.authService.user.getValue()!.token
    if(!token)
      return [];
    const jwt = this.authService.parseJwt(token);
    const roleKey = Object.keys(jwt).find(x=>x.endsWith('role'));
    return jwt[roleKey!];
  }
}
