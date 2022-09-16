import { RoleClaimModel } from "./role-claim.model";

export interface RoleModel{
    id:number;
    name:string;
    claims:RoleClaimModel[]
}