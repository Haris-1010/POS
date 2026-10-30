(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable'),
        _$searchItemElement = $('.js-search-item');

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
            getStockValuationReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('input[name="ItemID"]').val('');
            _$filterForm.find('input[name="VariationID"]').val('');

            _$filterForm.find('select[name="VarientID"]').val('').trigger('change');
            _$filterForm.find('select[name="CategoryID"]').val('').trigger('change');
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');

            _$filterForm.find('select[name="SortBy"]').val('VariationName').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            _$filterForm.find('input[name="Date"]').val('');

            getStockValuationReport('', false);
        });

        getStockValuationReport('', false)

        registerExcelExport('reportTable', getStockValuationReport, 1000);

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

    async function getStockValuationReport(groupByColumn, isSearch = false, pageLimit = 10) {
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
                url: GetStockValuationReportUrl,
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
                                'VariationName': d[i].VariationName,
                                'Variant': d[i].Variant,
                                'Category': d[i].Category,
                                'Manufacturer': d[i].Manufacturer,
                                'OpeningStock': addCommasToNumber(d[i].OpeningStock),
                                'CostPrice': `${currencySymbol} ${addCommasToNumber(d[i].CostPrice)}`,
                                'TotalValue': `${addCommasToNumber(d[i].TotalValue)}`,
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "VariationName", title: "Product Name" },
                { data: "Variant", title: "Variant" },
                { data: "Category", title: "Category" },
                { data: "Manufacturer", title: "Manufacturer" },
                { data: "OpeningStock", title: "Stock Qty" },
                { data: "CostPrice", title: "Unit Price" },
                { data: "TotalValue", title: "Total Value" },
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalOpeningStock = 0;
                        let totalTotalValue = 0;

                        rows.every(function () {
                            const data = this.data();
                            totalOpeningStock += parseNumber(data.OpeningStock);
                            totalTotalValue += parseNumber(data.TotalValue);
                        });

                        // Return the custom group header with totals
                        return `${group} (Total Qty: ${addCommasToNumber(totalOpeningStock)}
                                        | Total Value: ${addCommasToNumber(totalTotalValue, 2, currencySymbol)})`;
                    }
                }
                : false,
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

                calculateAndDisplayFooter(api, 4, 'OpeningStock');
                calculateAndDisplayFooter(api, 6, 'TotalValue');
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Product Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery)