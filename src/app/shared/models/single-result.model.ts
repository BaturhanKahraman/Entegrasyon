import { Result } from "./result.model";

export interface SingleResult<T> extends Result{
    data:T;
}