(function ($) {
    const _$saveDiscountModal = $('#saveDiscountModal'),
        _$saveDiscountForm = _$saveDiscountModal.find('form'),
        _$discountsTable = $('#discountsTable');

    const permissionsForm = $('#permissionsForm').serializeObject();

    let permissions = {
        canCreate: "1",
        canUpdate: "1",
        canDelete: "1"
    };
    $(document).on("change", 'select[name="DiscountType"]', function () {
        let type = $(this).val();
        let $valueInput = $('input[name="DiscountValue"]');

        if (type === "percentage") {
            $valueInput.attr("max", 100);   // percentage case
        } else {
            $valueInput.removeAttr("max");  // value case
        }
    });


    $(function () {
        $('.js-select2').select2({
            dropdownParent: $('#saveDiscountModal'),
            width: '99%'
        });
        _$saveDiscountForm.validate();

        _$saveDiscountModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop.fade.show').remove();
            _$saveDiscountForm.trigger('reset');
            _$saveDiscountForm.find('input[name="ID"]').val('0');
            _$saveDiscountForm.find('select[name="DiscountType"]').val('').trigger('change');
        });

        _$saveDiscountModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveDiscountForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            let data = _$saveDiscountForm.serializeObject();
            data.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();
            data.IsActive = _$saveDiscountForm
                .find('input[name="IsActive"]')
                .is(":checked") ? true : false;

            const url = SaveDiscountUrl;
            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveDiscountModal.modal('hide');
                    getDiscounts();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            } catch (error) {
                toastr.error("An error occurred while saving the customer.", "", { timeOut: 500 });
            } finally {
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        getDiscounts();
    });

    _$discountsTable.on('click', '.js-edit', function () {
        const _$this = $(this);
        _$saveDiscountForm.find('input[name="ID"]').val(_$this.data('discount-id'));
        _$saveDiscountForm.find('input[name="DiscountName"]').val(_$this.data('name'));
        _$saveDiscountForm.find('input[name="DiscountValue"]').val(_$this.data('value'));
        _$saveDiscountForm.find('input[name="IsActive"]').prop('checked', _$this.data('active'));
        _$saveDiscountForm.find('select[name="DiscountType"]').val(_$this.data('type')).trigger('change');
        _$saveDiscountModal.modal('show');
    });

    _$discountsTable.on('click', '.js-delete', function () {
        let discountId = $(this).data('discount-id');
        bootbox.confirm({
            title: 'Delete Discount',
            message: "Do you want to delete this discount?",
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
                    const url = DeleteDiscountUrl;
                    const data = {
                        ID: discountId,
                        '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
                    };
                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 500 });
                            getDiscounts();
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("An error occurred while deleting the customer.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });

    async function getDiscounts() {
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

        _$discountsTable.DataTable().destroy();
        const table = _$discountsTable.DataTable({
            dom: 'Blfrtip',
            buttons: [
                'copy',
                {
                    extend: 'csv',
                    text: 'Excel'
                },
                'pdf',
                'print',
                {
                    extend: 'colvis',
                    text: 'Columns'
                },
            ],
            lengthMenu: [10, 25, 50, 100, 1000],
            pageLength: 10,
            processing: true,
            ordering: false,
            serverSide: true,
            lengthChange: true,
            searching: true,
            start: 10,
            paging: true,
            "ajax": {
                url: GetPagedDiscountsUrl,
                type: "POST",
                data: {
                    '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val(),
                },
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    var d = $.parseJSON(json.data);
                    json.data = d;
                    var return_data = new Array();
                    for (var i = 0; i < d.length; i++) {
                        return_data.push({
                            'DiscountName': d[i].DiscountName,
                            'DiscountType': d[i].DiscountType,
                            'DiscountValue': d[i].DiscountValue,
                            'Status': d[i].IsActive,
                            'Actions': `<div class="btn-group-sm action-btns">
                            ${permissions.canUpdate ?
                                    `<button class="btn btn-default btn-sm js-edit"
                                        data-discount-id="${d[i].ID}"
                                        data-name="${d[i].DiscountName}"
                                        data-type="${d[i].DiscountType}"
                                        data-value="${d[i].DiscountValue}"
                                        data-active="${d[i].IsActive}"
                                        title="Edit Discount" data-toggle="tooltip">
                                        <i class="fa fa-edit"></i>
                                 </button>` : ''}
                            ${permissions.canDelete ?
                                    `<button class="btn btn-sm btn-danger js-delete" data-toggle="tooltip" data-discount-id="${d[i].ID}" title="Delete Discount">
                                        <i class="fa fa-trash"></i>
                                    </button>` : ''}
                             </div>`
                        });
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "DiscountName" },
                {
                    "data": "DiscountType",
                    "render": function (data, type, row) {
                        if (data == "value") {
                            return "In Value"
                        } else {
                            return "In Percentage"
                        }
                    }
                },
                { "data": "DiscountValue" },
                {
                    "data": "Status",
                    "render": function (data, type, row) {
                        if (data) {
                            return `<div class="badge badge-success">Active</div>`
                        } else {
                            return `<div class="badge badge-danger">In Active</div>`
                        }
                    }
                },
                { "data": "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search By Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery);