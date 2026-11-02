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
        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getBatchWiseStockReport('Batch', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('select[name="VarientID"]').val('').trigger('change');
            _$filterForm.find('select[name="CategoryID"]').val('').trigger('change');
            _$filterForm.find('select[name="ManufacturerID"]').val('').trigger('change');
            _$filterForm.find('select[name="SupplierID"]').val('').trigger('change');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');

            _$filterForm.find('input[name="ItemID"]').val('');
            _$filterForm.find('input[name="VariationID"]').val('');

            _$filterForm.find('select[name="SortBy"]').val('Batch').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            getBatchWiseStockReport('Batch', false);
        });

        getBatchWiseStockReport('Batch', false)

        registerExcelExport('reportTable', getBatchWiseStockReport, 1000);

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

    async function getBatchWiseStockReport(groupByColumn = 'Batch', isSearch = false, pageLimit = 10) {
        let currencySymbol = $("#currency").val();
        if ($.fn.DataTable.isDataTable(_$table)) {
            _$table.DataTable().destroy();
        }

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject(),
                //formData.StartDate = startDate.format('YYYY-MM-DD');
                //formData.EndDate = endDate.format('YYYY-MM-DD');
                formData.GroupByColumn = groupByColumn;
        } else {
            formData = {
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
                url: GetBatchWiseStockReportUrl,
                type: 'POST',
                data: formData,
                dataSrc: function (json) {
                    const return_data = [];
                    try {
                        const d = $.parseJSON(json.data);
                        for (let i = 0; i < d.length; i++) {
                            return_data.push({
                                'ProductName': d[i].ProductName,
                                'Category': d[i].Category,
                                'Variant': d[i].Variant,
                                'Batch': d[i].Batch,
                                'ExpiryDate': moment(d[i].ExpiryDate).format('DD MMMM YYYY'),
                                'OpeningStock': addCommasToNumber(d[i].OpeningStock || 0),
                                'CostPrice': `${currencySymbol} ${addCommasToNumber(d[i].CostPrice || 0)}`,
                                'SellingPrice': `${currencySymbol} ${addCommasToNumber(d[i].SellingPrice || 0)}`
                            });
                        }
                    } catch (ex) {
                        console.error('Error parsing data:', ex);
                    }
                    return return_data;
                }
            },
            columns: [
                { data: 'ProductName' },
                { data: 'Category' },
                { data: 'Variant' },
                { data: 'Batch' },
                { data: 'ExpiryDate' },
                { data: 'OpeningStock' },
                { data: 'CostPrice' },
                { data: 'SellingPrice' },
            ],
            rowGroup: groupByColumn
                ? {
                    dataSrc: groupByColumn,
                    startRender: function (rows, group) {
                        let totalQty = 0;

                        // Calculate the sum for each group
                        rows.every(function (rowIdx) {
                            const data = this.data();
                            totalQty += parseNumber(data.OpeningStock);
                        });

                        // Return the custom group header with totals
                        return `Batch No: ${group} (Total Qty Stock: ${addCommasToNumber(totalQty)})`;
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
                calculateAndDisplayFooter(api, 5, 'OpeningStock');
            }
        });
    }
})(jQuery)