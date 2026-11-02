(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable');

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
            getSupplierLedgerReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('select[name="Status"]').val('').trigger('change');
            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');

            _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            _$filterForm.find('input[name="Date"]').val('');

            getSupplierLedgerReport('', false);
        });

        getSupplierLedgerReport('', false)

        registerExcelExport('reportTable', getSupplierLedgerReport, 1000);
    });

    async function getSupplierLedgerReport(groupByColumn, isSearch = false, pageLimit = 10) {
        let currencySymbol = $("#currency").val();

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
                url: GetSupplierLedgerReportUrl,
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
                                'ReferenceNo': d[i].ReferenceNo,
                                'DateTime': moment(d[i].DateTime).format('DD MMMM YYYY'),
                                'Status': d[i].Status,
                                'Debit': addCommasToNumber(d[i].Debit, 2, currencySymbol),
                                'Credit': addCommasToNumber(d[i].Credit, 2, currencySymbol),
                                'RunningBalance': addCommasToNumber(d[i].RunningBalance, 2, currencySymbol),
                                'SupplierName': d[i].SupplierName,
                                'Description': d[i].Description
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "ReferenceNo", title: "Reference No" },
                { data: "DateTime", title: "Date Time" },
                { data: "Status", title: "Status" },
                { data: "Debit", title: "Debit" },
                { data: "Credit", title: "Credit" },
                { data: "RunningBalance", title: "Running Balance" },
                { data: "SupplierName", title: "Supplier" },
                { data: "Description", title: "Description" }
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalDebit = 0;
                        let totalCredit = 0;
                        let totalBalance = 0;

                        // Calculate the sum for each group
                        rows.every(function (rowIdx) {
                            const data = this.data();
                            totalDebit += parseNumber(data.Debit);
                            totalCredit += parseNumber(data.Credit);
                            totalBalance += parseNumber(data.RunningBalance);
                        });

                        // Return the custom group header with totals
                        return `${group} (Total Debit: ${addCommasToNumber(totalDebit, 2, currencySymbol)}
                            | Total Credit: ${addCommasToNumber(totalCredit, 2, currencySymbol)}
                            | Total Balance: ${addCommasToNumber(totalBalance, 2, currencySymbol)})`;
                    }
                }
                : false,
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Reference No Or Supplier Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();

                calculateAndDisplayFooter(api, 3, 'Debit', currencySymbol);
                calculateAndDisplayFooter(api, 4, 'Credit', currencySymbol);
                calculateAndDisplayFooter(api, 5, 'RunningBalance', currencySymbol);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Reference No Or Supplier Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery)