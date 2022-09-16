import { BranchOfficeModel } from "./branch-office.model";

export class BranchOfficeDetailModel implements BranchOfficeModel{
    id: number;
    createdAt: Date;
    name: string;
    userCount:number;
}