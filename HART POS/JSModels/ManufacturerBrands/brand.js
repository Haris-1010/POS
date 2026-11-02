(function ($) {
    const _$brandModal = $('#BrandModal'),
        _$brandForm = _$brandModal.find('form'),
        _$brandTable = $('#brandTable');

    //const manufacturerPermissionsForm = $('#manufacturerPermissionsForm').serializeObject();

    //let manufacturerPermissions = {
    //    canCreate: manufacturerPermissionsForm.canCreate === "1",
    //    canUpdate: manufacturerPermissionsForm.canUpdate === "1",
    //    canDelete: manufacturerPermissionsForm.canDelete === "1"
    //};

    $(function () {
        _$brandForm.validate();

        _$brandModal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$brandForm.trigger('reset');
            _$brandForm.find('input[name="ID"]').val('0');
        });

        _$brandModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$brandForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            const url = SaveBrandUrl;
            const data = _$brandForm.serializeObject();
            //data.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    _$brandModal.modal('hide');
                    getBrands();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save Brand due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });
        getBrands();
    });

    _$brandTable.on('click', '.js-edit', function () {
        const _$this = $(this);
        _$brandForm.find('input[name="ID"]').val(_$this.data('brand-id'));
        _$brandForm.find('input[name="Title"]').val(_$this.data('title'));
        _$brandModal.modal('show');
    });

    _$brandTable.on('click', '.js-delete', function () {
        let brandId = $(this).data('brand-id');
        bootbox.confirm({
            title: 'Delete Brand',
            message: "Do you want to delete this Brand?",
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
                    const url = DeleteBrandUrl;
                    const data = {
                        ID: brandId,
                        //    '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
                    };

                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 500 });
                            getBrands();
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("Failed to delete Brand due to a technical error.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });

    async function getBrands() {
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
        _$brandTable.DataTable().destroy();
        const table = _$brandTable.DataTable({
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
                url: GetPagedBrandsUrl,
                data: {
                    ManufacturerID: $("#manufacturerID").val()
                    //    '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
                },
                "type": "POST",
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    var d = $.parseJSON(json.data);
                    json.data = d;
                    var return_data = new Array();
                    for (var i = 0; i < d.length; i++) {
                        return_data.push({
                            'Name': d[i].Title,
                            'Actions': `<div class="btn-group-sm action-btns" role="group">
                                <button class="btn btn-sm btn-warning js-edit"
                                    data-brand-id="${d[i].ID}"
                                    data-title="${d[i].Title}"
                                    title="Edit">
                                    <i class="fa fa-edit"></i>
                                </button>
                                <button class="btn btn-sm btn-danger js-delete"
                                    data-brand-id="${d[i].ID}" title="Delete">
                                    <i class="fa fa-trash"></i>
                                </button>
                            </div>`
                        })
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "Name" },
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