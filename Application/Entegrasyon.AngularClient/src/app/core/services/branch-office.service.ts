import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, tap } from 'rxjs';
import { BranchOfficeDetailModel } from 'src/app/shared/models/branch-office-detail.model';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';
import { ListResult } from 'src/app/shared/models/list-result.model';
import { Result } from 'src/app/shared/models/result.model';
import { SingleResult } from 'src/app/shared/models/single-result.model';
import { environment } from 'src/environments/environment';

@Injectable()
export class BranchOfficeService {


  private branchSubject = new BehaviorSubject<BranchOfficeModel[]>([]);
  branches$ = this.branchSubject.asObservable();
  
  constructor(private http: HttpClient) {}
  private url = environment.url + 'branches/';

  init(){
    this.getBranches()
    .subscribe((branches) => this.branchSubject.next(branches));
  }
  getBranches():Observable<BranchOfficeModel[]> {
    const fullUrl = this.url + 'GetBranches';
    return this.http.get<ListResult<BranchOfficeModel>>(fullUrl)
      .pipe(map(x=>x.data));
  }
  editBranch(branchOffice: BranchOfficeModel) :Observable<SingleResult<BranchOfficeDetailModel>> {
    const fullUrl =this.url + 'UpdateBranch';
    
    return this.http.put<SingleResult<BranchOfficeDetailModel>>(fullUrl,branchOffice);
  }
  addBranch(name:string):Observable<Result> {
    const fullUrl =this.url + 'AddBranch';
    return this.http.post<Result>(fullUrl,{name:name});
  }
  GetBranchDetail(branchId:number):Observable<SingleResult<BranchOfficeDetailModel>>{
    const fullUrl = this.url + 'GetBranchDetail';
    return this.http.get<SingleResult<BranchOfficeDetailModel>>(fullUrl,{params:{branchId:branchId}});
  }
  deleteBranch(id: number):Observable<Result> {
    const fullUrl = this.url + 'DeleteBranch';
    return this.http.delete<Result>(fullUrl,{params:{id:id}});
  }
}
