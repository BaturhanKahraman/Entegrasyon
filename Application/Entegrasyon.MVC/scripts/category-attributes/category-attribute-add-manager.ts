import { CategoryAttribute } from "../models/category-attribute.model"
interface AttributeLocalization {
    isRequiredLabel: string;
    allowCustomLabel: string;
    isVarianterLabel: string;
    isSlicerLabel: string;
    addButtonLabel: string;
    removeButtonLabel: string;
}
const attributeTemplate = (index: number, categoryAttribute: CategoryAttribute, attrLabels: AttributeLocalization) => `
    <div class="form-group mb-3">
        <div class="row attribute-row" data-index="${index}">
            <div class="col-md-1">
                <label for="CategoryAttributeList_${index}__IsRequired" class="new-control new-checkbox checkbox-primary">
                    <input ${categoryAttribute?.isRequired ? "checked" : ""} class="new-control-input" id="CategoryAttributeList_${index}__IsRequired" name="CategoryAttributeList[${index}].IsRequired" type="checkbox" />
                    <span class="new-control-indicator"></span>
                    ${attrLabels.isRequiredLabel}
                </label>
            </div>
            <div class="col-md-1">
                <label for="CategoryAttributeList_${index}__AllowCustom"  class="new-control new-checkbox checkbox-primary">
                    <input ${categoryAttribute?.allowCustom ? "checked" : ""}  id="CategoryAttributeList_${index}__AllowCustom" class="new-control-input allow-custom-checkbox" name="CategoryAttributeList[${index}].AllowCustom" type="checkbox" />
                    <span class="new-control-indicator"></span>
                    ${attrLabels.allowCustomLabel}
                </label>
            </div>
            <div class="col-md-1">
                <label for="CategoryAttributeList_${index}__IsVarianter" class="new-control new-checkbox checkbox-primary">
                    <input ${categoryAttribute?.isVarianter ? "checked" : ""} id="CategoryAttributeList_${index}__IsVarianter" class="new-control-input" name="CategoryAttributeList[${index}].IsVarianter" type="checkbox" />
                    <span class="new-control-indicator"></span>
                    ${attrLabels.isVarianterLabel}
                </label>
            </div>
            <div class="col-md-2">
                <label for="CategoryAttributeList_${index}__IsSlicer" class="new-control new-checkbox checkbox-primary">
                    <input ${categoryAttribute?.isSlicer ? "checked" : ""} id="CategoryAttributeList_${index}__IsSlicer" class="new-control-input" name="CategoryAttributeList[${index}].IsSlicer" type="checkbox" />
                    <span class="new-control-indicator"></span>
                    ${attrLabels?.isSlicerLabel}
                </label>
            </div>                 
            <div class="col-md-4">
                <input value="${categoryAttribute?.categoryAttributeKey}" name="CategoryAttributeList[${index}].CategoryAttributeKey" type="text" class="form-control" />
            </div>
            <div class="col-md-3">
                <button type="button" class="btn btn-warning btn-value-add" style="width:140px">${attrLabels.addButtonLabel}
                <span class="value-count">
                ${categoryAttribute.categoryAttributeValues.length>0 ? "("+ categoryAttribute.categoryAttributeValues.length +")": ""}
                </span> </button>
                <button type="button" class="btn btn-danger remove-btn">${attrLabels.removeButtonLabel}</button>
            </div>
        </div>
    </div>
`;


export class CategoryAttributeAddManager {
    categoryAttributes: CategoryAttribute[];
    private attributeSetting: AttributeLocalization;
    constructor(categoryAttributes: CategoryAttribute[]) {
        this.setCategoryAttributes(categoryAttributes);
        this.initializeListeners();
    }

    setCategoryAttributes(attributes: CategoryAttribute[]) {
        console.log("attributes",attributes)
        console.log("category attributes",this.categoryAttributes)
        this.categoryAttributes = attributes || [];
        console.log("category attributes", this.categoryAttributes)
    }

    addCategory() {
        console.log(this.categoryAttributes)
        const categoryAttr: CategoryAttribute = {
            id: 0,
            isAddedAfterward: true,
            isRequired: false,
            allowCustom: false,
            isVarianter: false,
            isSlicer: false,
            categoryAttributeKey: "",
            categoryAttributeValues: []
        };
        this.categoryAttributes.push(categoryAttr);
        this.renderCategories();
    }
    renderCategories() {
        const categoryAttributesContainer = document.getElementById("attr-container");
        if (categoryAttributesContainer) {
            categoryAttributesContainer.innerHTML = "";
            console.log(this.categoryAttributes)
            this.categoryAttributes.forEach((catAttr, index) => {
                const attributeHtml = attributeTemplate(index, catAttr, this.attributeSetting);
                categoryAttributesContainer.innerHTML += attributeHtml;
            });
        }
    }
    setAttributeSettings(settings: AttributeLocalization) {
        this.attributeSetting = settings;
    }
    initializeListeners() {
        this.addDeleteButtonListener();
        this.addCheckboxListener();
        document.querySelector("#add-attribute-btn").addEventListener('click', () => this.addCategory())
    }
    private addDeleteButtonListener() {
        const deleteAttrBtns = document.querySelectorAll(".delete-attr-btn");
        deleteAttrBtns.forEach((button) =>{
            button.addEventListener("click", () => {
                var btn = button as HTMLElement;
                const index = parseInt(btn.dataset.index || "0");
                this.deleteCategory(index);
            });
        });
    }
    private addValueListener() {
        const addValueBtns = document.querySelectorAll(".btn-value-add");
        addValueBtns.forEach((btn, index) => {
            const dataIndex = +btn.parentElement.parentElement.dataset.index;
            $("#addValueModal").modal("show");

        });
    }
    private addCheckboxListener() {
        const attributeRows = document.querySelectorAll('.attribute-row');

        attributeRows.forEach((row) => {
            const isSlicerCheckbox = row.querySelector('[name$=".IsSlicer"]') as HTMLInputElement;
            const isVarianterCheckbox = row.querySelector('[name$=".IsVarianter"]') as HTMLInputElement;

            isSlicerCheckbox.addEventListener('change', () => {
                if (isSlicerCheckbox.checked && isVarianterCheckbox.checked) {
                    isVarianterCheckbox.checked = false;
                }
            });

            isVarianterCheckbox.addEventListener('change', () => {
                if (isVarianterCheckbox.checked && isSlicerCheckbox.checked) {
                    isSlicerCheckbox.checked = false;
                }
            });
        });
    }
    deleteCategory(index: number) {
        if (index >= 0 && index < this.categoryAttributes.length) {
            if (!confirm("Gerçekten silmek istediðinize emin misiniz ?")) {
                return;
            }
            this.categoryAttributes.splice(index, 1);
            this.renderCategories();
        }
    }
}