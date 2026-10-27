(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable'),
        _$searchItemElement = $('.js-search-item');

    let startDate = moment().startOf('month');
    let endDate = moment().endOf('month');

    window.reportDateRange = {
        getStart: () => startDate,
        getEnd: () => endDate,
        set: (s, e) => { startDate = s; endDate = e; }
    };

    $(function () {
        _$filterForm.find('input[name="Date"]').daterangepicker({
            opens: 'right',
            startDate: startDate,
            endDate: endDate,
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

        _$filterForm.find('input[name="Date"]').val(
            startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY')
        );
        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getPurchaseReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('input[name="ItemID"]').val('');
            _$filterForm.find('input[name="VariationID"]').val('');

            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="BrandID"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');
            _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            startDate = moment().startOf('month');
            endDate = moment().endOf('month');
            _$filterForm.find('input[name="Date"]').val(
                startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY')
            );
            getPurchaseReport('', false);
        });

        getPurchaseReport('', false)

        registerExcelExport('reportTable', getPurchaseReport, 1000);

        _$searchItemElement.autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: SearchItemsUrl,
                    data: {
                        SearchString: request.term,
                        SearchType: "",
                        NearToExpire: false,
                        ManufacturerID: "0",
                        SupplierID: "0"
                    },
                    type: "POST",
                    success: function (data) {
                        let results = $.map(data.Data, function (item) {
                            let rackInfo = item.Rack ? ` - Rack(${item.Rack})` : '';
                            let expiryStatus = item.ExpiryStatus === "Expired" ? " [Expired]" : "";
                            return {
                                label: `${item.ItemName} - ${item.VariationName}`,
                                value: item.ItemName,
                                data: item
                            };
                        });
                        response(results);
                    }
                });
            },
            minLength: 2,
            select: function (event, ui) {
                let itemData = ui.item.data;
                setTimeout(function () {
                    _$searchItemElement.val(itemData.ItemName); //VariationID//ItemName
                    _$filterForm.find('input[name="ItemID"]').val(itemData.ItemID);
                    _$filterForm.find('input[name="VariationID"]').val(itemData.VariationID);
                }, 10);
                //addItemToItemsTable(itemData);
            },
            open: function () {
                $('.ui-autocomplete').css('z-index', 1050);
            }
        }).data("ui-autocomplete")._renderItem = function (ul, item) {
            return $("<li>")
                .append(`<div>${item.label}</div>`)
                .appendTo(ul);
        };
    });

    async function getPurchaseReport(groupByColumn, isSearch = false, pageLimit = 10) {
        let currencySymbol = $("#currency").val();

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            formData.StartDate = startDate.format('YYYY-MM-DD');
            formData.EndDate = endDate.format('YYYY-MM-DD');
            formData.GroupByColumn = groupByColumn;
        } else {
            formData = {
                StartDate: startDate.format('YYYY-MM-DD'),
                EndDate: endDate.format('YYYY-MM-DD'),
            };
        }

        _$table.DataTable().destroy();
        const table = _$table.DataTable({
            dom: 'Blfrtip',
            buttons: [
                'copy',
                {
                    text: 'Excel',
                    className: 'btn-open-excel-modal',
                    action: function () {
                        $('#reportTarget').val('reportTable'); // Set which table to export
                        $('#excelExportModal').modal('show');
                    }
                },
                {
                    extend: 'csv',
                    className: 'btn-download-excel', // hidden button, does the real export
                    exportOptions: {
                        columns: ':visible'
                    }
                },
                'pdf',
                // Print Button funtionality => Print all applied filter's and control table th, td font-size
                getGenericPrintButton('#filterForm'),
                {
                    extend: 'colvis',
                    text: 'Columns'
                },
            ],
            lengthMenu: [10, 25, 50, 100, 500, 1000],
            pageLength: pageLimit ? pageLimit : 10,
            processing: true,
            ordering: false,
            serverSide: true,
            lengthChange: true,
            searching: true,
            paging: true,
            ajax: {
                url: GetPurchaseReportUrl,
                type: "POST",
                data: formData,
                dataSrc: function (json) {
                    var return_data = [];
                    try {
                        json = jQuery.parseJSON(JSON.stringify(json));
                        var d = $.parseJSON(json.data);
                        json.data = d;
                        for (var i = 0; i < d.length; i++) {
                            return_data.push({
                                'ProductName': d[i].ProductName,
                                'InvoiceNo': d[i].InvoiceNo,
                                'OrderDate': moment(d[i].OrderDate).format('DD MMM YYYY'),
                                'PurchaseReceivedAt': d[i].PurchaseReceivedAt ? moment(d[i].PurchaseReceivedAt).format('DD MMM YYYY') : 'N/A',
                                'Status': d[i].Status,
                                'Supplier': d[i].Supplier,
                                'Batch': d[i].Batch ?? 'N/A',
                                'Qty': addCommasToNumber(d[i].Qty),
                                'UnitCost': `${currencySymbol} ${addCommasToNumber(d[i].UnitCost)}`,
                                'LineTotal': `${addCommasToNumber(d[i].LineTotal)}`
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            columns: [
                { data: "ProductName", title: "Product" },
                { data: "InvoiceNo", title: "Invoice No" },
                { data: "OrderDate", title: "Order Date" },
                { data: "PurchaseReceivedAt", title: "Received At" },
                { data: "Status", title: "Status" },
                { data: "Supplier", title: "Supplier" },
                { data: "Batch", title: "Batch" },
                { data: "Qty", title: "Qty" },
                { data: "UnitCost", title: "Unit Price" },
                { data: "LineTotal", title: "Total" }
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalQty = 0;
                        let totalLineTotal = 0;

                        rows.every(function () {
                            const data = this.data();
                            totalQty += parseNumber(data.Qty);
                            totalLineTotal += parseNumber(data.LineTotal);
                        });

                        return `${group} (Total Qty: ${addCommasToNumber(totalQty)} | Total Amount: ${currencySymbol} ${addCommasToNumber(totalLineTotal)})`;
                    }
                }
                : false,
            language: {
                emptyTable: "No records found",
                searchPlaceholder: "Search by Invoice No, Product, or Supplier"
            },
            drawCallback: function () {
                var api = this.api();
                var info = api.page.info();
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate'); 
                pagination.toggle(info.pages > 1); 
                pagination.find('.paginate_button.previous').toggle(info.page > 0);
                 
                pagination.find('.paginate_button.next').toggle(info.page < info.pages - 1);
                 
                renderReportFooterNote(api);
            },

            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
                calculateAndDisplayFooter(api, 7, 'Qty');
                calculateAndDisplayFooter(api, 9, 'LineTotal', currencySymbol + ' ', true);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search by Invoice No or Product')
            .addClass('seachbar-tooltip');
    }
})(jQuery)