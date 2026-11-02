(function ($) {
    const _$filterForm = $('#filterForm');
    const _$table = $('#reportTable');
    $(function () {
        _$filterForm.find('input[name="Date"]').datepicker('setDate', new Date());
        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getDailySalesReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="BrandID"]').val('').trigger('change');
            _$filterForm.find('select[name="SalePersonUsername"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');

            _$filterForm.find('select[name="PaymentMode"]').val('Cash').trigger('change');
            _$filterForm.find('select[name="SortBy"]').val('InvoiceNo').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            _$filterForm.find('input[name="Date"]').datepicker('setDate', new Date());

            getDailySalesReport('', false);
        });

        getDailySalesReport('', false)
    });
    async function getDailySalesReport(groupByColumn, isSearch = false) {
        let currencySymbol = $("#currency").val();

        let formData = {};
        const $dateInput = _$filterForm.find('input[name="Date"]');
        if (isSearch) {
            formData = _$filterForm.serializeObject();
            formData.StartDate = moment($dateInput.val(), 'MM/DD/YYYY').format('YYYY-MM-DD');
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
                // Print Button funtionality => Print all applied filter's and control table th, td font-size
                getGenericPrintButton('#filterForm'),
                {
                    extend: 'colvis',
                    text: 'Columns'
                },
            ],
            lengthMenu: [10, 25, 50, 100, 500, 1000],
            pageLength: 10,
            processing: true,
            ordering: false,
            serverSide: true,
            lengthChange: true,
            searching: true,
            start: 10,
            paging: true,
            "ajax": {
                url: GetPagedDailySalesReportUrl,
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
                                'InvoiceNo': d[i].InvoiceNo,
                                'DateTime': moment(d[i].DateTime, "MM/DD/YYYY HH:mm:ss").format('DD MMMM YYYY'),
                                'CustomerName': d[i].CustomerName,
                                'ItemsCount': d[i].ItemsCount,
                                'TotalQty': addCommasToNumber(d[i].TotalQty),
                                'GrossAmount': `${addCommasToNumber(d[i].GrossAmount)}`,
                                'Discount': addCommasToNumber(d[i].Discount),
                                'NetAmount': `${addCommasToNumber(d[i].NetAmount)}`
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "InvoiceNo" },
                { data: "DateTime" },
                { data: "CustomerName" },
                { data: "ItemsCount" },
                { data: "TotalQty" },
                { data: "GrossAmount" },
                { data: "Discount" },
                { data: "NetAmount" }
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalQty = 0;
                        let grossTotal = 0;
                        let netAmount = 0;

                        // Calculate the sum for each group
                        rows.every(function (rowIdx) {
                            const data = this.data();
                            totalQty += parseNumber(data.TotalQty);
                        });

                        rows.every(function (rowIdx) {
                            const data = this.data();
                            grossTotal += parseNumber(data.GrossAmount);
                        });
                        rows.every(function (rowIdx) {
                            const data = this.data();
                            netAmount += parseNumber(data.NetAmount);
                        });

                        // Return the custom group header with totals
                        return `${group} (Total Qty: ${addCommasToNumber(totalQty)} | Total Gross Total: ${currencySymbol} ${addCommasToNumber(totalLineTotal)}
                                           | Total Net Total: ${currencySymbol} ${addCommasToNumber(NetAmount)})`;
                    }
                }
                : false,
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Invoice No"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();

                calculateAndDisplayFooter(api, 4, 'TotalQty');
                calculateAndDisplayFooter(api, 5, 'GrossAmount', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 6, 'Discount', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 7, 'NetAmount', currencySymbol + ' ', true);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Invoice No')
            .addClass('seachbar-tooltip');
    }
})(jQuery)