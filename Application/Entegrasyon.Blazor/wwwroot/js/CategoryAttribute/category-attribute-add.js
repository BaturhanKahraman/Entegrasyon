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
        console.log("Tetiklendim");
        updateValueCounts();
    });

    $('.attribute-row').on('DOMNodeInserted DOMNodeRemoved', 'input', function () {
        updateValueCounts();
    })

    function updateValueCounts() {
        $('.attribute-row').each(function () {
            var count = $(this).find('input[id$="__Name"]').length;
            console.log("count :", count);
            if (count > 0)
                $(this).find('.value-count').text('(' + count + ')');
            else if (count == 0) {
                $(this).find('.value-count').text('');
            }
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
                updateValueCounts();
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
    //kategori özellik değeri eklemek için bootstrap modal açıldığında
    //zaten ekli olan değerler içeriye, ile aktarılacak
    // değiştirildiğinde ya da eklendiğinde sync yapılacak.
    $(document).on('click', '.btn-value-add', function () {
        var index = $(this).parents('.attribute-row').data('index')
        $("#attr-index").val(index);
        var valuestxtBox = $("#values");
        valuestxtBox.val(null);
        var values = $(this).parents('.attribute-row').find('input[id$="__Name"]');
        console.log(values);
        if (values) {
            for (var i = 0; i < values.length; i++) {
                var oldValue = valuestxtBox.val();
                var addedValue = values.eq(i).val();
                if (!oldValue) {
                    valuestxtBox.val(addedValue);
                    continue;
                }
                if (i == values.length - 1) {
                    valuestxtBox.val(oldValue + ',' + addedValue);
                    break;
                }
                valuestxtBox.val(oldValue + ',' + addedValue +',');
            }
        }
        $("#addValueModal").modal("show");
    });
    //değer ekleme işlemi
    $(document).on('click', '#add-values-btn', function () {
        var values = $('#values').val();// girilen değerler
        var id = $('#attr-index').val();// attribute index
        var row = $('[data-index="' + id + '"]');//satırı
        var oldValues = row.find('input[id$="__Name"]').map(function () {
            return $(this).val();
        }).get();//içerisindeki input değerleri.
        if (values) {
            var valuesSplit = values.trim().split(',');
            //kopya değerleri ayıklama
            valuesSplit = valuesSplit.filter(function (value, index, self) {
                // Boş değerleri filtrele
                if (value.trim() === '') {
                    return false;
                }
                // Kopya değerleri filtrele
                return index === self.indexOf(value);
            });//temizleme işlemi
            if (valuesSplit.length == 0) {//hiç yoksa sil
                var confirmAccepted = confirm("Tüm değerleri sildiniz, devam edecek misiniz ?");
                if (confirmAccepted) {
                    row.find('input').clear();
                }
                // sayfadaki hali hazırda eklenmişler inputlar da silinecek mi ?
            }
            if (valuesSplit.length > 0) {
                //arrayi unionla, inputtan gelen farkları ekle
                var newAddedValues = $(valuesSplit).not(oldValues).get(); // yeni eklenenler
                for (var i = 0; i < newAddedValues.length; i++) {
                    row.append(
                        `<input 
                                type="hidden" 
                                value="${newAddedValues[i].trim()}"
                                name="CategoryAttributeList[${id}].CategoryAttributeValues[${i}].Name"
                                class="cat-attr-value"
                                id="CategoryAttributeList_${id}__CategoryAttributeValues_${i}__Id"
                                                />`)// önceki eklenenlerle karışabilir
                }

                for (var i = 0; i < valuesSplit.length; i++) {
                    if ($.inArray(valuesSplit[i], oldValues) === -1)
                        continue;
                    row.append(
                        `<input 
                                type="hidden" 
                                value="${valuesSplit[i].trim()}"
                                name="CategoryAttributeList[${id}].CategoryAttributeValues[${i}].Name"
                                class="cat-attr-value"
                                id="CategoryAttributeList_${id}__CategoryAttributeValues_${i}__Id"
                                                />`)
                }
                //array farklarını al
                var diff1 = $(valuesSplit).not(oldValues).get(); // yeni eklenenler
                console.log(diff1);
                var diff2 = $(oldValues).not(valuesSplit).get(); // silinenler
                console.log(diff2);
            }
        }
        updateValueCounts();
        $("#addValueModal").modal("hide");

    });
});