import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';
import { Result } from 'src/app/shared/models/result.model';
import { environment } from 'src/environments/environment';

@Injectable()
export class BranchOfficeService {
  constructor(private http: HttpClient) {}
  private url = environment.url + 'branches/';
  getBranches():Observable<BranchOfficeModel[]> {
    const fullUrl = this.url + 'GetBranches';
    return this.http.get<Result<BranchOfficeModel[]>>(fullUrl)
      .pipe(map(x=>x.data));
  }
}
