(function ($) {
    const _$table = $('#reportTable'),
        _$referredBySearchElement = $('.js-search-referred-by');
    _$filterForm = $('#filterForm');

    const permissionsForm = $('#permissionsForm').serializeObject();

    let permissions = {
        isEMRIntegration: "1",
    };

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
            getItemWiseSaleReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('input[name="ReferredByID"]').val(''); 
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');
            _$filterForm.find('select[name="BrandID"]').val('').trigger('change');
            _$filterForm.find('select[name="SortBy"]').val('ItemName').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');
            _$filterForm.find('input[type="radio"]').prop('checked', false);

            startDate = moment().startOf('month');
            endDate = moment().endOf('month');

            _$filterForm.find('input[name="Date"]').val(
                startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY')
            );
            getItemWiseSaleReport('', false);
        });

        getItemWiseSaleReport('', false);

        registerExcelExport('reportTable', getItemWiseSaleReport, 1000);
    });
     
    async function getItemWiseSaleReport(groupByColumn, isSearch = false, pageLimit = 10) {
        let currencySymbol = $("#currency").val();

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            formData.StartDate = startDate.format('YYYY-MM-DD');
            formData.EndDate = endDate.format('YYYY-MM-DD');
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
                url: GetPagedItemWiseSaleReportUrl,
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
                                'ItemName': d[i].ItemName,
                                'TotalCustomers': d[i].TotalCustomers,
                                'Qty': addCommasToNumber(d[i].Qty), // addCommasToNumber is placed in shared helper functions js file
                                'RatePerUnit': `${currencySymbol} ${addCommasToNumber(d[i].PerUnitPrice)}`,
                                'LineTotal': `${addCommasToNumber(d[i].LineTotal)}`,
                                //'Actions': `<div class="btn-group"></div>`
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "ItemName" },
                { data: "TotalCustomers" },
                { data: "Qty" },
                { data: "RatePerUnit" },
                { data: "LineTotal" }
                //{ data: "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Item Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();

                calculateAndDisplayFooter(api, 1, 'TotalCustomers');
                calculateAndDisplayFooter(api, 2, 'Qty');
                calculateAndDisplayFooter(api, 4, 'LineTotal', currencySymbol + ' ', true);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Item Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery)