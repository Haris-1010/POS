(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable'),
        _$customerSearchElement = $('.js-search-customer');

    let startDate = moment().startOf('month');
    let endDate = moment().endOf('month');

    window.reportDateRange = {
        getStart: () => startDate,
        getEnd: () => endDate,
        set: (s, e) => { startDate = s; endDate = e; }
    };
    $(function () {
        _$customerSearchElement.autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: SearchCustomerUrl,
                    data: { SearchString: request.term },
                    type: "POST",
                    success: function (data) {
                        let results = $.map(data.Data, function (item) {
                            return {
                                label: `${item.CustomerName} | ${item.Phone} (${item.CustomerType})`,
                                value: item.CustomerName,
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
                } else {
                    let itemData = ui.item.data;
                    _$filterForm.find('input[name="CustomerID"]').val(itemData.CustomerID);
                    _$filterForm.find('input[name="CustomerType"]').val(itemData.CustomerType);
                    _$customerSearchElement.val(itemData.CustomerName);
                }
            },
            open: function () {
                $('.ui-autocomplete').css('z-index', 1000);
            }
        }).data("ui-autocomplete")._renderItem = function (ul, item) {
            let content;
            if (item.data === null) {
                content = `<div style="background-color:#13075A; padding:5px; font-weight:bold; color:white"> + ${item.label} </div>`;
            } else {
                content = `<div>${item.label}</div>`;
            }
            return $("<li>")
                .append(content)
                .appendTo(ul);
        };

        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getCustomerOutstandingReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('input[name="CustomerID"]').val('');
            _$filterForm.find('input[name="CustomerType"]').val('');
            _$filterForm.find('select[name="MinOutstandingAmount"]').val('');

            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');
            _$filterForm.find('select[name="SortBy"]').val('Customer').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            getCustomerOutstandingReport('', false);
        });

        getCustomerOutstandingReport('', false)

        registerExcelExport('reportTable', getCustomerOutstandingReport, 1000);
    });

    async function getCustomerOutstandingReport(groupByColumn, isSearch = false, pageLimit = 10) {
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
                url: GetCustomerOutstandingReportUrl,
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
                                'Customer': d[i].Customer,
                                'DateTime': moment(d[i].DateTime).format('DD MMMM YYYY'),
                                'TotalSale': addCommasToNumber(d[i].TotalSale, 2, currencySymbol),
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
                { data: "Customer" },
                { data: "DateTime" },
                { data: "TotalSale" },
                { data: "PaidAmount" },
                { data: "Balance" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Customer Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();

                calculateAndDisplayFooter(api, 2, 'TotalSale', currencySymbol);
                calculateAndDisplayFooter(api, 3, 'PaidAmount', currencySymbol);
                calculateAndDisplayFooter(api, 4, 'Balance', currencySymbol);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Customer Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery)