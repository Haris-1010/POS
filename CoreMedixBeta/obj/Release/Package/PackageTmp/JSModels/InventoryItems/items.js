(function ($) {
    const _$itemsTable = $('#itemsTable'),
        _$filterForm = $('#filterForm'),
        _$itemsInfoContainer = $("#itemsInfoContainer");

    const selectedItems = new Set();
    let importRows = [];
    let deleteIdentifiers = [];

    var filteredData = $('#filterData').data('filtered');
    if (filteredData) {  

        getInventoryItemList(null, false, filteredData);
        getInventoryItemsInfo(); 
        $(document).on('click', '.filter-card', function () {
            const filterType = $(this).data('filter');
            getInventoryItemList(null, false, filterType);
        });

        startDate = moment();
        endDate = moment();
        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getInventoryItemList('', true);
        });
        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('.js-select2').val(null).trigger('change');
            _$filterForm.find('input[type="radio"]').prop('checked', false);
            _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

            getInventoryItemList('', false);
        });
    } else {
        $(function () {
            _$filterForm.find('input[name="Date"]').daterangepicker({
                opens: 'right',
                ranges: {
                    'Today': [moment(), moment()],
                    'This Month': [moment().startOf('month'), moment().endOf('month')],
                    'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')],
                    'This Year': [moment().startOf('year'), moment().endOf('year')],
                    'Last Year': [moment().subtract(1, 'year').startOf('year'), moment().subtract(1, 'year').endOf('year')]
                }
            }, function (start, end, label) {
                startDate = start;
                endDate = end;
                //getInventoryItemList();
            });

            _$filterForm.on('click', '.js-apply-filter-btn', function () {
                getInventoryItemList('', true);
            });

            $(document).on('click', '.filter-card', function () {
                const filterType = $(this).data('filter');
                getInventoryItemList(null, false, filterType);
            });

            _$filterForm.on('click', '.js-clear-filter-btn', function () {
                _$filterForm[0].reset();
                _$filterForm.find('.js-select2').val(null).trigger('change');
                _$filterForm.find('input[type="radio"]').prop('checked', false);
                _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
                _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');

                getInventoryItemList('', false);
            });

            //_$filterForm.find('select').on('change', function () {
            //    getInventoryItemList();
            //});
            startDate = moment();
            endDate = moment();
            getInventoryItemList('', false);
            getInventoryItemsInfo();
        });
    }

    async function getInventoryItemsInfo() {
        const url = getInventoryItemsUrl;
        const data = {
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: responseData } = resp;
            if (status) {
                _$itemsInfoContainer.find('.js-total-items').text(responseData.TotalItems);
                _$itemsInfoContainer.find('.js-new-arrival').text(responseData.NewArrivalItems);
                _$itemsInfoContainer.find('.js-low-stock').text(responseData.LowStockItems);
                _$itemsInfoContainer.find('.js-expiring-soon').text(responseData.TotalExpirySoonItems);
            }
        } catch (error) {
            toastr.error("An error occurred while deleting the customer.", "", { timeOut: 500 });
        }
    }

    function calculateAndDisplayFooter(api, columnIndex, propertyName, prefix = '', showAvg = false) {
        var data = api.data().toArray();
        var values = data.map(row => parseNumber(row[propertyName].toString()));
        if (values.length === 0) {
            var footerContent = `N/A`;

            $(api.column(columnIndex).footer()).html(footerContent);
            return;
        }
        var sum = values.reduce((acc, val) => acc + val, 0);
        var min = Math.min(...values);
        var max = Math.max(...values);
        var avg = showAvg ? (sum / values.length) : null;

        // Create footer content
        var footerContent = `
        <div style="white-space: nowrap; font-size:.8rem !important;">Sum: ${prefix}${formatNumber(sum)}</div>
        <div style="white-space: nowrap; font-size:.8rem !important;">Min: ${prefix}${formatNumber(min)}</div>
        <div style="white-space: nowrap; font-size:.8rem !important;">Max: ${prefix}${formatNumber(max)}</div>
        ${showAvg ? `<div style="white-space: nowrap; font-size:.8rem !important;">Avg: ${prefix}${formatNumber(avg)}</div>` : ''}`;

        // Set footer HTML
        $(api.column(columnIndex).footer()).html(footerContent);
    }

    async function getInventoryItemList(groupByColumn, isSearch = false, filterType = null) {
        const buttonsArray = [
            {
                extend: "pageLength",
                className: "btn btn-default",
                titleAttr: 'Show rows',
                text: '<i class="fa fa-ellipsis-v"></i> Show rows',
                init: function (api, node, config) {
                    $(node).removeClass('dt-button');
                },
            }
        ];
        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            formData.StartDate = startDate.format('YYYY-MM-DD');
            formData.EndDate = endDate.format('YYYY-MM-DD');
        } else {
            formData = {
            };
        }

        if (filterType) {
            formData.FilterType = filterType;
        }

        _$itemsTable.DataTable().destroy();
        function addCommasToNumber(number) {
            // Handle null, undefined, or non-numeric values
            if (number == null || isNaN(number)) {
                return "0";
            }
            // Convert to string and add commas as thousand separators
            return number.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
        }

        function calculateAndDisplayFooter(api, column, field) {
            var total = api
                .column(column)
                .data()
                .reduce(function (sum, value) {
                    // Handle undefined, null, or non-string values
                    if (value == null) {
                        return sum;
                    }
                    // Remove commas for VariationStockQty and parse as number
                    var num = field === 'VariationStockQty' ? parseFloat(value.toString().replace(/,/g, '')) : parseFloat(value);
                    return sum + (isNaN(num) ? 0 : num);
                }, 0);
            $(api.column(column).footer()).html(addCommasToNumber(total));
        }

        const table = $('#itemsTable').DataTable({
            dom: 'Blfrtip',
            buttons: [
                'copy',
                {
                    extend: 'csv',
                    text: 'Excel'
                },
                'pdf',
                'print',
                {
                    extend: 'colvis',
                    text: 'Columns'
                },
            ],
            lengthMenu: [10, 25, 50, 100, 1000],
            pageLength: 10,
            processing: true,
            ordering: false,
            serverSide: true,
            lengthChange: true,
            searching: true,
            paging: true,
            ajax: {
                url: getpagedInventoryItemsUrl,
                type: "POST",
                data: formData,
                dataSrc: function (json) {
                    console.log("Raw AJAX response:", json);
                    var return_data = [];
                    try {
                        // Parse json.data if it's a string
                        var d = typeof json.data === 'string' ? JSON.parse(json.data) : json.data;
                        if (!Array.isArray(d)) {
                            console.error("json.data is not an array after parsing:", d);
                            return return_data;
                        }

                        var lowStockItems = 0;
                        var expiringSoonItems = 0;
                        var newArrivals = 0;
                        var totalItems = 0;

                        for (var i = 0; i < d.length; i++) {
                            lowStockItems += d[i].NearToFinishVariations || 0;
                            expiringSoonItems += d[i].NearToExpireVariations || 0;
                            newArrivals += d[i].NewArrivals || 0;
                            totalItems += d[i].TotalVariations || 0;

                            return_data.push({
                                Checkbox: `<input type="checkbox" class="js-item-check" data-item-id="${d[i].ID}" ${selectedItems.has(Number(d[i].ID)) ? 'checked' : ''}>`,
                                UniqueID: d[i].UniqueID,
                                ProductName: d[i].ProductName,
                                ProductType: d[i].ProductType,
                                ManufacturerName: d[i].ManufacturerName,
                                SupplierName: d[i].SupplierName,
                                TotalVariations: d[i].TotalVariations,
                                NearToExpireVariations: d[i].NearToExpireVariations,
                                NearToFinishVariations: d[i].NearToFinishVariations,
                                VariationStockQty: addCommasToNumber(d[i].VariationStockQty || 0),
                                'Actions': `
                                        <div class="btn-group">
                                            <a class="btn btn-sm btn-default" 
                                               title="Edit Invoice" 
                                               data-toggle="tooltip"
                                               href="${urlManufacturer}?ItemID=${d[i].ID}&ManufacturerID=${d[i].ManufacturerID}">
                                               <i class="fas fa-edit"></i>
                                            </a>

                                            <button class="btn btn-sm btn-danger js-delete-btn" 
                                                    title="Delete Item" 
                                                    data-toggle="tooltip"
                                                    data-item-id="${d[i].ID}">
                                               <i class="fas fa-trash"></i>
                                            </button>
                                        </div>`


                            });
                        }

                        // Update UI elements (uncomment if needed)
                        // $('.js-low-stock').text(lowStockItems);
                        // $('.js-expiring-soon').text(expiringSoonItems);
                        // $('.js-new-arrival').text(newArrivals);
                        // $('.js-total-items').text(totalItems);
                    } catch (ex) {
                        console.error("Error in dataSrc:", ex);
                    }
                    return return_data;
                }
            },
            columns: [
                { data: "Checkbox", orderable: false, searchable: false, className: "text-center" },
                { data: "UniqueID" },
                { data: "ProductName" },
                { data: "ProductType" },
                { data: "ManufacturerName" },
                { data: "SupplierName" },
                { data: "TotalVariations" },
                { data: "NearToExpireVariations" },
                { data: "NearToFinishVariations" },
                { data: "VariationStockQty" },
                { data: "Actions", orderable: false, searchable: false }
            ],
            language: {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Product Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
                calculateAndDisplayFooter(api, 6, 'TotalVariations');
                calculateAndDisplayFooter(api, 7, 'NearToExpireVariations');
                calculateAndDisplayFooter(api, 8, 'NearToFinishVariations');
                calculateAndDisplayFooter(api, 9, 'VariationStockQty');
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Product Name')
            .addClass('seachbar-tooltip');
    }

    async function getItemActivityLogs(variationID) {
        const url = getItemActivityLogsUrl;

        const data = {
            VariationID: variationID
        };

        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: logs } = resp;

            const $tbody = $("#activityLogsTable tbody");
            $tbody.empty(); // clear table body

            if (status) {
                toastr.success(message, "", { timeOut: 500 });

                if (logs && logs.length > 0) {
                    logs.forEach(log => {
                        const formattedVariationName = formatVariationName(log.VariationName);
                        const formattedTableName = formatTableName(log.Table);

                        const rowHtml = `
                        <tr>
                            <td>${moment(log.TimeStamp).format("DD MMM YYYY hh:mm A")}</td>
                            <td>${formattedVariationName}</td>
                            <td>${log.Qty}</td>
                            <td>${log.UnitCost}</td>
                            <td>${log.Discount}</td>
                            <td>${log.UserName}</td>
                            <td>${formattedTableName}</td>
                            <td>${log.EventType}</td>
                            <td>${log.Description}</td>
                            <td>${log.IPAddress}</td>
                        </tr>
                    `;
                        $tbody.append(rowHtml);
                    });
                } else {
                    $tbody.append(`<tr><td colspan="10" class="text-center text-danger">No records found</td></tr>`);
                }

                $("#activityLogsModal").modal("show");
            } else {
                // If message is "Data Not Found", still show the modal with "No records"
                if (message === "Data Not Found") {
                    toastr.warning(message, "", { timeOut: 500 });
                    $tbody.append(`<tr><td colspan="10" class="text-center text-danger">No records found</td></tr>`);
                    $("#activityLogsModal").modal("show");
                } else {
                    toastr.error(message || "Unexpected error", "", { timeOut: 500 });
                }
            }
        } catch (error) {
            console.error("Error fetching item activity logs:", error);
            toastr.error("An error occurred while fetching activity logs.", "", { timeOut: 500 });
        }
    }

    _$itemsTable.on('click', '.js-delete-btn', function () {
        const itemID = $(this).data('item-id');
        bootbox.confirm({
            title: 'Delete Confirmation',
            message: "Do you want to delete this Item?",
            buttons: {
                confirm: {
                    label: 'Confirm',
                    className: 'btn-success'
                },
                cancel: {
                    label: 'Cancel',
                    className: 'btn-danger'
                }
            },
            callback: function (result) {
                if (result) {
                    const url = deleteInventoryItemUrl;
                    const data = { ID: itemID }

                    $.post(url, data, function (resp) {
                        const { Status: status, Message: message } = resp;

                        if (status) {
                            getInventoryItemList();
                            toastr.success(message, "", { timeOut: 500 });
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    }).fail(function () {
                        toastr.error("Unable to process your request due to a technical error.");
                    });
                }
            }
        });
    });

    _$itemsTable.on('click', '.js-view-logs-btn', function () {
        const VariationID = $(this).data('variation-id');
        if (!VariationID) {
            toastr.error("Item ID not found", "", { timeOut: 500 });
            return;
        }

        getItemActivityLogs(VariationID);
    });

    function formatVariationName(name) {
        if (!name) return '';
        const index = name.indexOf('-');
        return index !== -1 ? name.substring(index + 1).trim() : name.trim();
    }

    function formatTableName(tableName) {
        if (!tableName) return '';

        // Step 1: Remove prefix before dot (e.g., "Pharmacy.SaleInvoices" → "SaleInvoices")
        let name = tableName.split('.').pop();

        // Step 2: Add space before uppercase letters (e.g., "SaleInvoices" → "Sale Invoices")
        name = name.replace(/([a-z])([A-Z])/g, '$1 $2');

        return name;
    }
    function escapeHtml(value) {
        return String(value === null || value === undefined ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function cellText(row, aliases) {
        const raw = BulkFile.findValue(row, aliases);
        if (raw === null || raw === undefined) return '';
        if (typeof raw === 'number') return String(raw);
        return String(raw).trim();
    }

    function refreshSelectionUI() {
        const count = selectedItems.size;
        $('.js-selected-count').text(count);
        if (count > 0) $('.js-delete-selected-btn').show();
        else $('.js-delete-selected-btn').hide();
    }

    _$itemsTable.on('change', '.js-item-check', function () {
        const itemID = Number($(this).data('item-id'));
        if (this.checked) selectedItems.add(itemID);
        else selectedItems.delete(itemID);

        _$itemsTable.find(`.js-item-check[data-item-id="${itemID}"]`).prop('checked', this.checked);
        refreshSelectionUI();
    });

    _$itemsTable.on('change', '.js-select-all-items', function () {
        const isChecked = this.checked;
        _$itemsTable.find('.js-item-check').each(function () {
            const itemID = Number($(this).data('item-id'));
            $(this).prop('checked', isChecked);
            if (isChecked) selectedItems.add(itemID);
            else selectedItems.delete(itemID);
        });
        refreshSelectionUI();
    });

    $('.js-delete-selected-btn').on('click', function () {
        if (selectedItems.size === 0) {
            toastr.info('No items selected.', "", { timeOut: 1500 });
            return;
        }

        const ids = Array.from(selectedItems);
        bootbox.confirm({
            title: 'Delete Confirmation',
            message: `Do you want to delete ${ids.length} selected item(s) along with all their variations?`,
            buttons: {
                confirm: { label: 'Confirm', className: 'btn-success' },
                cancel: { label: 'Cancel', className: 'btn-danger' }
            },
            callback: function (result) {
                if (!result) return;

                $.ajax({
                    url: bulkDeleteInventoryItemsUrl,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ IDs: ids, Identifiers: [] }),
                    success: function (resp) {
                        const { Status: status, Message: message } = resp;
                        selectedItems.clear();
                        refreshSelectionUI();

                        if (status) {
                            toastr.success(message, "", { timeOut: 2000 });
                            getInventoryItemList();
                            getInventoryItemsInfo();
                        } else {
                            toastr.error(message, "", { timeOut: 3000 });
                        }
                    },
                    error: function () {
                        toastr.error("Unable to process your request due to a technical error.");
                    }
                });
            }
        });
    });

    const importTemplateHeaders = [
        'Item Name', 'Product Type', 'Medicine Salt', 'Manufacturer', 'Supplier',
        'Variation Name', 'SKU', 'Expiry Date', 'Opening Stock', 'Cost Price',
        'Selling Price', 'Opening Stock Rate', 'Reorder Point', 'Tax %', 'Batch', 'No. of Pills in Pack'
    ];

    const deleteTemplateHeaders = ['Item Name', 'Unique ID'];

    function mapImportRow(row, index) {
        return {
            RowNumber: String(index + 2),
            ItemName: cellText(row, ['Item Name']),
            ProductType: cellText(row, ['Product Type']),
            MedicineSalt: cellText(row, ['Medicine Salt', 'Salt', 'Generic']),
            ManufacturerName: cellText(row, ['Manufacturer', 'Manufacturer Name']),
            SupplierName: cellText(row, ['Supplier', 'Supplier Name']),
            VariationName: cellText(row, ['Variation Name', 'Variation']),
            SKU: cellText(row, ['SKU']),
            ExpiryDate: BulkFile.formatDate(BulkFile.findValue(row, ['Expiry Date', 'Expiry'])),
            OpeningStock: cellText(row, ['Opening Stock', 'Opening Stock (Qty)', 'Stock']),
            CostPrice: cellText(row, ['Cost Price', 'Cost']),
            SellingPrice: cellText(row, ['Selling Price', 'Sale Price']),
            OpeningStockRate: cellText(row, ['Opening Stock Rate']),
            ReorderPoint: cellText(row, ['Reorder Point']),
            Tax: cellText(row, ['Tax %', 'Tax']),
            Batch: cellText(row, ['Batch', 'Batch No', 'Batch No#']),
            NoOfPillsInPack: cellText(row, ['No. of Pills in Pack', 'Pills in Pack'])
        };
    }

    function renderImportPreview(rows) {
        const headers = importTemplateHeaders.slice(0, 8);
        const $table = $('#importPreviewTable');

        $table.find('thead').html('<tr>' + headers.map(header => `<th>${escapeHtml(header)}</th>`).join('') + '</tr>');

        const body = rows.slice(0, 8).map(row =>
            '<tr>' + headers.map(header => `<td>${escapeHtml(cellText(row, [header]))}</td>`).join('') + '</tr>'
        ).join('');

        $table.find('tbody').html(body || '<tr><td colspan="' + headers.length + '" class="text-center">No rows found</td></tr>');
        $('.js-preview-row-count').text(rows.length);
        $('.js-import-preview').show();
    }

    $('.js-import-items-btn').on('click', function () {
        $('#importItemsModal').modal('show');
    });

    $('.js-download-import-template-btn').on('click', function () {
        BulkFile.downloadTemplate({
            fileName: 'Inventory-Items-Template.xlsx',
            sheetName: 'Items',
            headers: importTemplateHeaders,
            rows: [[
                'Panadol', 'Medicine', 'Paracetamol', 'HealthCorp Pharma', 'Acme Medical Supplies',
                'Panadol 500mg Strip', '1001', '31-Dec-2027', '100', '10.50', '15.00', '10.50',
                '10', '0', 'B-001', '10'
            ]]
        });
    });

    $('.js-import-file-input').on('change', function () {
        const file = this.files && this.files[0];
        $(this).next('.custom-file-label').text(file ? file.name : 'Choose file...');

        importRows = [];
        $('.js-import-preview').hide();
        $('.js-import-result').hide();
        $('.js-run-import-btn').prop('disabled', true);

        BulkFile.parse(file, function (rows) {
            importRows = rows;
            renderImportPreview(rows);
            $('.js-run-import-btn').prop('disabled', rows.length === 0);
        }, function (error) {
            toastr.error(error, "", { timeOut: 3000 });
        });
    });

    $('.js-run-import-btn').on('click', async function () {
        const _$btn = $(this);
        if (!importRows.length) return;

        _$btn.prop('disabled', true).text('Importing...');
        try {
            const payload = { Rows: importRows.map((row, index) => mapImportRow(row, index)) };
            const resp = await $.ajax({
                url: importInventoryItemsUrl,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload)
            });

            const { Status: status, Message: message, Data: responseData } = resp;
            const $result = $('.js-import-result');
            let html = `<strong>${escapeHtml(message)}</strong>`;

            if (responseData && responseData.Errors && responseData.Errors.length) {
                html += '<ul class="mb-0 mt-2" style="max-height:160px; overflow:auto;">' +
                    responseData.Errors.map(error => `<li>${escapeHtml(error)}</li>`).join('') + '</ul>';
            }

            $result.removeClass('alert-success alert-danger alert-info')
                .addClass(status ? 'alert-success' : 'alert-danger')
                .html(html).show();

            if (status) {
                toastr.success(message, "", { timeOut: 2500 });
                importRows = [];
                $('.js-import-file-input').val('').next('.custom-file-label').text('Choose file...');
                $('.js-import-preview').hide();
                $('.js-run-import-btn').prop('disabled', true);
                getInventoryItemList();
                getInventoryItemsInfo();
            } else {
                toastr.error(message, "", { timeOut: 3000 });
            }
        } catch (error) {
            toastr.error("Unable to process your request due to a technical error.");
        } finally {
            _$btn.prop('disabled', false).html('<i class="fas fa-file-import"></i> Import Now');
        }
    });

    $('.js-bulk-delete-btn').on('click', function () {
        $('#bulkDeleteItemsModal').modal('show');
    });

    $('.js-download-delete-template-btn').on('click', function () {
        BulkFile.downloadTemplate({
            fileName: 'Inventory-Items-Delete-Template.xlsx',
            sheetName: 'Delete',
            headers: deleteTemplateHeaders,
            rows: [
                ['Panadol', ''],
                ['', 'a1b2c3']
            ]
        });
    });

    $('.js-delete-file-input').on('change', function () {
        const file = this.files && this.files[0];
        $(this).next('.custom-file-label').text(file ? file.name : 'Choose file...');

        deleteIdentifiers = [];
        $('.js-delete-preview').hide();
        $('.js-delete-result').hide();
        $('.js-run-bulk-delete-btn').prop('disabled', true);

        BulkFile.parse(file, function (rows) {
            rows.forEach(row => {
                [cellText(row, ['Item Name']), cellText(row, ['Unique ID'])].forEach(value => {
                    if (value.length > 0 && deleteIdentifiers.indexOf(value) === -1) deleteIdentifiers.push(value);
                });
            });

            $('.js-delete-row-count').text(deleteIdentifiers.length);
            $('.js-delete-identifier-list').html(
                deleteIdentifiers.map(value => `<li><i class="fas fa-circle text-danger mr-1" style="font-size:6px; vertical-align:middle;"></i>${escapeHtml(value)}</li>`).join('')
            );
            $('.js-delete-preview').show();
            $('.js-run-bulk-delete-btn').prop('disabled', deleteIdentifiers.length === 0);
        }, function (error) {
            toastr.error(error, "", { timeOut: 3000 });
        });
    });

    $('.js-run-bulk-delete-btn').on('click', function () {
        if (!deleteIdentifiers.length) return;

        bootbox.confirm({
            title: 'Delete Confirmation',
            message: `${deleteIdentifiers.length} row(s) will be matched and their item(s) will be deleted along with all variations. Do you want to continue?`,
            buttons: {
                confirm: { label: 'Confirm', className: 'btn-success' },
                cancel: { label: 'Cancel', className: 'btn-danger' }
            },
            callback: async function (result) {
                if (!result) return;

                try {
                    const resp = await $.ajax({
                        url: bulkDeleteInventoryItemsUrl,
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({ IDs: [], Identifiers: deleteIdentifiers })
                    });

                    const { Status: status, Message: message } = resp;
                    const $result = $('.js-delete-result');

                    $result.removeClass('alert-success alert-danger alert-info')
                        .addClass(status ? 'alert-success' : 'alert-danger')
                        .html(`<strong>${escapeHtml(message)}</strong>`).show();

                    if (status) {
                        toastr.success(message, "", { timeOut: 2500 });
                        deleteIdentifiers = [];
                        $('.js-delete-file-input').val('').next('.custom-file-label').text('Choose file...');
                        $('.js-delete-preview').hide();
                        $('.js-run-bulk-delete-btn').prop('disabled', true);
                        selectedItems.clear();
                        refreshSelectionUI();
                        getInventoryItemList();
                        getInventoryItemsInfo();
                    } else {
                        toastr.error(message, "", { timeOut: 3000 });
                    }
                } catch (error) {
                    toastr.error("Unable to process your request due to a technical error.");
                }
            }
        });
    });

    $.fn.serializeObject = function () {
        var o = {};
        var a = this.serializeArray();
        $.each(a, function () {
            if (o[this.name]) {
                if (!Array.isArray(o[this.name])) {
                    o[this.name] = [o[this.name]];
                }
                o[this.name].push(this.value || '');
            } else {
                o[this.name] = this.value || '';
            }
        });
        return o;
    };
})(jQuery)