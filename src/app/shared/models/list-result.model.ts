import { Result } from "./result.model";

export interface ListResult<T> extends Result{
    data:T[];
}