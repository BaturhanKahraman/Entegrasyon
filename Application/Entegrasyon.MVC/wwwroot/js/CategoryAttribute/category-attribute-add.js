$(document).ready(function () {
    $("#add-attribute-btn").click(function () {
        var lastAttributeIndex = $(".attribute-row").last().data("index");
        if (lastAttributeIndex != 0 && !lastAttributeIndex) {
            lastAttributeIndex = 0;
        }
        else {
            lastAttributeIndex++
        }
        var attributeHtml = `
                <div class="form-group mb-3">
                    <div class="row attribute-row" data-index=${lastAttributeIndex}>
                         <div class="col-md-1">
                            <label for="CategoryAttributeList_${lastAttributeIndex}__IsRequired" class="new-control new-checkbox checkbox-primary">
                                <input type="checkbox" id="CategoryAttributeList_${lastAttributeIndex}__IsRequired" name="CategoryAttributeList[${lastAttributeIndex}].IsRequired" 
                                class="new-control-input" value="true" />
                                 <span class="new-control-indicator"></span>Zorunlu mu?
                            </label>
                            <span class="text-danger field-validation-valid" data-valmsg-for="CategoryAttributeList_${lastAttributeIndex}__IsRequired" data-valmsg-replace="true"></span>
                         </div>
                         <div class="col-md-1">
                            <label for="CategoryAttributeList_${lastAttributeIndex}__AllowCustom" class="new-control new-checkbox checkbox-primary">
                                <input type="checkbox" id="CategoryAttributeList_${lastAttributeIndex}__AllowCustom" name="CategoryAttributeList[${lastAttributeIndex}].AllowCustom" 
                                class="new-control-input allow-custom-checkbox" value="true" />
                                 <span class="new-control-indicator"></span>Normal Giriş
                            </label>
                            <span class="text-danger field-validation-valid" data-valmsg-for="CategoryAttributeList_${lastAttributeIndex}__IsVarianter" data-valmsg-replace="true"></span>
                         </div>
                        <div class="col-md-1">
                            <label for="CategoryAttributeList_${lastAttributeIndex}__IsVarianter" class="new-control new-checkbox checkbox-primary">
                                <input type="checkbox" id="CategoryAttributeList_${lastAttributeIndex}__IsVarianter" name="CategoryAttributeList[${lastAttributeIndex}].IsVarianter" 
                                class="new-control-input" value="true" />
                                 <span class="new-control-indicator"></span>Varyant
                            </label>
                            <span class="text-danger field-validation-valid" data-valmsg-for="CategoryAttributeList_${lastAttributeIndex}__IsVarianter" data-valmsg-replace="true"></span>
                         </div>
                        <div class="col-md-2">
                            <label for="CategoryAttributeList_${lastAttributeIndex}__IsSlicer" class="new-control new-checkbox checkbox-primary">
                                <input type="checkbox" id="CategoryAttributeList_${lastAttributeIndex}__IsSlicer" name="CategoryAttributeList[${lastAttributeIndex}].IsSlicer" 
                                class="new-control-input" value="true" />
                                 <span class="new-control-indicator"></span>Sayfa Bölücü
                            </label>
                            <span class="text-danger field-validation-valid" data-valmsg-for="CategoryAttributeList_${lastAttributeIndex}__IsSlicer" data-valmsg-replace="true"></span>
                         </div>
                    <div class="col-md-4">
                        <input class="form-control" type="text" data-val="true" 
                        data-val-maxlength="Özellik ismi 55 karakterden fazla olamaz." 
                        data-val-maxlength-max="55" 
                        data-val-minlength="Özellik ismi 2 karakterden az olamaz." 
                        data-val-minlength-min="2" data-val-required="Lütfen özellik için bir isim girin."
                        maxlength="55"
                        placeholder="Kategori Özellik İsmi"
                        id="CategoryAttributeList_${lastAttributeIndex}__CategoryAttributeKey" 
                        name="CategoryAttributeList[${lastAttributeIndex}].CategoryAttributeKey"/>
                        <span class="text-danger field-validation-valid"
                            data-valmsg-for="CategoryAttributeList[${lastAttributeIndex}].CategoryAttributeKey"
                            data-valmsg-replace="true"></span>
                         </div>
                    <div class="col-md-3">
                        <button type="button" class="btn btn-warning btn-value-add" style="width=78px">Değer Ekle</button>
                        <button type="button" class="btn btn-danger remove-btn">Sil</button>
                    </div>
                </div>
                </div>`;

        $(".attr-container").append(attributeHtml);
        resetValidators();
        $(".attribute-row").last().hide().slideDown();
    });

    $(document).on("click", ".remove-btn", function () {
        $(this).closest(".attribute-row").slideUp(function () {
            $(this).remove();
            reindexAttributes();
            resetValidators();
        })
    });

    function reindexAttributes() {
        $(".attr-container .attribute-row").each(function (index) {
            $(this).data("index", index);
            $(this).find("input, select").each(function () {
                var name = $(this).attr("name");
                var newName = name.replace(/\[\d+\]/g, "[" + index + "]");
                $(this).attr("name", newName);
            });
        });
        resetValidators();
    }

    function resetValidators() {
        var form = $("form")
            .removeData("validator") /* added by the raw jquery.validate plugin */
            .removeData("unobtrusiveValidation");  /* added by the jquery unobtrusive plugin*/

        $.validator.unobtrusive.parse($("form"));
    }


    $("#addExistingAttributeBtn").click(function () {
        var selectedValue = $("#existingAttributeSelect").val();
        if (selectedValue) {
            // Seçilen özelliği modele ekleyebilirsiniz
            // Örneğin: categoryAttributeList.push(selectedValue);

            // Ekledikten sonra gerekli işlemleri yapabilirsiniz
            // Örneğin: updateAttributeList();

            // Modalı kapatın
            $("#existingAttributeModal").modal("hide");
        }
    });

    updateValueCounts(); // Sayfa yüklendiğinde value-count'ları güncelle

    // Attribute row eklenip çıkarıldığında value-count'ları güncelle
    $(document).on('change', '.attribute-row input', function () {
        updateValueCounts();
    });

    function updateValueCounts() {
        $('.attribute-row').each(function () {
            var count = $(this).find('input[id$="__Name"]').length;
            if (count >0)
                $(this).find('.value-count').text('('+count+')');
        });
    }

    $(document).on('change', '.allow-custom-checkbox', function () {
        var isSelected = $(this).prop("checked");
        if (isSelected) {
            var inputs = $(this).closest('.attribute-row')
                .find('input[id*="__CategoryAttributeValues_"]');
            if (inputs.length > 0) {
                var confirmed = confirm("Eğer düz yazı girmeyi seçerseniz eklediğiniz değerler kaybolacaktır. Emin misiniz ?")
                if (!confirmed) {
                    $(this).prop("checked", false);
                    return;
                }
                inputs.remove();
            };
            $(this).parents('.attribute-row').find('.btn-value-add').prop('disabled', isSelected);
        } else {
            $(this).parents('.attribute-row').find('.btn-value-add').prop('disabled', false);
        }
    });
    $(".allow-custom-checkbox").each(function () {
        var isSelected = $(this).prop('checked');
        if (isSelected) {
            $(this).parents('.attribute-row').find('.btn-value-add').prop('disabled', isSelected);
        } else {
            $(this).parents('.attribute-row').find('.btn-value-add').prop('disabled', false);
        }
    })

    $(document).on('click', '.btn-value-add', function () {
        var index = $(this).parents('.attribute-row').data('index')
        $("#attr-index").val(index);
        $("#addValueModal").modal("show");
    });

    $(document).on('click', '#add-values-btn', function () {
        var values = $('#values').val();
        var id = $('#attr-index').val();
        if (values) {
            var valuesSplit = values.trim().split(',');
            valuesSplit = valuesSplit.filter(function (value, index, self) {
                // Boş değerleri filtrele
                if (value.trim() === '') {
                    return false;
                }
                // Kopya değerleri filtrele
                return index === self.indexOf(value);
            });
            if (valuesSplit.length > 0) {
                var row = $('[data-index="' + id + '"]');
                console.log(row);
                for (var i = 0; i < valuesSplit.length; i++) {
                    row.append(
                        `<input 
                                type="hidden" 
                                value="${valuesSplit[i].trim()}"
                                name="CategoryAttributeList[${id}].CategoryAttributeValues[${i}].Name"
                                class="cat-attr-value"
                                id="CategoryAttributeList_${id}__CategoryAttributeValues_${i}__Id"
                                                />`)
                }
            }
        }
        $("#addValueModal").modal("hide");
    });
});