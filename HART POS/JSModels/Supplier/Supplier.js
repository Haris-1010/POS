(function ($) {
    const _$saveSupplierModal = $('#saveSupplierModal'),
        _$saveSupplierForm = _$saveSupplierModal.find('form'),
        _$suppliersTable = $('#suppliersTable');
         
    let supplierPermissions = {
        canCreate: "1",
        canUpdate: "1",
        canDelete: "1"
    };

    $(function () {
        $('.js-select2').select2({
            dropdownParent: $('#saveSupplierModal'),
            width: '99%'
        });
        _$saveSupplierModal.validate();
        _$saveSupplierModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop').remove();
            _$saveSupplierForm.trigger('reset'); 
            _$saveSupplierForm.find('select[name="City"]').val(null).trigger('change');
            _$saveSupplierForm.find('input[name="ID"]').val('');

            _$saveSupplierForm.validate().resetForm();  
            _$saveSupplierForm.find('.error').removeClass('error')
        });
        _$saveSupplierModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveSupplierForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            let formData = _$saveSupplierForm.serializeObject();
            formData.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();

            const url = saveSupplierUrl;
            const data = formData;

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    _$saveSupplierModal.modal('hide');
                    getSuppliers();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save manufacturer due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });
        getSuppliers();
    })
    async function getSuppliers() {
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

        _$suppliersTable.DataTable().destroy();
        const table = _$suppliersTable.DataTable({
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
                url: getSupplierUrl,
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
                            'Name': d[i].Name,
                            'Phone': d[i].Phone,
                            'Email': d[i].Email,
                            'City': d[i].City,
                            'Actions': `<div class="btn-group-sm action-btns">
                            ${supplierPermissions.canUpdate ?
                                    `<button class="btn btn-default btn-sm js-edit"
                                        data-supplier-id="${d[i].ID}"
                                        data-name="${d[i].Name}"
                                        data-phone="${d[i].Phone}"
                                        data-email="${d[i].Email}"
                                        data-city="${d[i].City}"
                                        title="Edit Supplier" data-toggle="tooltip">
                                        <i class="fa fa-edit"></i>
                                 </button>` : ''}
                            ${supplierPermissions.canDelete ?
                                    `<button class="btn btn-sm btn-danger js-delete" data-toggle="tooltip" data-supplier-id="${d[i].ID}" title="Delete Supplier">
                                        <i class="fa fa-trash"></i>
                                    </button>` : ''}
                             </div>`
                        })
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "Name" },
                { "data": "Phone" },
                { "data": "Email" },
                { "data": "City" },
                { "data": "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Name, Phone, Email"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search By Name, Phone, Email')
            .addClass('seachbar-tooltip');
        //    $.fn.dataTable.Debounce(table);
    }

    _$suppliersTable.on('click', '.js-edit', function () {
        const _$this = $(this);
        _$saveSupplierForm.find('input[name="ID"]').val(_$this.data('supplier-id'));
        _$saveSupplierForm.find('input[name="Name"]').val(_$this.data('name'));
        _$saveSupplierForm.find('input[name="Phone"]').val(_$this.data('phone'));
        _$saveSupplierForm.find('input[name="Email"]').val(_$this.data('email'));
        _$saveSupplierForm.find('select[name="City"]').val(_$this.data('city')).trigger('change');
        _$saveSupplierModal.modal('show');
    })

    _$suppliersTable.on('click', '.js-delete', function () {
        let supplierId = $(this).data('supplier-id');

        bootbox.confirm({
            title: 'Delete Supplier',
            message: "Do you want to delete this supplier?",
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
                    const url = deleteSupplierUrl;
                    const data = {
                        SupplierID: supplierId,
                        '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
                    };

                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 500 });
                            getSuppliers();
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("Failed to delete manufacturer due to a technical error.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });
    $.fn.serializeObject = function () {
        var o = {};
        var a = this.serializeArray();
        $.each(a, function () {
            if (o[this.name]) {
                if (!Array.isArray(o[this.name])) {
                    o[this.name] = [o[this.name]];
                }
                o[this.name].push(this.value || '');
            } else {
                o[this.name] = this.value || '';
            }
        });
        return o;
    };
})(jQuery)