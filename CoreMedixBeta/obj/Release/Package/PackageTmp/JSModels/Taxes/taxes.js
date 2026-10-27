(function ($) {
    const _$saveTaxModal = $('#saveTaxModal'),
        _$saveTaxForm = _$saveTaxModal.find('form'),
        _$taxesTable = $('#taxesTable');

    const TaxesPermissionsForm = $('#TaxesPermissionsForm').serializeObject();

    let TaxesPermissions = {
        canUpdate: "1",
        canDelete: "1"
    };

    $(function () {
        $('.js-select2').select2({
            dropdownParent: $('#saveTaxModal'),
            width: '99%'
        });
        _$saveTaxModal.validate();
        _$saveTaxModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop').remove();

            // Reset form fields
            _$saveTaxForm.trigger('reset');
            _$saveTaxForm.find('select[name="PaymentMode"]').val(null).trigger('change');
            _$saveTaxForm.find('input[name="ID"]').val('0');

            // Reset validation messages
            if (_$saveTaxForm.data('validator')) {
                _$saveTaxForm.validate().resetForm();           // Remove error messages
                _$saveTaxForm.find('.is-invalid').removeClass('is-invalid'); // Remove red borders
            }
        });

        _$saveTaxModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveTaxForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            let formData = _$saveTaxForm.serializeObject();

            const url = SaveTaxUrl;
            const data = formData;

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    _$saveTaxModal.modal('hide');
                    getTaxes();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save Tax due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });
        getTaxes();
    })
    async function getTaxes() {
        const buttonsArray = [
            {
                extend: "pageLength",
                className: "btn btn-default",
                titleAttr: 'Show rows',
                text: '<i class="fa fa-ellipsis-v"></i> Show rows',
                init: function (api, node, config) {
                    $(node).removeClass('dt-button');
                },
            }
        ];

        _$taxesTable.DataTable().destroy();
        const table = _$taxesTable.DataTable({
            dom: 'Blfrtip',
            lengthChange: false,
            buttons: buttonsArray,
            "processing": false,
            "serverSide": true,
            "searchable": true,
            "stateSave": true,
            "start": 10,
            "lengthMenu": [[10, 25, 50, 100], [10, 25, 50, 100]],
            "pageLength": 10,
            "paging": true,

            "ajax": {
                url: GetPagedTaxesUrl,
                data: {
                },
                "type": "POST",
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    var d = $.parseJSON(json.data);
                    json.data = d;
                    var return_data = new Array();
                    for (var i = 0; i < d.length; i++) {
                        return_data.push({
                            'Title': d[i].Title,
                            'Percentage': d[i].Percentage,
                            'PaymentMode': d[i].PaymentMode,
                            'Actions': `<div class="btn-group-sm action-btns">
                                            ${TaxesPermissions.canUpdate ? `
                                            <button class="btn btn-default btn-sm js-edit"
                                              data-tax-id="${d[i].ID}"
                                              data-title="${d[i].Title}"
                                              data-percentage="${d[i].Percentage}"
                                              data-payment-mode="${d[i].PaymentMode}"
                                              title="Edit Tax" data-toggle="tooltip">
                                              <i class="fa fa-edit"></i>
                                            </button>
                                            ` : ''}

                                            ${TaxesPermissions.canDelete ? `
                                            <button class="btn btn-sm btn-danger js-delete" data-toggle="tooltip" data-tax-id="${d[i].ID}" title="Delete Tax">
                                                <i class="fa fa-trash"></i>
                                            </button>
                                            ` : ''}

                                         </div>`
                        });
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "Title" },
                { "data": "Percentage" },
                { "data": "PaymentMode" },
                { "data": "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Title"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search By Title')
            .addClass('seachbar-tooltip');
        //    $.fn.dataTable.Debounce(table);
    }

    _$taxesTable.on('click', '.js-edit', function () {
        const _$this = $(this);
        _$saveTaxForm.find('input[name="ID"]').val(_$this.data('tax-id'));
        _$saveTaxForm.find('input[name="Title"]').val(_$this.data('title'));
        _$saveTaxForm.find('input[name="Percentage"]').val(_$this.data('percentage'));
        _$saveTaxForm.find('select[name="PaymentMode"]').val(_$this.data('payment-mode')).trigger('change');
        _$saveTaxModal.modal('show');
    })

    _$taxesTable.on('click', '.js-delete', function () {
        let taxId = $(this).data('tax-id');

        bootbox.confirm({
            title: 'Delete Tax',
            message: "Do you want to delete this Tax?",
            size: 'small',
            buttons: {
                confirm: {
                    label: 'Confirm',
                    className: 'btn-success'
                },
                cancel: {
                    label: 'Cancel',
                    className: 'btn-danger'
                }
            },
            callback: async function (result) {
                if (result) {
                    const url = DeleteTaxUrl;
                    const data = {
                        ID: taxId,
                    };

                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 500 });
                            getTaxes();
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("Failed to delete Tax due to a technical error.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });
})(jQuery)