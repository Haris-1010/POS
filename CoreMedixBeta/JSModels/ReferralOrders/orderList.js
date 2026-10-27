(function ($) {
    const _$table = $('#reportTable'),
        _$referredBySearchElement = $('.js-search-referred-by');
    _$filterForm = $('#filterForm');

    const permissionsForm = $('#permissionsForm').serializeObject();

    let permissions = {
        isEMRIntegration: "1",
    };

    let startDate, endDate;
    $(function () {
        _$filterForm.find('input[name="Date"]').daterangepicker({
            opens: 'right',
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
            const groupByColumn = _$filterForm.find('input[name="GroupBy"]:checked').val() || null;
            getOrderList(groupByColumn, true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('.js-select2').val(null).trigger('change');
            _$filterForm.find('input[type="radio"]').prop('checked', false);
            getOrderList('', false);
        });

        startDate = moment();
        endDate = moment();

        getOrderList('', false);
    });

    _$referredBySearchElement.keyup(function () {
        if (!$(this).val().trim()) {
            _$filterForm.find('input[name="ReferredByID"]').val('');
            _$filterForm.find('input[name="ReferredByType"]').val('');
        }
    });
    /*
    _$referredBySearchElement.autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/Pharmacy/ReferralContacts/SearchContact/',
                data: { SearchString: request.term },
                type: "POST",
                success: function (data) {
                    let results = $.map(data.Data, function (item) {
                        return {
                            label: `${item.ContactName} | ${item.Phone} (${item.ContactType})`,
                            value: item.ContactName,
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
                _$saveContactModal.modal("show");
            } else {
                let itemData = ui.item.data;
                _$filterForm.find('input[name="ReferredByID"]').val(itemData.ContactID);
                _$filterForm.find('input[name="ReferredByType"]').val(itemData.ContactType);
                _$referredBySearchElement.val(itemData.ContactName);
            }
        },
        open: function () {
            $('.ui-autocomplete').css('z-index', 1000);
        }
    }).data("ui-autocomplete")._renderItem = function (ul, item) {
        let content = `<div>${item.label}</div>`;

        return $("<li>")
            .append(content)
            .appendTo(ul);
    };
    */
    async function getOrderList(groupByColumn, isSearch = false) {
        let currencySymbol = $("#currency").val();

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            formData.StartDate = startDate.format('YYYY-MM-DD');
            formData.EndDate = endDate.format('YYYY-MM-DD');
            formData.GroupByColumn = groupByColumn;
        } else {
            formData = {
            };
        }

        _$table.DataTable().destroy();
        const table = _$table.DataTable({
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
                url: GetPagedOrdersUrl,
                type: "POST",
                data: formData,
                dataSrc: function (json) {
                    var return_data = new Array();
                    try {
                        json = jQuery.parseJSON(JSON.stringify(json));
                        var d = $.parseJSON(json.data);
                        json.data = d;
                        for (var i = 0; i < d.length; i++) {
                            return_data.push({
                                'OrderOn': moment(d[i].OrderOn).format('DD MMMM YYYY | hh:mm A'),
                                'PatientName': d[i].PatientName,
                                'PatientPhone': d[i].PatientPhone,
                                'MR_No': d[i].MR_No,
                                'ReferredByName': d[i].ReferredByName ? d[i].ReferredByName : "-",
                                'Source': d[i].Source,
                                'Actions': `<div class="btn-group-sm action-btns">
    <a href="/PrintPub/BreifPrescription/?id=${d[i].PrescriptionID}" class="btn btn-dark btn-sm" target="_blank" title="View Prescription" data-toggle="tooltip">
        <i class="fa fa-eye"></i>
    </a>
    ${d[i].IsConvertedToInvoice
                                        ? `<span class="badge badge-success" title="Already Converted to Invoice" data-toggle="tooltip">
               Converted To Invoice
           </span>`
                                        : `<a class="btn btn-sm btn-success text-white" title="Convert To Invoice" data-toggle="tooltip" 
             href="${manageInvoiceUrl}?ReferralOrderID=${d[i].ID}
             &ReferredByID=${d[i].ReferredByID}
             &ReferredByType=${d[i].ReferredByType}
             &ReferredByName=${d[i].ReferredByName}
             &CustomerID=${d[i].PatientUsername}
             &CustomerName=${d[i].PatientName}
             &CustomerType=Patient">
             <i class="fas fa-file-invoice-dollar"></i>
           </a>`
                                    }
</div>`
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "OrderOn" },
                { data: "PatientName" },
                { data: "PatientPhone" },
                { data: "MR_No" },
                { data: "ReferredByName" },
                { data: "Source" },
                { data: "Actions" }
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        //let totalQty = 0;
                        //let totalLineTotal = 0;

                        //// Calculate the sum for each group
                        //rows.every(function (rowIdx) {
                        //    const data = this.data();
                        //    totalQty += parseNumber(data.Qty);
                        //});

                        //rows.every(function (rowIdx) {
                        //    const data = this.data();
                        //    totalLineTotal += parseNumber(data.LineTotal);
                        //});

                        //// Return the custom group header with totals
                        //return `${group} (Total Qty: ${addCommasToNumber(totalQty)} | Total Line Total: ${currencySymbol} ${addCommasToNumber(totalLineTotal)})`;
                    }
                }
                : false,
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Patient Name,Phone,Mr"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Patient Name,Phone,Mr')
            .addClass('seachbar-tooltip');
    }
})(jQuery)