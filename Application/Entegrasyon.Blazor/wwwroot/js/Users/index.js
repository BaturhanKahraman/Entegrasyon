
$(document).on("click", ".show-modal", function () {
    var userId = $(this).data("id");
    var userName = $(this).data("username")
    $.get("/Auth/ResetPassword?userId=" + userId +"&username="+userName, function (data) {
        $("#modal-placeholder").show();
        $("#modal-placeholder").html(data);
        $("#reset-password-modal").modal("show");
    });
});