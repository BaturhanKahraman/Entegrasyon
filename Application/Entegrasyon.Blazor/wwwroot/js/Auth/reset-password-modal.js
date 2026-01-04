
$(document).on("submit", "#reset-password-form", function (e) {
    e.preventDefault();
    var form = $(this);
    var loading = $("#modal-loading");
    loading.show();
    form.hide();

    $.post("Auth/resetpassword", form.serialize(), function (data) {
        loading.hide();
        if (data.success === true) {
            $("#modal-success").show();
            if (data.message) {
                $("#modal-success-text").text(data.message);
            }
        } else if (data.success===false) {
            form.show();
            $("#modal-danger").text(data.message);
        }
    });
});