(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable');

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
            getManufacturerWiseSalesReport('Manufacturer', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');

            _$filterForm.find('select[name="SortBy"]').val('Manufacturer').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            startDate = moment().startOf('month');
            endDate = moment().endOf('month');
            _$filterForm.find('input[name="Date"]').val(
                startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY')
            );

            getManufacturerWiseSalesReport('Manufacturer', false);
        });

        getManufacturerWiseSalesReport('Manufacturer', false)

        registerExcelExport('reportTable', getManufacturerWiseSalesReport, 1000);
    });

    async function getManufacturerWiseSalesReport(groupByColumn, isSearch = false, pageLimit = 10) {
        let currencySymbol = $("#currency").val();
        if ($.fn.DataTable.isDataTable(_$table)) {
            _$table.DataTable().destroy();
        }

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject(),
                formData.StartDate = startDate.format('YYYY-MM-DD');
            formData.EndDate = endDate.format('YYYY-MM-DD');
            formData.GroupByColumn = groupByColumn;
        } else {
            formData = {
                StartDate: startDate.format('YYYY-MM-DD'),
                EndDate: endDate.format('YYYY-MM-DD'),
                GroupByColumn: groupByColumn,
            };
        }

        _$table.DataTable({
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
            searching: true,
            paging: true,
            ajax: {
                url: GetManufacturerWiseSalesReportUrl,
                type: 'POST',
                data: formData,
                dataSrc: function (json) {
                    const return_data = [];
                    try {
                        const d = $.parseJSON(json.data);
                        for (let i = 0; i < d.length; i++) {
                            return_data.push({
                                'Manufacturer': d[i].Manufacturer,
                                'ProductName': d[i].ProductName,
                                'Qty': d[i].Qty,
                                'NetSale': addCommasToNumber(d[i].NetSale || 0)
                            });
                        }
                    } catch (ex) {
                        console.error('Error parsing data:', ex);
                    }
                    return return_data;
                }
            },
            columns: [
                { data: 'Manufacturer' },
                { data: 'ProductName' },
                { data: 'Qty' },
                { data: 'NetSale' },
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalQty = 0;
                        let totalNetSale = 0;

                        // Calculate the sum for each group
                        rows.every(function (rowIdx) {
                            const data = this.data();
                            totalQty += data.Qty;
                        });

                        rows.every(function (rowIdx) {
                            const data = this.data();
                            totalNetSale += parseNumber(data.NetSale);
                        });

                        // Return the custom group header with totals
                        return `Manufacturer Name: ${group} (Total Qty Sold: ${addCommasToNumber(totalQty)} | Total Sale: ${currencySymbol} ${addCommasToNumber(totalNetSale)})`;
                    }
                }
                : false,
            language: {
                emptyTable: 'No records found',
                searchPlaceholder: 'Search By Manufacturer Or Product Name'
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
                calculateAndDisplayFooter(api, 3, 'NetSale', currencySymbol + ' ', true);
            }
        });
    }
})(jQuery)