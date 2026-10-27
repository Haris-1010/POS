(function ($) {
    const _$ordersTable = $('#ordersTable'),
        _$filterForm = $("#filterForm"),
        _$purchaseOrderCards = $("#purchaseOrderCards"),
        purchaseOrdersPermissionForm = $('#PurchaseOrdersPermissionsForm').serializeObject();
    let purchaseOrdersPermission = {
        canUpdate: "1",
        canWhatsapp: "1",
        canDelete: "1",
        canCreateReceivePuruchase: "1",
        canPrint: "1",
    };
    const _$modal = $('#saveClearDueModal');

    let startDate, endDate;

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

        _$modal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$modal.find('form')[0].reset();
        });

        $(document).on('click', '.filter-card', function () {
            const filterType = $(this).data('filter');
            getPurchaseOrders(false, filterType);
        });

        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getPurchaseOrders(true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('.js-select2').val(null).trigger('change');
            startDate = moment().subtract(29, 'days');
            endDate = moment();
            getPurchaseOrders(false);
        });

        startDate = moment().subtract(29, 'days');
        endDate = moment();
        getPurchaseOrdersInfo();
        getPurchaseOrders(false);
    })

    async function getPurchaseOrdersInfo() {
        const url = GetPurchaseOrdersInfoUrl;
        const data = { 
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: responseData } = resp;
            if (status) {
                _$purchaseOrderCards.find('.js-pending-orders').text(responseData.TotalPending);
                _$purchaseOrderCards.find('.js-ordered').text(responseData.TotalOrders);
                _$purchaseOrderCards.find('.js-received-orders').text(responseData.TotalReceived);
                _$purchaseOrderCards.find('.js-total-order').text(responseData.AllOrders);
            }
        } catch (error) {
            toastr.error("An error occurred while deleting the customer.", "", { timeOut: 500 });
        }
    }

    async function getPurchaseOrders(isSearch = false, filterType = null) {
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

        if (filterType) {
            formData.FilterType = filterType;
        }

        _$ordersTable.DataTable().destroy();
        const table = _$ordersTable.DataTable({
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
                url: GetPagedPurchaseOrdersUrl,
                type: "POST",
                data: formData,
                dataSrc: function (json) {
                    var return_data = new Array();
                    var pendingOrders = 0;
                    var ordered = 0;
                    var receivedOrders = 0;
                    var totalOrders = 0;

                    try {
                        json = jQuery.parseJSON(JSON.stringify(json));
                        var d = $.parseJSON(json.data);
                        json.data = d;
                        for (var i = 0; i < d.length; i++) {
                            totalOrders++;
                            if (d[i].Status === 'Pending') {
                                pendingOrders++;
                            } else if (d[i].Status === 'Ordered') {
                                ordered++;
                            } else if (d[i].Status === 'Received') {
                                receivedOrders++;
                            }
                            return_data.push({
                                'ReferenceNo': d[i].ReferenceNo,
                                'Date': d[i].Date,
                                'Supplier': d[i].SuplierName,
                                'Status': d[i].Status,
                                'TotalItems': d[i].TotalItems,
                                'SubTotal': `${addCommasToNumber(d[i].SubTotal)}`,
                                'Actions': getPurchaseOrderActions(d[i])
                            });
                        }

                        //$('.js-pending-orders').text(pendingOrders);
                        //$('.js-ordered').text(ordered);
                        //$('.js-received-orders').text(receivedOrders);
                        //$('.js-total-order').text(totalOrders);
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "ReferenceNo" },
                { data: "Date" },
                { data: "Supplier" },
                { data: "Status" },
                { data: "TotalItems" },
                { data: "SubTotal" },
                { data: "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Refrence No"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
                calculateAndDisplayFooter(api, 5, 'SubTotal', currencySymbol + ' ', true);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Reference No')
            .addClass('seachbar-tooltip');
    }

    _$ordersTable.on('click', '.js-delete', function () {
        let purchaseId = $(this).data('purchase-id');
        bootbox.confirm({
            title: 'Delete Purchase Order',
            message: "Do you want to delete this Purchase Order?",
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
                    const url = DeletePurchaseOrderUrl;
                    const data = {
                        ID: purchaseId, 
                    };
                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 1000 });
                            _$filterForm.trigger("reset");
                            getPurchaseOrders(false);
                        } else {
                            toastr.error(message, "", { timeOut: 1000 });
                        }
                    } catch (error) {
                        toastr.error("An error occurred while deleting the Purchase.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });

    let selectedClearDue = {};

    _$ordersTable.on('click', '.js-clear-due', function () {
        const $btn = $(this);

        const subTotal = parseFloat($btn.data('subtotal')) || 0;
        const paidAmount = parseFloat($btn.data('paid')) || 0;
        const remaining = parseFloat($btn.data('remaining')) || (subTotal - paidAmount);
        const purchaseID = $btn.data('purchaseid') || 0;

        selectedClearDue = {
            ID: $btn.data('id'),
            SubTotal: subTotal,
            PaidAmount: paidAmount,
            PurchaseID: purchaseID
        };

        /*const _$modal = $('#saveClearDueModal');*/

        _$modal.find('input[name="ID"]').val(selectedClearDue.ID);
        _$modal.find('input[name="PaidAmount"]').val(paidAmount.toFixed(2));
        _$modal.find('input[name="RemainingAmount"]').val(remaining.toFixed(2));

        _$modal.find('input[name="Date"]').datepicker({
            format: 'mm/dd/yyyy',
            autoclose: true,
            todayHighlight: true
        }).datepicker('setDate', new Date());
        _$modal.modal('show');
    });

    $('.js-clearDue-save-btn').on('click', async function () {
        const _$saveBtn = $(this);
        const enteredRemaining = parseFloat(_$modal.find('input[name="RemainingAmount"]').val()) || 0;

        const { SubTotal, PaidAmount, ID, PurchaseID } = selectedClearDue;
        const dueLeft = SubTotal - PaidAmount;

        if (enteredRemaining > dueLeft) {
            toastr.error(`Remaining amount cannot exceed PKR ${dueLeft.toFixed(2)}.`);
            return;
        }

        if (enteredRemaining < 0) {
            toastr.error("Remaining amount must be greater than 0.");
            return;
        }

        const data = {
            ID: ID,
            RemainingAmount: enteredRemaining,
            PurchasePayment: {
                Date: formatDateToISO(_$modal.find('input[name="Date"]').val()),
                PaidAmount: enteredRemaining,
                PaymentMode: _$modal.find('select[name="PaymentMode"]').val(),
                PurchaseID: PurchaseID,
                AttachmentURL: _$modal.find('input[name="AttachmentURL"]').val(),
            },
            LastModifiedBy: _$modal.find('input[name="LastModifiedBy"]').val(),
            
        };
        try {
            _$saveBtn.text('Save').prop('disabled', true);

            const url = ClearDueAmountUrl;
            const resp = await $.post(url, data);
            const { Status: status, Message: message } = resp;
            if (status) {
                toastr.success(message, "", { timeOut: 1000 });
                $('.modal').modal('hide');
                _$modal.modal({
                    backdrop: 'static',
                    keyboard: false
                });
                _$modal.find('input[name="Date"]').datepicker('setDate', new Date());
                const $clearBtn = $(`.js-clear-due[data-id="${ID}"]`);

                if ($clearBtn.length) {
                    const updatedPaid = PaidAmount + enteredRemaining;
                    const updatedRemaining = SubTotal - updatedPaid;

                    // If fully paid, remove the button
                    if (updatedRemaining <= 0) {
                        $clearBtn.closest('.action-btns').find('.js-clear-due').remove();
                    } else {
                        // Update data attributes if partially paid
                        $clearBtn
                            .data('paid', updatedPaid)
                            .data('remaining', updatedRemaining);
                    }
                }
            } else {
                toastr.error(message, "", { timeOut: 1000 });
            }
        } catch (err) {
            toastr.error("An error occurred while clear due Amount.", "", { timeOut: 500 });
        } finally {
            _$saveBtn.text('Save').prop('disabled', false);
        }
    });
    function getPurchaseOrderActions(order) {
        let actions = `<div class="btn-group-sm action-btns">`;

        if (order.IsPurchaseReceived) {
            actions += `
            <span class="badge badge-success p-2">
                <i class="fas fa-check-circle"></i> Purchase Received
            </span>
            ${order.RemainingAmount > 0 ? `
                <button class="btn btn-sm btn-warning js-clear-due" title="Clear Due Amount"
                    data-id="${order.ID}"
                    data-purchaseid="${order.PurchaseID}"
                    data-subtotal="${order.SubTotal}"
                    data-paid="${order.PaidAmount}"
                    data-remaining="${order.RemainingAmount}">
                    <i class="fas fa-money-bill-wave"></i> Clear Due
                </button>` : ''}
        `;
        } else {
            actions += `
            ${purchaseOrdersPermission.canUpdate ? `
                <a class="btn btn-sm btn-default" 
                   title="Edit Purchase" 
                   data-toggle="tooltip"
                   href="${managePurchaseOrderUrl}?ID=${order.ID}">
                    <i class="fas fa-edit"></i>
                </a>` : ''}

            ${purchaseOrdersPermission.canDelete ? `
                <button class="btn btn-sm btn-danger js-delete" 
                        data-toggle="tooltip"
                        data-purchase-id="${order.ID}" 
                        title="Delete Purchase">
                    <i class="fa fa-trash"></i>
                </button>` : ''}

            ${purchaseOrdersPermission.canCreateReceivePuruchase ? `
                <a class="btn btn-sm btn-primary text-white" 
                   data-toggle="tooltip"
                   href="${managePurchaseUrl}?PurchaseOrderID=${order.ID}"
                   title="Receive Purchase">
                    Receive Purchase
                </a>` : ''}

            ${purchaseOrdersPermission.canPrint ? `
                <a class="btn btn-sm btn-dark text-white" 
                   title="Print Order" 
                   data-toggle="tooltip"
                   href="${printPurchaseOrderUrl}?ID=${order.ID}" 
                   target="_blank">
                    <i class="fas fa-print"></i>
                </a>` : ''}
        `;
        }

        actions += `</div>`;
        return actions;
    }


    //_$ordersTable.on('click', '.js-send-whatsapp-sms', function () {
    //    const _$this = $(this);
    //    bootbox.confirm({
    //        title: 'Send PurchaseOrder Via Whatsapp',
    //        message: "Are you sure to send this Order through Whatsapp?",
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

    //                const url = '/Pharmacy/Purchases/GetPurchasePrintShortURL';
    //                const obj = {
    //                    id: _$this.data('id'),
    //                };
    //                try {
    //                    const resp = await $.get(url, obj);
    //                    const { Status: status, Message: message, Data: data } = resp;
    //                    if (status) {
    //                        let msg = `Dear *${_$this.data('suplier-name')}*, Please click on the link below to view your Purchase Order details.Regards *Instacare* .View Here:\n ${data}`;
    //                        let encodedMsg = encodeURIComponent(msg);
    //                        let phone = _$this.data('phone').replace(/\D/g, '');
    //                        if (phone.startsWith('0')) {
    //                            phone = '92' + phone.substring(1);
    //                        }
    //                        window.open(`https://web.whatsapp.com/send?phone=${phone}&text=${encodedMsg}`, '_blank');
    //                    } else {
    //                        show_notification("danger", "", message);
    //                    }
    //                } catch (error) {
    //                    show_notification("danger", "", "An error occurred");
    //                } finally {
    //                    $('body').loading('stop');
    //                }
    //            }
    //        }
    //    });
    //});

    _$ordersTable.on('click', '.js-send-whatsapp-sms', function () {
        const _$this = $(this);
        bootbox.confirm({
            title: 'Send PurchaseOrder',
            message: "Are you sure to send this Order?",
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

                    const url = SendPurchaseOrderToSupplierUrl;
                    const obj = {
                        ID: _$this.data('id'),
                        ReceiverName: _$this.data('suplier-name'),
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

    function formatDateToISO(dateString) {
        const date = new Date(dateString);
        const yyyy = date.getFullYear();
        const MM = String(date.getMonth() + 1).padStart(2, '0');
        const dd = String(date.getDate()).padStart(2, '0');
        const HH = String(date.getHours()).padStart(2, '0');
        const mm = String(date.getMinutes()).padStart(2, '0');
        const ss = String(date.getSeconds()).padStart(2, '0');

        return `${yyyy}-${MM}-${dd} ${HH}:${mm}:${ss}`;
    }
})(jQuery)