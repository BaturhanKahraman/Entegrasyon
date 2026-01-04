"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var categoryList = [];
function addCategory() {
    var lastId = 0;
    if (categoryList.length > 0)
        lastId = categoryList[categoryList.length - 1].id;
    var categoryAttr = {
        id: lastId,
        IsAddedAfterward: true,
        IsRequired: false,
        AllowCustom: false,
        IsVarianter: false,
        IsSlicer: false,
        CategoryAttributeKey: ""
    };
    categoryList.push(categoryAttr);
    renderCategories();
}
function deleteCategory(index) {
    if (index >= 0 && index < categoryList.length) {
        categoryList.splice(index, 1);
        renderCategories();
    }
}
function renderCategories() {
    $("#category-attributes").empty();
    for (var i = 0; i < categoryList.length; i++) {
        $("#category-attributes").append("\n        <div> ".concat(i, ". eleman Attribute : ").concat(categoryList[i].id, " \n        <button class=\"btn btn-danger delete-attr-btn\" data-index=\"").concat(i, "\">Sil</button> <div>\n        "));
    }
}
$("#category-attributes").on("click", ".delete-attr-btn", function () {
    var confirmed = confirm("Ger�ekten silmek istedi�inize emin misiniz ?");
    if (!confirmed)
        return;
    var index = $(this).data("index");
    deleteCategory(index);
    renderCategories();
});
renderCategories();
//# sourceMappingURL=create.js.map