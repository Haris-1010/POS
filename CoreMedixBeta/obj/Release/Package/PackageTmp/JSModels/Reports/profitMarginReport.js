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
    var ProfitDataStart = $('#profitmarginData').data('startdate');
    var ProfitDataEnd = $('#profitmarginData').data('enddate');

    if (ProfitDataStart && ProfitDataEnd) {
        startDate = moment(ProfitDataStart, 'YYYY-MM-DD');
        endDate = moment(ProfitDataEnd, 'YYYY-MM-DD');
        _$filterForm.find('input[name="Date"]').daterangepicker({
            opens: 'right',
            startDate: startDate,
            endDate: endDate,
        })
        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getProfitMarginReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('select[name="VarientID"]').val('').trigger('change');
            _$filterForm.find('select[name="CategoryID"]').val('').trigger('change');
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="BrandID"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');

            _$filterForm.find('input[name="ItemID"]').val('');
            _$filterForm.find('input[name="VariationID"]').val('');

            _$filterForm.find('select[name="SortBy"]').val('ProductName').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            startDate = moment().startOf('month');
            endDate = moment().endOf('month');

            _$filterForm.find('input[name="Date"]').val(
                startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY')
            );
            getProfitMarginReport('', false);
        });

        registerExcelExport('reportTable', getProfitMarginReport, 1000);

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
                    _$searchItemElement.val(itemData.ItemName);
                    _$filterForm.find('input[name="ItemID"]').val(itemData.ItemID);
                    _$filterForm.find('input[name="VariationID"]').val(itemData.VariationID);
                }, 10);
            },
            open: function () {
                $('.ui-autocomplete').css('z-index', 1050);
            }
        }).data("ui-autocomplete")._renderItem = function (ul, item) {
            return $("<li>")
                .append(`<div>${item.label}</div>`)
                .appendTo(ul);
        };
        getProfitMarginReport('', true);
    } else {
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
                getProfitMarginReport('', true);
            });

            _$filterForm.on('click', '.js-clear-filter-btn', function () {
                _$filterForm[0].reset();

                _$filterForm.find('select[name="VarientID"]').val('').trigger('change');
                _$filterForm.find('select[name="CategoryID"]').val('').trigger('change');
                _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
                _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
                _$filterForm.find('select[name="BrandID"]').val('').trigger('change');
                _$filterForm.find('select[name="LocationID"]').val('').trigger('change');

                _$filterForm.find('input[name="ItemID"]').val('');
                _$filterForm.find('input[name="VariationID"]').val('');

                _$filterForm.find('select[name="SortBy"]').val('ProductName').trigger('change');
                _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

                startDate = moment().startOf('month');
                endDate = moment().endOf('month');

                _$filterForm.find('input[name="Date"]').val(
                    startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY')
                );
                getProfitMarginReport('', false);
            });

            getProfitMarginReport('', false)

            registerExcelExport('reportTable', getProfitMarginReport, 1000);

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
                        _$searchItemElement.val(itemData.ItemName);
                        _$filterForm.find('input[name="ItemID"]').val(itemData.ItemID);
                        _$filterForm.find('input[name="VariationID"]').val(itemData.VariationID);
                    }, 10);
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
    }
    async function getProfitMarginReport(groupByColumn, isSearch = false, pageLimit = 10) {
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
                url: GetProfitMarginReportUrl,
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
                                'ProductName': d[i].ProductName,
                                'Variant': d[i].Variant,
                                'Category': d[i].Category,
                                'PurchaseRate': `${currencySymbol} ${addCommasToNumber(d[i].PurchaseRate)}`,
                                'SaleRate': `${currencySymbol} ${addCommasToNumber(d[i].SaleRate)}`,
                                'QtySold': addCommasToNumber(d[i].QtySold),
                                'ProfitPerUnit': `${currencySymbol} ${addCommasToNumber(d[i].ProfitPerUnit, 2)}`,
                                'TotalProfit': `${addCommasToNumber(d[i].TotalProfit, 2)}`
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "ProductName", title: "Product" },
                { data: "Variant", title: "Variant" },
                { data: "Category", title: "Category" },
                { data: "PurchaseRate", title: "Purchase Rate" },
                { data: "SaleRate", title: "Sale Rate" },
                { data: "QtySold", title: "Qty Sold" },
                { data: "ProfitPerUnit", title: "Profit Per Unit" },
                { data: "TotalProfit", title: "Total Profit" }
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalQty = 0;
                        let totalProfit = 0;

                        rows.every(function () {
                            const data = this.data();
                            totalQty += parseNumber(data.QtySold);
                            totalProfit += parseNumber(data.TotalProfit.replace(currencySymbol, '').trim());
                        });

                        // Return the custom group header with totals
                        return `${group} (Total Qty: ${addCommasToNumber(totalQty)} | Total Profit: ${currencySymbol} ${addCommasToNumber(totalProfit, 2)})`;
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

                calculateAndDisplayFooter(api, 5, 'QtySold');
                calculateAndDisplayFooter(api, 7, 'TotalProfit');
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Product Name')
            .addClass('seachbar-tooltip');
    }
})(jQuery)