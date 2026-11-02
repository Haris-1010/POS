 (function ($) {
    const _$invoicesTable = $('#invoicesTable'),
        _$filterForm = $("#filterForm"),
        _$salesInfoContainer = $("#salesInfoContainer"),
        _$customerSearchElement = _$filterForm.find('.js-search-customer'),
        saleInvoicesPermissionForm = $('#SaleInvoicesPermissionsForm').serializeObject();
    let saleInvoicesPermission = {
        canUpdate: "1",
        canWhatsapp: "1",
        canDelete: "1",
        canPrint: "1",
        canReturn: "1",
    };

    let startDate, endDate, customerId;
     
    var invoiceDataStart = $('#saleInvoiceData').data('startdate');
    var invoicesDataEnd = $('#saleInvoiceData').data('enddate');
    var customerIdAttr = $('#customerData').data('customer');
     
    if ((invoiceDataStart && invoicesDataEnd) || customerIdAttr  ) {
        if (invoiceDataStart && invoicesDataEnd) {
            startDate = moment(invoiceDataStart, 'YYYY-MM-DD');
            endDate = moment(invoicesDataEnd, 'YYYY-MM-DD');

            _$filterForm.find('input[name="Date"]').daterangepicker({
                opens: 'right',
                startDate: startDate,
                endDate: endDate,
            });
        } else {
            // Fallback when only customerId comes
            startDate = moment().subtract(29, 'days');
            endDate = moment();
        }

        if (customerIdAttr) {
            customerId = customerIdAttr;
            _$filterForm.find('input[name="CustomerID"]').val(customerId);
            _$customerSearchElement.val('');
        }
        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getInvoices(true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('.js-select2').val(null).trigger('change');
            startDate = moment().subtract(29, 'days');
            endDate = moment();
            getInvoices(false);
        });

        getSalesInfo();
        getInvoices(true);
    }


    else {
        $(function () {
            _$filterForm.find('input[name="Date"]').daterangepicker({
                opens: 'right',
                startDate: moment().subtract(29, 'days'),
                endDate: moment(),
                ranges: {
                    'Today': [moment(), moment()],
                    'Yesterday': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
                    'Last 7 Days': [moment().subtract(6, 'days'), moment()],
                    'Last 30 Days': [moment().subtract(29, 'days'), moment()],
                    'This Month': [moment().startOf('month'), moment().endOf('month')],
                    'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
                }
            }, function (start, end, label) {
                startDate = start;
                endDate = end;
            });

            _$filterForm.on('click', '.js-apply-filter-btn', function () {
                getInvoices(true);
            });

            _$filterForm.on('click', '.js-clear-filter-btn', function () {
                _$filterForm[0].reset();
                _$filterForm.find('.js-select2').val(null).trigger('change');
                startDate = moment().subtract(29, 'days');
                endDate = moment();
                getInvoices(false);
            });

            startDate = moment().subtract(29, 'days');
            endDate = moment();

            getInvoices(false);
            getSalesInfo();
        });
    }

    async function getSalesInfo() {
        const url = GetSalesInfoUrl;
        const data = {
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: responseData } = resp;
            if (status) {
                _$salesInfoContainer.find('.js-total-invoices').text(responseData.TotalInvoices);
                _$salesInfoContainer.find('.js-paid').text(responseData.TotalPaidAmount);
                _$salesInfoContainer.find('.js-pending').text(responseData.TotalPendingAmount);
            }
        } catch (error) {
            toastr.error("An error occurred while deleting the Patient.", "", { timeOut: 500 });
        }
    }

    async function getInvoices(isSearch = false) {
        let currencySymbol = $("#currency").val();
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

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            formData.StartDate = startDate.format('YYYY-MM-DD');
            formData.EndDate = endDate.format('YYYY-MM-DD');
        } else {
            formData = {
            };
        }

        _$invoicesTable.DataTable().destroy();
        const table = _$invoicesTable.DataTable({
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
            lengthMenu: [10, 25, 50, 100,1000],
            pageLength: 10,
            processing: true,
            ordering: false,
            serverSide: true,
            lengthChange: true,
            searching: true,
            start: 10,
            paging: true,
            "ajax": {
                url: GetPagedSaleUrl,
                type: "POST",
                data: formData,
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    var d = $.parseJSON(json.data);
                    json.data = d;
                    var return_data = new Array();
                    for (var i = 0; i < d.length; i++) {
                        return_data.push({
                            InvoiceNo: d[i].InvoiceNo,
                            InvoiceDate: d[i].InvoiceDate,
                            CustomerName: d[i].CustomerName,
                            NetTotal: `${addCommasToNumber(d[i].NetTotal)}`,
                            PaidAmount: `${addCommasToNumber(d[i].PaidAmount)}`,
                            RemainingAmount: `${addCommasToNumber(d[i].RemainingAmount)}`,
                            'Actions': `<div class="btn-group-sm action-btns">
                                ${saleInvoicesPermission.canUpdate ? `
                                    <a href="${manageInvoiceUrl}?InvoiceID=${d[i].ID}"
                                       class="btn btn-default btn-sm js-edit"
                                       title="Edit Patient"
                                       data-toggle="tooltip">
                                        <i class="fa fa-edit"></i>
                                    </a>` : ''}

                                ${saleInvoicesPermission.canDelete ? `
                                    <button class="btn btn-sm btn-danger js-delete"
                                            data-toggle="tooltip"
                                            data-id="${d[i].ID}"
                                            title="Delete Patient">
                                        <i class="fa fa-trash"></i>
                                    </button>` : ''}

                                ${saleInvoicesPermission.canPrint ? `
                                    <a class="btn btn-sm btn-dark text-white"
                                       title="Print Sale"
                                       data-toggle="tooltip"
                                       href="${printSaleInvoiceThermalUrl}?ID=${d[i].ID}"
                                       target="_blank">
                                        <i class="fas fa-print"></i>
                                    </a>` : ''}

                                ${saleInvoicesPermission.canReturn ? `
                                    <a class="btn btn-sm btn-success text-white"
                                       title="Sale Return"
                                       data-toggle="tooltip"
                                       href="${manageInvoiceReturnUrl}?ID=${d[i].ID}">
                                        <i class="fas fa-undo"></i>
                                    </a>` : ''}
                            </div>`

                            /*<a class="btn btn-sm btn-dark text-white" title="Print Sale" data-toggle="tooltip" href="/Pharmacy/Print/PrintSaleInvoiceThermal?ID=${d[i].ID}"><i class="fas fa-print"></i> </a>*/
                        });
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "InvoiceNo" },
                { "data": "InvoiceDate" },
                { "data": "CustomerName" },
                { "data": "NetTotal" },
                { "data": "PaidAmount" },
                { "data": "RemainingAmount" },
                { "data": "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Invoice Number"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
                calculateAndDisplayFooter(api, 4, 'NetTotal', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 5, 'PaidAmount', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 6, 'RemainingAmount', currencySymbol + ' ', true);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search By Invoice Number')
            .addClass('seachbar-tooltip');
    }

    _$customerSearchElement.autocomplete({
        source: function (request, response) {
            $.ajax({
                url: SearchCustomerUrl,
                data: { SearchString: request.term },
                type: "POST",
                success: function (data) {
                    let results = $.map(data.Data, function (item) {
                        return {
                            label: `${item.CustomerName} | ${item.Phone} (${item.CustomerType})`,
                            value: item.CustomerName,
                            data: item
                        };
                    });
                    response(results);
                }
            });
        },
        minLength: 2,
        select: function (event, ui) {
            if (ui.item.data === null) {
                event.preventDefault();
                _$saveCustomerModal.modal("show");
            } else {
                let itemData = ui.item.data;
                _$filterForm.find('input[name="CustomerID"]').val(itemData.CustomerID);
                _$customerSearchElement.val(itemData.CustomerName);
            }
        },
        open: function () {
            $('.ui-autocomplete').css('z-index', 1000);
        }
    }).data("ui-autocomplete")._renderItem = function (ul, item) {
        return $("<li>")
            .append(`<div>${item.label}</div>`)
            .appendTo(ul);
    };

    //_$invoicesTable.on('click', '.js-send-whatsapp-sms', function () {
    //    const _$this = $(this);
    //    bootbox.confirm({
    //        title: 'Send Invoice Via Whatsapp',
    //        message: "Are you sure to send this invoice through Wathsapp?",
    //        size: 'small',
    //        buttons: {
    //            confirm: {
    //                label: 'Confirm',
    //                className: 'btn-success'
    //            },
    //            cancel: {
    //                label: 'Cancel',
    //                className: 'btn-danger'
    //            }
    //        },
    //        callback: async function (result) {
    //            if (result) {
    //                $('body').loading({ 'message': 'Sending SMS Please Wait...' })

    //                const url = '/Pharmacy/Sales/GetInvoicePrintShortURL';
    //                const obj = {
    //                    ID: _$this.data('id'),
    //                };
    //                try {
    //                    const resp = await $.get(url, obj);
    //                    const { Status: status, Message: message, Data: data } = resp;
    //                    if (status) {
    //                        let msg = `Dear *${_$this.data('patient-name')}*, Please click on the link below to view your Invoice details.Regards *${_$this.data('sale-person')}* .View Here:\n ${data}`;
    //                        let encodedMsg = encodeURIComponent(msg);
    //                        let phone = _$this.data('phone').replace(/\D/g, '');
    //                        if (phone.startsWith('0')) {
    //                            phone = '92' + phone.substring(1);
    //                        }
    //                        window.open(`https://web.whatsapp.com/send?phone=${phone}&text=${encodedMsg}`, '_blank');
    //                    } else {
    //                        toastr.error(message, "", { timeOut: 500 });
    //                    }
    //                } catch (error) {
    //                    toastr.error("An error occurred", "", { timeOut: 500 });
    //                } finally {
    //                    $('body').loading('stop');
    //                }
    //            }
    //        }
    //    });
    //});

    _$invoicesTable.on('click', '.js-send-whatsapp-sms', function () {
        const _$this = $(this);
        bootbox.confirm({
            title: 'Send Invoice',
            message: "Are you sure to send this invoice?",
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
                    $('body').loading({ 'message': 'Sending SMS Please Wait...' })

                    const url = SendInvoiceToCustomer;
                    const obj = {
                        ID: _$this.data('id'),
                        ReceiverName: _$this.data('patient-name'),
                        ReceiverPhone: _$this.data('phone').replace(/\D/g, '')
                    };

                    try {
                        const resp = await $.post(url, obj);
                        const { Status: status, Message: message, Data: data } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 1000 });
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("An error occurred", "", { timeOut: 500 });
                    } finally {
                        $('body').loading('stop');
                    }
                }
            }
        });
    });

    _$invoicesTable.on('click', '.js-delete', function () {
        let id = $(this).data('id');
        bootbox.confirm({
            title: 'Delete Invoice',
            message: "Do you want to delete this invoice?",
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
                    const url = DeleteSaleUrl;
                    const data = {
                        ID: id,
                    };
                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 1000 });
                            _$filterForm.trigger("reset");
                            getInvoices(false);
                        } else {
                            toastr.error(message, "", { timeOut: 1000 });
                        }
                    } catch (error) {
                        toastr.error("An error occurred while deleting the Patient.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });
})(jQuery);