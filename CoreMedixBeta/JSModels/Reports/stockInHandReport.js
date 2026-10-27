(function ($) {
    const _$table = $('#reportTable'),
        _$filterForm = $('#filterForm');

    let startDate, endDate;

    window.reportDateRange = {
        getStart: () => startDate,
        getEnd: () => endDate,
        set: (s, e) => { startDate = s; endDate = e; }
    };

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

        _$filterForm.find('input[name="Date"]').val('');

        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            const groupByColumn = _$filterForm.find('input[name="GroupBy"]:checked').val() || null;
            getStockInHandReport(groupByColumn, true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="BrandID"]').val('').trigger('change'); 

            _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');
            _$filterForm.find('select[name="QtyFilter"]').val('0').trigger('change');
            _$filterForm.find('input[name="QtyValue"]').val('')

            _$filterForm.find('input[name="Date"]').val('');

            _$filterForm.find('input[type="radio"]').prop('checked', false);
            getStockInHandReport('', false);
        });

        getStockInHandReport('', false);

        registerExcelExport('reportTable', getStockInHandReport, 1000);
    });

    async function getStockInHandReport(groupByColumn, isSearch = false, pageLimit = 10) {
        let currencySymbol = $("#currency").val();
        if ($.fn.DataTable.isDataTable(_$table)) {
            _$table.DataTable().destroy();
        }

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            if (startDate && endDate) {
                formData.StartDate = startDate.format('YYYY-MM-DD');
                formData.EndDate = endDate.format('YYYY-MM-DD');
            }
            formData.GroupByColumn = groupByColumn;
        } else {
            formData = {
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
            lengthMenu: [10, 25, 50, 100, 1000],
            pageLength: pageLimit ? pageLimit : 10,
            processing: true,
            ordering: false,
            serverSide: true,
            searching: true,
            paging: true,
            ajax: {
                url: GetPagedStockInHandReportUrl,
                type: 'POST',
                data: formData,
                dataSrc: function (json) {
                    const return_data = [];
                    try {
                        const d = $.parseJSON(json.data);
                        for (let i = 0; i < d.length; i++) {
                            const qty = d[i].Qty || 0;
                            const packUnits = d[i].PackUnits || 0;
                            const displayQty = qty < packUnits ? `${qty} L` : qty;

                            return_data.push({
                                'SrNo': i + 1,
                                'SKU': d[i].SKU,
                                'ItemName': d[i].ItemName,
                                ///*'Category': d[i].Category,*/
                                //'Location': d[i].Location,
                                'Qty': displayQty,
                                'QtyUnit': d[i].Unit || '',
                                'ShelfQty': '',
                                'Difference': '',
                                'PurchasePrice': `${addCommasToNumber(d[i].PurchasePrice || 0)}`,
                                'SalePrice': `${addCommasToNumber(d[i].SalePrice || 0)}`,
                                'Profit': `${addCommasToNumber((d[i].SalePrice - d[i].PurchasePrice) || 0)}`,
                                'Margin': d[i].SalePrice > 0
                                    ? (((d[i].SalePrice - d[i].PurchasePrice) / d[i].SalePrice) * 100).toFixed(2) + '%'
                                    : '0.00%',
                                'PackUnits': d[i].PackUnits,
                            });
                        }
                    } catch (ex) {
                        console.error('Error parsing data:', ex);
                    }
                    return return_data;
                }
            },
            columns: [
                { data: 'SrNo' },
                { data: 'SKU' },
                { data: 'ItemName' },
                ///*{ data: 'Category' },*/
                //{ data: 'Location' },
                {
                    data: 'Qty',
                    //render: $.fn.dataTable.render.number(',', '.', 0, '')
                },
                { data: 'ShelfQty' },
                { data: 'Difference' },
                {
                    data: 'PurchasePrice',
                    render: $.fn.dataTable.render.number(',', '.', 2, '')
                },
                {
                    data: 'SalePrice',
                    render: $.fn.dataTable.render.number(',', '.', 2, '')
                },
                { data: 'Profit' },
                { data: 'Margin' },
                { data: 'PackUnits' }
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalQty = 0;

                        // Calculate the sum for each group
                        rows.every(function (rowIdx) {
                            const data = this.data();
                            totalQty += data.Qty;
                        });

                        // Return the custom group header with totals
                        return `${group} (Total Qty: ${addCommasToNumber(totalQty)})`;
                    }
                }
                : false,
            language: {
                emptyTable: 'No records found',
                searchPlaceholder: 'Search By Item Name'
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
                calculateAndDisplayFooter(api, 4, 'Qty');
            }
        });
    }
})(jQuery)