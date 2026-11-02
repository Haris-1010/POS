(function ($) {
    const _$saleInfoForm = $('#saleInfoForm'),
        _$referredBySearchElement = $('.js-search-referred-by');

    const _$saveContactModal = $('#saveContactModal'),
        _$saveContactForm = _$saveContactModal.find('form');

    $(function () {
        _$saveContactModal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$saveContactForm.trigger('reset');
            _$saveContactForm.find('input[name="ID"]').val('0');
        });

        _$saveContactForm.validate();

        _$saveContactModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveContactForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            let data = _$saveContactForm.serializeObject();

            const url = SaveContactUrl;
            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message, Data: contactId } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$referredBySearchElement.val(data.Name);
                    _$saleInfoForm.find('input[name="ReferredByID"]').val(contactId);
                    _$saleInfoForm.find('input[name="ReferredByType"]').val("Contact");
                    _$saveContactModal.modal('hide');
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            } catch (error) {
                toastr.error("An error occurred while saving the customer.", "", { timeOut: 500 });
            } finally {
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

         
         
    })
})(jQuery)