import { LogAction, LogType } from "./log.enum";
export class ApplicationLogDetail{
    createdAt:Date;
    userInfos:string;
    ipAddress:string;
    logAction:LogAction;
    logType:LogType;
    content:string;
    id:number;
}