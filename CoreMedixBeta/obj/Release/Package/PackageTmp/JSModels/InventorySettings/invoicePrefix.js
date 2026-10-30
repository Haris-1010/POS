(function ($) {
    const _$perfixform = $('#InvoicePerfixForm');
    $(function () {
        $(".js-save-btn").on("click", function () { 
            if (!_$perfixform.valid()) {
                return false;
            }
            let data = _$perfixform.serializeObject();
            const url = SaveInvoiceSettingUrl;

            $.post(url, data, function (resp) {
                const { Status: status, Message: message, data: Data } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                }
                else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            }).fail(function () {
                toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
            });
        });
    })
})(jQuery)