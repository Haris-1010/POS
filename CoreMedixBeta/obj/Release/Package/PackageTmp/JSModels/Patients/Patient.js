(function ($) {
    const _$saveCustomerModal = $('#saveCustomerModal'),
        _$saveCustomerForm = _$saveCustomerModal.find('form'),
        _$customersTable = $('#customersTable');

    let customerPermissions = {
        canCreate: "1",
        canUpdate: "1",
        canDelete: "1"
    };

    $(function () {
        // Date Picker Initialization
        $('.js-date-picker').datepicker({
            format: 'dd/mm/yyyy',
            autoclose: true,
            todayHighlight: true,
            orientation: 'bottom',
            startView: 2, 
            language: 'en'
        });

        // For Select2 inside the modal
        $('#saveCustomerModal .js-select2').select2({
            dropdownParent: $('#saveCustomerModal'),
            width: '100%'
        });


        $('.card .js-select2').select2({
            width: '100%'
        }); 

        _$saveCustomerForm.validate({
            rules: {
                CNIC: {
                    required: false,
                    pattern: /^[0-9]{5}-[0-9]{7}-[0-9]{1}$/
                },
                Gender: {
                    required: true
                },
                City: {
                    required: true
                }
            },
            messages: {
                CNIC: "CNIC must be in the format 12345-1234567-1",
                Gender: "Please select gender",
                City: "Please select city"
            },
            errorPlacement: function (error, element) {
                error.addClass("text-danger");

                if (element.hasClass("js-select2")) { 
                    error.insertAfter(element.next('.select2'));
                } else { 
                    error.insertAfter(element);
                }
            },
            highlight: function (element) {
                $(element).addClass("is-invalid");
            },
            unhighlight: function (element) {
                $(element).removeClass("is-invalid");
            }
        });


        _$saveCustomerForm.find('input[name="CNIC"]').on('input', function () {
            let value = $(this).val().replace(/\D/g, '');
            if (value.length > 5 && value.length <= 12) {
                value = value.slice(0, 5) + '-' + value.slice(5);
            }
            if (value.length > 12) {
                value = value.slice(0, 5) + '-' + value.slice(5, 12) + '-' + value.slice(12, 13);
            }
            $(this).val(value);
        });

        // Reset modal on close
        _$saveCustomerModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop.fade.show').remove();
             
            _$saveCustomerForm.find('input[type="text"], input[type="tel"], input[type="email"]').val('');
            _$saveCustomerForm.find('input[name="ID"]').val('0');
             
            _$saveCustomerForm.find('input[name="DOB"]').datepicker('setDate', null);
             
            _$saveCustomerForm.find('select[name="City"], select[name="Gender"]').val(null).trigger('change');
             
            _$saveCustomerForm.validate().resetForm();
            _$saveCustomerForm.find('.error, .is-invalid').removeClass('error is-invalid');
        });


        // Save Customer
        _$saveCustomerModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveCustomerForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            let data = _$saveCustomerForm.serializeObject();

            if (data.DOB) {
                let dob = moment(data.DOB, [
                    'DD-MM-YYYY',
                    'MM/DD/YYYY',
                    'YYYY-MM-DD',
                    'DD/MM/YYYY',
                    'MM-DD-YYYY'
                ], true);

                if (dob.isValid()) {
                    // DB ke liye ISO format bhejna (safe for SQL)
                    data.DOB = dob.format('YYYY-MM-DD');
                } else {
                    toastr.error("Invalid date format for DOB.", "", { timeOut: 3000 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    return;
                }
            }



            // PurchaseDate format conversion for DD-MM-YYYY
            if (data.PurchaseDate) {
                let purchaseDate = moment(data.PurchaseDate, 'DD-MM-YYYY');
                if (purchaseDate.isValid()) {
                    data.PurchaseDate = purchaseDate.format('YYYY-MM-DD');
                } else {
                    toastr.error("Invalid date format for Purchase Date. Please use DD-MM-YYYY", "", { timeOut: 3000 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    return;
                }
            }

            data.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();

            const url = saveCustomerUrl;
            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveCustomerModal.modal('hide');
                    getCustomers();

                    // these three lines for the Invoice Page
                    $('.js-search-customer').val(data.Name);   
                    $('input[name="CustomerID"]').val(resp.Data || 0);
                    $('input[name="CustomerType"]').val("Customer");

                } else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            } catch (error) {
                toastr.error("An error occurred while saving the customer.", "", { timeOut: 500 });
            } finally {
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        // Edit customer
        _$customersTable.on('click', '.js-edit', function () {
            const _$this = $(this);

            const customerId = _$this.data('customer-id') || 0;
            const name = _$this.data('name') || '';
            const phone = _$this.data('phone') || '';
            const cnic = _$this.data('cnic') || '';
            const city = _$this.data('city') || '';
            const address = _$this.data('address') || '';
            const dob = _$this.data('dob') || '';
            const gender = _$this.data('gender') || '';

            _$saveCustomerForm.find('input[name="ID"]').val(customerId);
            _$saveCustomerForm.find('input[name="Name"]').val(name);
            _$saveCustomerForm.find('input[name="Phone"]').val(phone);
            _$saveCustomerForm.find('input[name="CNIC"]').val(cnic);
            _$saveCustomerForm.find('select[name="City"]').val(city).trigger('change');
            _$saveCustomerForm.find('input[name="Address"]').val(address);

            let rawDob = _$this.data('dob');
            if (rawDob) {
                let dob = moment(rawDob, ["DD/MM/YYYY", "YYYY-MM-DD", "MM/DD/YYYY"]);
                if (dob.isValid()) {
                    _$saveCustomerForm.find('input[name="DOB"]').datepicker("setDate", dob.toDate());
                } else {
                    _$saveCustomerForm.find('input[name="DOB"]').val('');
                }
            } else {
                _$saveCustomerForm.find('input[name="DOB"]').val('');
            }


            _$saveCustomerForm.find('select[name="Gender"]').val(gender).trigger('change');
            _$saveCustomerModal.modal('show');
        });

        // Delete customer
        _$customersTable.on('click', '.js-delete', function () {
            let customerId = $(this).data('customer-id');
            let deletedby = $(this).data('deletedby');
            bootbox.confirm({
                title: 'Delete Patient',
                message: "Do you want to delete this Patient?",
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
                        const url = deleteCustomerUrl;
                        const data = {
                            ID: customerId,
                            DeletedBy: deletedby
                        };
                        try {
                            const resp = await $.post(url, data);
                            const { Status: status, Message: message } = resp;
                            if (status) {
                                toastr.success(message, "", { timeOut: 500 });
                                getCustomers();
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

        // Load Customers
        getCustomers();
    });
    async function getCustomers() {
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

        _$customersTable.DataTable().destroy();
        const table = _$customersTable.DataTable({
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
                url: getCustomersUrl,
                type: "POST",
                data: {
                },
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    var d = $.parseJSON(json.data);
                    json.data = d;
                    var return_data = new Array();
                    for (var i = 0; i < d.length; i++) {
                        // Format DOB to dd/mm/yyyy for data-dob attribute
                        let formattedDob = d[i].DOB ? moment(d[i].DOB).format('DD/MM/YYYY') : '';
                        return_data.push({
                            'MR_No': d[i].MR_No,
                            'Name': d[i].Name,
                            'Phone': d[i].Phone,
                            'CNIC': d[i].CNIC,
                            'City': d[i].City,
                            'Address': d[i].Address,
                            'DOB': d[i].DOB, // Keep original for column display if needed
                            'Gender': d[i].Gender,
                            'Actions': `<div class="btn-group-sm action-btns">
                            ${customerPermissions.canUpdate ?
                                    `<button class="btn btn-default btn-sm js-edit"
                                    data-customer-id="${d[i].ID || 0}"
                                    data-name="${d[i].Name || ''}"
                                    data-phone="${d[i].Phone || ''}"
                                    data-cnic="${d[i].CNIC || ''}"
                                    data-city="${d[i].City || ''}"
                                    data-address="${d[i].Address || ''}"
                                    data-dob="${formattedDob}"  // Use formatted DOB here
                                    data-gender="${d[i].Gender || ''}"
                                    title="Edit Customer" data-toggle="tooltip">
                                    <i class="fa fa-edit"></i>
                                </button>` : ''}
                            ${customerPermissions.canDelete ?
                                    `<button class="btn btn-sm btn-danger js-delete" data-toggle="tooltip" data-customer-id="${d[i].ID}" data-deletedby="${d[i].Name}" title="Delete Customer">
                                    <i class="fa fa-trash"></i>
                                </button>` : ''}
                        </div>`
                        });
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "MR_No" },
                { "data": "Name" },
                { "data": "Phone" },
                { "data": "CNIC" },
                { "data": "City" },
                { "data": "Address" }, 
                { "data": "DOB", },
                { "data": "Gender" },
                { "data": "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Name, Phone, CNIC"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search By Name, Phone, CNIC')
            .addClass('seachbar-tooltip');
    }

    // Serialize form to object
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