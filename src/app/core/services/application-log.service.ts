import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApplicationLogDetail } from 'src/app/shared/models/application-log-detail.model';
import { LogAction, LogType } from 'src/app/shared/models/log.enum';
import { Paginate } from 'src/app/shared/models/paginate.model';
import { SingleResult } from 'src/app/shared/models/single-result.model';
import { environment } from 'src/environments/environment';

@Injectable()
export class ApplicationLogService {

constructor(private http:HttpClient) { }
     url = environment.url + 'Logs/'
    getPaginatedLogs(page:number=1,itemCount:number=50,logAct: LogAction =LogAction.None,logType:LogType=LogType.None)
    :Observable<SingleResult<Paginate<ApplicationLogDetail>>>{
        const fullUrl =this.url + 'GetLogDetails';
        const params = new HttpParams()
        .append('page',page)
        .append('itemCount',itemCount)
        .append('logAction',logAct)
        .append('logType',logType);
        return this.http.get<SingleResult<Paginate<ApplicationLogDetail>>>(fullUrl,{params:params});
    }
}
