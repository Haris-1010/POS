(function ($) {
    const _$PrefrenceForm = $('#PrefrenceForm');

    _$PrefrenceForm.find('.js-printer-type').on('change', async function () {
        if (!_$PrefrenceForm.valid()) {
            return;
        }
        const url = SavePreference;
        const data = _$PrefrenceForm.serializeObject();
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message } = resp;
            if (status) {
                toastr.success(message, "", { timeOut: 1000 });
            } else {
                toastr.error(message, "", { timeOut: 1000 });
            }
        } catch (error) {
            toastr.error("Failed to save Prefrence due to a technical error.", "", { timeOut: 500 });
        }
    })
})(jQuery)