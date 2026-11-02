(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable');
    $(function () {
        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getSupplierOutstandingReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="MinOutstandingAmount"]').val('');
            _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            getSupplierOutstandingReport('', false);
        });

        getSupplierOutstandingReport('', false);
    });

    async function getSupplierOutstandingReport(groupByColumn, isSearch = false) {
        let currencySymbol = $("#currency").val();

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
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
                url: GetSupplierOutstandingReportUrl,
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
                                'SupplierName': d[i].SupplierName,
                                'DateTime': moment(d[i].DateTime).format('DD MMMM YYYY'),
                                'TotalPurchase': addCommasToNumber(d[i].TotalPurchase, 2, currencySymbol),
                                'PaidAmount': addCommasToNumber(d[i].PaidAmount, 2, currencySymbol),
                                'Balance': addCommasToNumber(d[i].Balance, 2, currencySymbol)
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "SupplierName" },
                { data: "DateTime" },
                { data: "TotalPurchase" },
                { data: "PaidAmount" },
                { data: "Balance" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Supplier Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();

                calculateAndDisplayFooter(api, 2, 'TotalPurchase', currencySymbol);
                calculateAndDisplayFooter(api, 3, 'PaidAmount', currencySymbol);
                calculateAndDisplayFooter(api, 4, 'Balance', currencySymbol);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Supplier Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery)