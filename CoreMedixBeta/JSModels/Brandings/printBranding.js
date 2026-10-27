(function ($) {
    var brandingID = null;
    var customFieldsSize = 0;
    const _$brandingForm = $('#printBrandingForm'),
        _$newFieldBtn = $('#newFieldBtn')
    _$saveBrandingBtn = _$brandingForm.find('.js-save-btn');

    const _$saveFieldsModal = $('#saveFieldModal'),
        _$saveFieldsForm = _$saveFieldsModal.find('form');
    _$fieldsTable = $('#fieldsTable');

    const permissionsForm = $('#permissionsForm').serializeObject();

    let permissions = {
        canCreate: "1",
        canUpdate: "1",
        canDelete: "1"
    };

    $(function () {
        _$brandingForm.validate();
        _$saveFieldsForm.validate();

        _$saveFieldsModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop.fade.show').remove();
            _$saveFieldsForm.trigger('reset');
            _$saveFieldsForm.find('input[name="ID"]').val('0');
        });

        _$newFieldBtn.on("click", async function () {
            if (customFieldsSize == 3) {
                toastr.info("Maximum 3 custom fileds are allowed", "", { timeOut: 2000 })
                return;
            } else {
                $('#saveFieldModal').modal('show');
            }
        })

        _$saveBrandingBtn.on("click", async function () {
            const _$thisBtn = $(this);
            _$thisBtn.text('Saving...').prop('disabled', true);
            if (!_$brandingForm.valid()) {
                return;
            }

            const url = SavePrintBrandingUrl;
            let data = _$brandingForm.serializeObject();
            data.BrandingImages = await getBase64Images();
            data.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();

            $.post(url, data, function (resp) {
                const { Status: status, Message: message, Data: response } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 2000 });
                    setFormData(response);
                    _$thisBtn.text('Save Branding').prop('disabled', false);
                }
                else {
                    toastr.error(message, "", { timeOut: 1000 });
                    _$thisBtn.text('Save Branding').prop('disabled', false);
                }
            }).fail(function () {
                toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
                _$thisBtn.text('Save Branding').prop('disabled', false);
            });
        });

        $(document).on("click", ".js-remove-image-btn", function () {
            const target = $(this).data("preview-id");
            $(`#${target}`).attr('src', facilityNotFoundUrl);

            $(`#${target}`).closest('.profile-pic, .boxed-pic').find('input[type=file],input[type="hidden"]').val("");
            toastr.success("Image Removed", "", { timeOut: 1000 });
        });

        //Save custom fileds from modals..................................
        _$saveFieldsModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveFieldsForm.valid()) {
                return;
            }
            let data = _$saveFieldsForm.serializeObject();
            _$saveBtn.text('Saving...').prop('disabled', true);

            data.BrandingID = brandingID;
            data.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();
            data.InVisible = _$saveFieldsForm
                .find('input[name="InVisible"]')
                .is(":checked") ? true : false;

            const url = SavePrintBrandingCustomFieldUrl;
            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(resp.message, "", { timeOut: 500 });
                    _$saveFieldsModal.modal('hide');
                    getCustomFields();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            } catch (error) {
                toastr.error("An error occurred while saving the custom field.", "", { timeOut: 500 });
            } finally {
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        getPrintBranding();

        //call get custom fields ................
        getCustomFields();
    });

    function setFormData(data) {
        _$brandingForm.find('input[name="BusinessName"]').val(data.BusinessName);
        _$brandingForm.find('input[name="BusinessPhone"]').val(data.BusinessPhone);
        _$brandingForm.find('input[name="BusinessEmail"]').val(data.BusinessEmail);
        _$brandingForm.find('input[name="HeaderText"]').val(data.HeaderText);
        _$brandingForm.find('input[name="FooterText"]').val(data.FooterText);
        _$brandingForm.find('input[name="Logo"]').val(data.Logo);
        _$brandingForm.find('input[name="HeaderImg"]').val(data.HeaderImg);
        _$brandingForm.find('input[name="FooterImg"]').val(data.FooterImg);
        _$brandingForm.find('input[name="ID"]').val(data.ID);
        brandingID = data.ID;

        let defaultUrl = facilityNotFoundUrl;
        $('#logoImagePreview').attr('src', data.Logo ? data.Logo : defaultUrl);
        $('#headerImagePreview').attr('src', data.HeaderImg ? data.HeaderImg : defaultUrl);
        $('#footerImagePreview').attr('src', data.FooterImg ? data.FooterImg : defaultUrl);
        $('input[type=file]').val("");
    }

    function getPrintBranding() {
        let data = {
        };

        const url = getPrintBrandingUrl;
        $.post(url, data)
            .done(function (resp) {
                const { Status: status, Message: message, Data: response } = resp;
                if (status && response && response.ID) {
                    setFormData(response);
                } else {
                    toastr.error(message, "", { timeOut: 1000 });
                }
            })
            .fail(function () {
                toastr.error("An error occurred while fetching migration details.", "", { timeOut: 500 });
            });
    }

    /**
     * Function to get image data in base64 format for the required DTO format.
     */
    async function getBase64Images() {
        const images = [];

        const logoFile = document.getElementById('logoFile').files[0];
        //const headerFile = document.getElementById('headerFile').files[0];
        //const footerFile = document.getElementById('footerFile').files[0];

        if (logoFile) {
            images.push({
                ImageName: logoFile.name,
                ImageURL: await toBase64(logoFile),
                Type: 'LOGO'
            });
        }

        //if (headerFile) {
        //    images.push({
        //        ImageName: headerFile.name,
        //        ImageURL: await toBase64(headerFile),
        //        Type: 'HEADER'
        //    });
        //}

        //if (footerFile) {
        //    images.push({
        //        ImageName: footerFile.name,
        //        ImageURL: await toBase64(footerFile),
        //        Type: 'FOOTER'
        //    });
        //}

        return images;
    }

    /**
     * Convert a file to base64 string.
     * @param {File} file
     * @returns {Promise<string>} Base64 encoded string
     */
    function toBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result.split(',')[1]); // Remove the prefix "data:image/png;base64,"
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }

    window.previewImage = function (input, previewSelector) {
        if (input.files && input.files[0]) {
            const file = input.files[0];

            // Validate file type
            const validTypes = ['image/jpeg', 'image/png'];
            if (!validTypes.includes(file.type)) {
                toastr.error("Only JPG and PNG images are allowed.", "Validation Failed", { timeOut: 2000 });
                return;
            }

            // Validate file size (2MB = 2 * 1024 * 1024 bytes)
            const maxSize = 2 * 1024 * 1024;
            if (file.size > maxSize) {
                toastr.error("The file size must be less than 2MB", "Validation Failed", { timeOut: 2000 });
                return;
            }

            const reader = new FileReader();
            reader.onload = function (e) {
                $(previewSelector).attr('src', e.target.result);

                // hidden input update bhi karo
                const hiddenInput = $(previewSelector).closest('.profile-pic, .boxed-pic').find('input[type="hidden"]');
                hiddenInput.val(e.target.result);
            };
            reader.readAsDataURL(file);
        } else {
            $(previewSelector).attr('src', facilityNotFoundUrl).attr('alt', 'No Image');
            $(previewSelector).closest('.profile-pic, .boxed-pic').find('input[type="hidden"]').val("");
        }
    };


    //Get custom fileds from modals
    async function getCustomFields() {
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

        _$fieldsTable.DataTable().destroy();
        const table = _$fieldsTable.DataTable({
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
            lengthMenu: [10, 25, 50, 100 ,1000],
            pageLength: 10,
            processing: true,
            ordering: false,
            serverSide: true,
            lengthChange: true,
            searching: false,
            start: 10,
            paging: true,
            "ajax": {
                url: getPrintBrandingCustomFieldUrl,
                type: "POST",
                data: {
                    '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val(),
                },
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    var d = $.parseJSON(json.data);
                    json.data = d;
                    customFieldsSize = d.length;
                    var return_data = new Array();
                    for (var i = 0; i < d.length; i++) {
                        return_data.push({
                            'FieldName': d[i].FieldName,
                            'FieldValue': d[i].FieldValue,
                            'ForModule': d[i].ForModule,
                            'Order': d[i].Order,
                            'InVisible': d[i].InVisible,
                            'Actions': `<div class="btn-group-sm action-btns">
                            ${permissions.canUpdate ?
                                    `<button class="btn btn-default btn-sm js-edit"
                                        data-field-id="${d[i].ID}"
                                data-name="${d[i].FieldName}"
                                 data-value="${d[i].FieldValue}"
                                data-module="${d[i].ForModule}"
                                data-order="${d[i].Order}"
                                data-visible="${d[i].InVisible}"
                                data-brandingId="${d[i].BrandingID}"
                                        title="Edit Field" data-toggle="tooltip">
                                        <i class="fa fa-edit"></i>
                                 </button>` : ''}
                            ${permissions.canDelete ?
                                    `<button class="btn btn-sm btn-danger js-delete" data-toggle="tooltip" data-field-id="${d[i].ID}" title="Delete Field">
                                        <i class="fa fa-trash"></i>
                                    </button>` : ''}
                             </div>`
                        });
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "FieldName" },
                { "data": "FieldValue" },
                {
                    "data": "ForModule",
                },
                { "data": "Order" },
                {
                    "data": "InVisible",
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

    //Edit custom field or fill modal inputs
    _$fieldsTable.on('click', '.js-edit', function () {
        const _$this = $(this);
        _$saveFieldsForm.find('input[name="ID"]').val(_$this.data('field-id'));
        _$saveFieldsForm.find('input[name="FieldName"]').val(_$this.data('name'));
        _$saveFieldsForm.find('input[name="FieldValue"]').val(_$this.data('value'));
        _$saveFieldsForm.find('input[name="Order"]').val(_$this.data('order'));
        _$saveFieldsForm.find('input[name="InVisible"]').prop('checked', _$this.data('visible'));
        _$saveFieldsForm.find('select[name="ForModule"]').val(_$this.data('module')).trigger('change');
        _$saveFieldsModal.modal('show');
    });

    //Delete custom field form listing ...
    _$fieldsTable.on('click', '.js-delete', function () {
        let filedId = $(this).data('field-id');
        bootbox.confirm({
            title: 'Delete Field',
            message: "Do you want to delete this field?",
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
                    const url = DeleteCustomFieldUrl;
                    const data = {
                        ID: filedId,
                        '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
                    };
                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 500 });
                            getCustomFields();
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("An error occurred while deleting the field.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });
})(jQuery);

/*
 (function ($) {
    const _$rollbackBtn = $(".js-roll-back-btn");
    const _$migrateBtn = $(".js-migrate-btn");
    $(function () {
        _$migrateBtn.on("click", function () {
            const url = '/Pharmacy/InventoryMigrations/MigrateData';
            let data = {
            }

            $.post(url, data, function (resp) {
                const { Status: status, Message: message, Data: response } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 2000 });
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                }
                else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            }).fail(function () {
                toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
            });
        });

        _$rollbackBtn.on("click", function () {
            const url = '/Pharmacy/InventoryMigrations/RollBackMigration';
            let data = {
                ,
                MigrationID: $('#migrationId').val()
            }

            $.post(url, data, function (resp) {
                const { Status: status, Message: message, Data: response } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 2000 });
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                }
                else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            }).fail(function () {
                toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
            });
        });

        getMigrationDetails()
    })

    function getMigrationDetails() {
        let data = {
        };

        const url = '/Pharmacy/InventoryMigrations/GetMigrationDetails';
        $.post(url, data)
            .done(function (resp) {
                const { Status: status, Message: message, Data: response } = resp;

                if (status && response && response.MigrationID) {
                    $('#migrationId').val(response.MigrationID);
                    $(".card-body").html(`<div>
                                              <h6>Data Migrated On : ${response.MigrationDate}</h6>
                                              <h6>Data Migrated By : ${response.MigratedByName}</h6>
                                              <h6>Migration ID : ${response.MigrationID}</h6>
                                          </div>`
                    );

                    _$rollbackBtn.show();
                    _$migrateBtn.hide();
                } else {
                    _$migrateBtn.show();
                    _$rollbackBtn.hide();
                }
            })
            .fail(function () {
                toastr.error("An error occurred while fetching migration details.", "", { timeOut: 500 });
                _$migrateBtn.show();
                _$rollbackBtn.hide();
            });
    }
})(jQuery)

 */