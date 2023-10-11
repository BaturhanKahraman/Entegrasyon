"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CategoryAttributeAddManager = void 0;
var attributeTemplate = function (index, categoryAttribute, attrLabels) { return "\n    <div class=\"form-group mb-3\">\n        <div class=\"row attribute-row\" data-index=\"".concat(index, "\">\n            <div class=\"col-md-1\">\n                <label for=\"CategoryAttributeList_").concat(index, "__IsRequired\" class=\"new-control new-checkbox checkbox-primary\">\n                    <input ").concat((categoryAttribute === null || categoryAttribute === void 0 ? void 0 : categoryAttribute.isRequired) ? "checked" : "", " class=\"new-control-input\" id=\"CategoryAttributeList_").concat(index, "__IsRequired\" name=\"CategoryAttributeList[").concat(index, "].IsRequired\" type=\"checkbox\" />\n                    <span class=\"new-control-indicator\"></span>\n                    ").concat(attrLabels.isRequiredLabel, "\n                </label>\n            </div>\n            <div class=\"col-md-1\">\n                <label for=\"CategoryAttributeList_").concat(index, "__AllowCustom\"  class=\"new-control new-checkbox checkbox-primary\">\n                    <input ").concat((categoryAttribute === null || categoryAttribute === void 0 ? void 0 : categoryAttribute.allowCustom) ? "checked" : "", "  id=\"CategoryAttributeList_").concat(index, "__AllowCustom\" class=\"new-control-input allow-custom-checkbox\" name=\"CategoryAttributeList[").concat(index, "].AllowCustom\" type=\"checkbox\" />\n                    <span class=\"new-control-indicator\"></span>\n                    ").concat(attrLabels.allowCustomLabel, "\n                </label>\n            </div>\n            <div class=\"col-md-1\">\n                <label for=\"CategoryAttributeList_").concat(index, "__IsVarianter\" class=\"new-control new-checkbox checkbox-primary\">\n                    <input ").concat((categoryAttribute === null || categoryAttribute === void 0 ? void 0 : categoryAttribute.isVarianter) ? "checked" : "", " id=\"CategoryAttributeList_").concat(index, "__IsVarianter\" class=\"new-control-input\" name=\"CategoryAttributeList[").concat(index, "].IsVarianter\" type=\"checkbox\" />\n                    <span class=\"new-control-indicator\"></span>\n                    ").concat(attrLabels.isVarianterLabel, "\n                </label>\n            </div>\n            <div class=\"col-md-2\">\n                <label for=\"CategoryAttributeList_").concat(index, "__IsSlicer\" class=\"new-control new-checkbox checkbox-primary\">\n                    <input ").concat((categoryAttribute === null || categoryAttribute === void 0 ? void 0 : categoryAttribute.isSlicer) ? "checked" : "", " id=\"CategoryAttributeList_").concat(index, "__IsSlicer\" class=\"new-control-input\" name=\"CategoryAttributeList[").concat(index, "].IsSlicer\" type=\"checkbox\" />\n                    <span class=\"new-control-indicator\"></span>\n                    ").concat(attrLabels === null || attrLabels === void 0 ? void 0 : attrLabels.isSlicerLabel, "\n                </label>\n            </div>                 \n            <div class=\"col-md-4\">\n                <input value=\"").concat(categoryAttribute === null || categoryAttribute === void 0 ? void 0 : categoryAttribute.categoryAttributeKey, "\" name=\"CategoryAttributeList[").concat(index, "].CategoryAttributeKey\" type=\"text\" class=\"form-control\" />\n            </div>\n            <div class=\"col-md-3\">\n                <button type=\"button\" class=\"btn btn-warning btn-value-add\" style=\"width:140px\">").concat(attrLabels.addButtonLabel, "\n                <span class=\"value-count\">\n                ").concat(categoryAttribute.categoryAttributeValues.length > 0 ? "(" + categoryAttribute.categoryAttributeValues.length + ")" : "", "\n                </span> </button>\n                <button type=\"button\" class=\"btn btn-danger remove-btn\">").concat(attrLabels.removeButtonLabel, "</button>\n            </div>\n        </div>\n    </div>\n"); };
var CategoryAttributeAddManager = /** @class */ (function () {
    function CategoryAttributeAddManager(categoryAttributes) {
        this.setCategoryAttributes(categoryAttributes);
        this.initializeListeners();
    }
    CategoryAttributeAddManager.prototype.setCategoryAttributes = function (attributes) {
        console.log("attributes", attributes);
        console.log("category attributes", this.categoryAttributes);
        this.categoryAttributes = attributes || [];
        console.log("category attributes", this.categoryAttributes);
    };
    CategoryAttributeAddManager.prototype.addCategory = function () {
        console.log(this.categoryAttributes);
        var categoryAttr = {
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
    };
    CategoryAttributeAddManager.prototype.renderCategories = function () {
        var _this = this;
        var categoryAttributesContainer = document.getElementById("attr-container");
        if (categoryAttributesContainer) {
            categoryAttributesContainer.innerHTML = "";
            console.log(this.categoryAttributes);
            this.categoryAttributes.forEach(function (catAttr, index) {
                var attributeHtml = attributeTemplate(index, catAttr, _this.attributeSetting);
                categoryAttributesContainer.innerHTML += attributeHtml;
            });
        }
    };
    CategoryAttributeAddManager.prototype.setAttributeSettings = function (settings) {
        this.attributeSetting = settings;
    };
    CategoryAttributeAddManager.prototype.initializeListeners = function () {
        var _this = this;
        this.addDeleteButtonListener();
        this.addCheckboxListener();
        document.querySelector("#add-attribute-btn").addEventListener('click', function () { return _this.addCategory(); });
    };
    CategoryAttributeAddManager.prototype.addDeleteButtonListener = function () {
        var _this = this;
        var deleteAttrBtns = document.querySelectorAll(".delete-attr-btn");
        deleteAttrBtns.forEach(function (button) {
            button.addEventListener("click", function () {
                var btn = button;
                var index = parseInt(btn.dataset.index || "0");
                _this.deleteCategory(index);
            });
        });
    };
    CategoryAttributeAddManager.prototype.addValueListener = function () {
        var addValueBtns = document.querySelectorAll(".btn-value-add");
        addValueBtns.forEach(function (btn, index) {
            var dataIndex = +btn.parentElement.parentElement.dataset.index;
            $("#addValueModal").modal("show");
        });
    };
    CategoryAttributeAddManager.prototype.addCheckboxListener = function () {
        var attributeRows = document.querySelectorAll('.attribute-row');
        attributeRows.forEach(function (row) {
            var isSlicerCheckbox = row.querySelector('[name$=".IsSlicer"]');
            var isVarianterCheckbox = row.querySelector('[name$=".IsVarianter"]');
            isSlicerCheckbox.addEventListener('change', function () {
                if (isSlicerCheckbox.checked && isVarianterCheckbox.checked) {
                    isVarianterCheckbox.checked = false;
                }
            });
            isVarianterCheckbox.addEventListener('change', function () {
                if (isVarianterCheckbox.checked && isSlicerCheckbox.checked) {
                    isSlicerCheckbox.checked = false;
                }
            });
        });
    };
    CategoryAttributeAddManager.prototype.deleteCategory = function (index) {
        if (index >= 0 && index < this.categoryAttributes.length) {
            if (!confirm("Ger�ekten silmek istedi�inize emin misiniz ?")) {
                return;
            }
            this.categoryAttributes.splice(index, 1);
            this.renderCategories();
        }
    };
    return CategoryAttributeAddManager;
}());
exports.CategoryAttributeAddManager = CategoryAttributeAddManager;
//# sourceMappingURL=category-attribute-add-manager.js.map