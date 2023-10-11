import {CategoryAttributeValue } from "./category-attribute-value.model"

export interface CategoryAttribute{
    id: number;
    isAddedAfterward: boolean;
    isRequired : boolean;
    allowCustom: boolean;
    isVarianter: boolean;
    isSlicer: boolean; 
    categoryAttributeKey: string;
    categoryAttributeValues: CategoryAttributeValue[];
}