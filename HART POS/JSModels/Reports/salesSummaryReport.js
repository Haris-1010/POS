(function ($) {
    const _$filterForm = $('#filterForm');
    const _$table = $('#reportTable');

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
            getSalesSummaryReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('select[name="SalePersonUsername"]').val('').trigger('change');
            _$filterForm.find('select[name="CategoryID"]').val('').trigger('change');
            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');
            _$filterForm.find('select[name="DiscountID"]').val('').trigger('change');

            _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            startDate = moment().startOf('month');
            endDate = moment().endOf('month');

            _$filterForm.find('input[name="Date"]').val(
                startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY')
            );

            getSalesSummaryReport('', false);
        });

        getSalesSummaryReport('', false)

        registerExcelExport('reportTable', getSalesSummaryReport, 1000);
    });

    async function getSalesSummaryReport(groupByColumn, isSearch = false, pageLimit = 10) {
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
            start: 10,
            paging: true,
            "ajax": {
                url: GetPagedSalesSummaryReportUrl,
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
                                'Name': d[i].Name,
                                'Category': d[i].Category,
                                'Qty': addCommasToNumber(d[i].Qty),
                                'PurchaseRate': `${currencySymbol} ${addCommasToNumber(d[i].PurchaseRate)}`,
                                'SaleRate': `${currencySymbol} ${addCommasToNumber(d[i].SaleRate)}`,
                                'GrossAmount': `${addCommasToNumber(d[i].GrossAmount)}`,
                                'Discount': `${addCommasToNumber(d[i].Discount)}`,
                                'NetSale': `${addCommasToNumber(d[i].NetSale > 0 ? d[i].NetSale : 0)}`,
                                'Profit': `${addCommasToNumber(d[i].Profit)}`,
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "Name" },
                { data: "Category" },
                { data: "Qty" },
                { data: "PurchaseRate" },
                { data: "SaleRate" },
                { data: "GrossAmount" },
                { data: "Discount" },
                { data: "NetSale" },
                { data: "Profit" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Product Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();

                calculateAndDisplayFooter(api, 2, 'Qty');
                calculateAndDisplayFooter(api, 5, 'GrossAmount', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 6, 'Discount', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 7, 'NetSale', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 8, 'Profit', currencySymbol + ' ', true);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Product Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery)