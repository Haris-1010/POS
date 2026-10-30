(function ($) {
    const _$saveManufacturerModal = $('#saveManufacturerModal'),
        _$saveManufacturerForm = _$saveManufacturerModal.find('form'),
        _$manufacturersTable = $('#manufacturersTable');

    let manufacturerPermissions = {
        canCreate: "1",
        canUpdate: "1",
        canDelete: "1"
    };

    $(function () {
        $('#saveManufacturerModal').on('shown.bs.modal', function () {
            const $cityDropdown = $(this).find('.js-select2');

            if ($cityDropdown.hasClass('select2-hidden-accessible')) {
                $cityDropdown.select2('destroy');
            }

            $cityDropdown.select2({
                dropdownParent: $('#saveManufacturerModal'),
                width: '100%'
            });
        });

        _$saveManufacturerForm.validate();

        _$saveManufacturerModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop.fade.show').remove();
            _$saveManufacturerForm.trigger('reset');
            _$saveManufacturerForm.find('input[name="ID"]').val('0');
        });

        _$saveManufacturerModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveManufacturerForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            const url = saveManufacturerUrl;
            const data = _$saveManufacturerForm.serializeObject();
            data.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    _$saveManufacturerModal.modal('hide');
                    getManufacturers();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save manufacturer due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        getManufacturers();
    });

    _$manufacturersTable.on('click', '.js-edit', function () {
        const _$this = $(this);

        _$saveManufacturerForm.find('input[name="ID"]').val(_$this.data('manufacturer-id'));
        _$saveManufacturerForm.find('input[name="Name"]').val(_$this.data('name'));
        _$saveManufacturerForm.find('input[name="Phone"]').val(_$this.data('phone'));
        _$saveManufacturerForm.find('input[name="Email"]').val(_$this.data('email'));
        _$saveManufacturerForm.find('select[name="City"]').val(_$this.data('city')).trigger('change');
        _$saveManufacturerModal.modal('show');
    });

    _$manufacturersTable.on('click', '.js-delete', function () {
        let manufacturerId = $(this).data('manufacturer-id');
        bootbox.confirm({
            title: 'Delete Manufacturer',
            message: "Do you want to delete this manufacturer?",
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
                    const url = deleteManufacturerUrl;
                    const data = {
                        ID: manufacturerId,
                        '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
                    };

                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 500 });
                            getManufacturers();
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

    async function getManufacturers() {
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

        _$manufacturersTable.DataTable().destroy();
        const table = _$manufacturersTable.DataTable({
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
                url: getManufacturerUrl,
                data: {
                    /*'__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()*/
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
                            ${manufacturerPermissions.canUpdate ? `
                                <button class="btn btn-default btn-sm js-edit"
                                    data-manufacturer-id="${d[i].ID}"
                                    data-name="${d[i].Name}"
                                    data-phone="${d[i].Phone}"
                                    data-email="${d[i].Email}"
                                    data-city="${d[i].City}"
                                    title="Edit Manufacturer" data-toggle="tooltip">
                                    <i class="fa fa-edit"></i>
                                </button>` : ''}
                            ${manufacturerPermissions.canDelete ? `
                                <button class="btn btn-sm btn-danger js-delete" 
                                    data-toggle="tooltip" 
                                    data-manufacturer-id="${d[i].ID}" 
                                    title="Delete Manufacturer">
                                    <i class="fa fa-trash"></i>
                                </button>` : ''} 
                            <a class="btn btn-sm btn-secondary" 
                               title="View Brand" 
                               data-toggle="tooltip" 
                               href="${brandUrl}?ID=${d[i].ID}">
                               <i class="fas fa-star"></i>
                            </a>
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
    }
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
})(jQuery);