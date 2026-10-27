(function ($) {
    const _$purchaseOrderForm = $('#purchaseOrderForm'),
        _$purchaseItemsTable = $('#purchaseItemsTable');

    const _$itemSearchForm = $('#itemSearchForm'),
        _$searchItemElement = _$itemSearchForm.find('.js-search-item');

    const _$savePurchaseBtn = $('#savePurchaseBtn'),
        _$saveAndPrintPurchaseBtn = $('#saveAndPrintPurchaseBtn');

    $(function () {
        _$purchaseOrderForm.validate({
            errorPlacement: function (error, element) {
                if (element.hasClass('select2-hidden-accessible')) {
                    element.parent().append(error);
                } else {
                    error.insertAfter(element);
                }
            }
        });

        setUpFieldsInCaseOfUpdate();
    })

    function setUpFieldsInCaseOfUpdate() {
        const purchaseOrderID = parseInt(_$purchaseOrderForm.find('input[name="ID"]').val()) || 0;
        if (purchaseOrderID <= 0) {
            return;
        }

        let purchaseDate = _$purchaseOrderForm.find('input[name="PurchaseDateInCaseOfUpdate"]').val();
        if (purchaseDate) {
            _$purchaseOrderForm.find('input[name="OrderDate"]').datepicker('setDate', formatDateForUpdate(purchaseDate));
        }
    }

    _$searchItemElement.autocomplete({
        source: function (request, response) {
            // Get the supplier ID from the purchase order form
            const supplierID = _$purchaseOrderForm.find('select[name="SupplierID"]').val() || 0;

            // Prepare the data object to match the SearchItemsInputDto
            const requestData = {
                SearchString: request.term,
                SearchType: _$itemSearchForm.find('select[name="SearchType"]').val(),
                ManufacturerID: parseInt(_$itemSearchForm.find('select[name="ManufacturerID"]').val()) || 0,
                SupplierID: parseInt(supplierID),
                NearToFinish: _$itemSearchForm.find('input[name="NearToFinish"]').is(':checked'),
                Finished: _$itemSearchForm.find('input[name="Finished"]').is(':checked'),
                NearToExpire: _$itemSearchForm.find('input[name="NearToExpire"]').is(':checked'),
                Expired: _$itemSearchForm.find('input[name="Expired"]').is(':checked')
            };

            $.ajax({
                url: InventoryItemsUrl,
                data: JSON.stringify(requestData),
                contentType: 'application/json',
                type: "POST",
                success: function (data) {
                    if (data.Status) {
                        let results = $.map(data.Data, function (item) {
                            let rackInfo = item.Rack ? ` - Rack(${item.Rack})` : '';
                            let expiryStatus = item.ExpiryStatus === "Expired" ? " [Expired]" : "";
                            return {
                                label: `${item.ItemName} - ${item.VariationName} - Qty(${item.Quantity}) ${rackInfo} ${expiryStatus} ${item.Formation ? ' - ' + item.Formation : ''}`,
                                value: item.ItemName,
                                data: item
                            };
                        });
                        response(results);
                    } else {
                        response([]);
                    }
                },
                error: function () {
                    response([]);
                    toastr.error("Error searching for items.", "", { timeOut: 1000 });
                }
            });
        },
        minLength: 2,
        select: function (event, ui) {
            let itemData = ui.item.data;
            setTimeout(function () {
                _$searchItemElement.val('');
            }, 10);
            addItemToItemsTable(itemData);
            event.preventDefault();
        },
        open: function () {
            $('.ui-autocomplete').css('z-index', 1050);
        }
    }).data("ui-autocomplete")._renderItem = function (ul, item) {
        return $("<li>")
            .append(`<div>${item.label}</div>`)
            .appendTo(ul);
    };

    _$itemSearchForm.find('input[name="NearToFinish"]').on('click', async function () {
        if (!$(this).is(':checked')) {
            _$purchaseItemsTable.find('tr.js-near-to-finish').each(function () {
                const itemID = $(this).find("input[name='ItemID']").val();
                const variationID = $(this).find("input[name='VariationID']").val();

                if (itemID && variationID) {
                    $(this).remove();
                }
            });

            calculateItemsLineTotal();

            return;
        }

        // Get the supplier ID from the purchase order form
        const supplierID = _$purchaseOrderForm.find('select[name="SupplierID"]').val() || 0;

        const data = {
            SearchString: "",
            SearchType: "Name",
            ManufacturerID: parseInt(_$itemSearchForm.find('select[name="ManufacturerID"]').val()) || 0,
            SupplierID: parseInt(supplierID),
            NearToFinish: true,
            Finished: false,
            NearToExpire: false,
            Expired: false
        };

        try {
            const resp = await $.ajax({
                url: InventoryItemsUrl,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(data)
            });

            if (resp.Status && Array.isArray(resp.Data) && resp.Data.length > 0) {
                $.each(resp.Data, function (index, item) {
                    addItemToItemsTable(item, "js-near-to-finish");
                });
            } else {
                toastr.info("No near to finish items found.", "", { timeOut: 1000 });
            }
        } catch (error) {
            toastr.error("Failed to apply filter due to a technical error.", "", { timeOut: 1000 });
        }
    });

    _$itemSearchForm.find('input[name="NearToExpire"]').on('click', async function () {
        if (!$(this).is(':checked')) {
            _$purchaseItemsTable.find('tr.js-near-to-expire').each(function () {
                const itemID = $(this).find("input[name='ItemID']").val();
                const variationID = $(this).find("input[name='VariationID']").val();

                if (itemID && variationID) {
                    $(this).remove();
                }
            });

            calculateItemsLineTotal();
            return;
        }

        // Get the supplier ID from the purchase order form
        const supplierID = _$purchaseOrderForm.find('select[name="SupplierID"]').val() || 0;

        const data = {
            SearchString: "",
            SearchType: "Name",
            ManufacturerID: parseInt(_$itemSearchForm.find('select[name="ManufacturerID"]').val()) || 0,
            SupplierID: parseInt(supplierID),
            NearToFinish: false,
            Finished: false,
            NearToExpire: true,
            Expired: false
        };

        try {
            const resp = await $.ajax({
                url: InventoryItemsUrl,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(data)
            });

            if (resp.Status && Array.isArray(resp.Data) && resp.Data.length > 0) {
                $.each(resp.Data, function (index, item) {
                    addItemToItemsTable(item, "js-near-to-expire");
                });
            } else {
                toastr.info("No near to expire items found.", "", { timeOut: 1000 });
            }
        } catch (error) {
            toastr.error("Failed to apply filter due to a technical error.", "", { timeOut: 1000 });
        }
    });

    _$itemSearchForm.find('input[name="Finished"]').on('click', async function () {
        if (!$(this).is(':checked')) {
            _$purchaseItemsTable.find('tr.js-finished').each(function () {
                const itemID = $(this).find("input[name='ItemID']").val();
                const variationID = $(this).find("input[name='VariationID']").val();
                if (itemID && variationID) {
                    $(this).remove();
                }
            });

            calculateItemsLineTotal();
            return;
        }

        // Get the supplier ID from the purchase order form
        const supplierID = _$purchaseOrderForm.find('select[name="SupplierID"]').val() || 0;

        const data = {
            SearchString: "",
            SearchType: "Name",
            ManufacturerID: parseInt(_$itemSearchForm.find('select[name="ManufacturerID"]').val()) || 0,
            SupplierID: parseInt(supplierID),
            NearToFinish: false,
            Finished: true,
            NearToExpire: false,
            Expired: false
        };

        try {
            const resp = await $.ajax({
                url: InventoryItemsUrl,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(data)
            });

            if (resp.Status && Array.isArray(resp.Data) && resp.Data.length > 0) {
                $.each(resp.Data, function (index, item) {
                    addItemToItemsTable(item, "js-finished");
                });
            } else {
                toastr.info("No finished items found.", "", { timeOut: 1000 });
            }
        } catch (error) {
            toastr.error("Failed to apply filter due to a technical error.", "", { timeOut: 1000 });
        }
    });

    _$itemSearchForm.find('input[name="Expired"]').on('click', async function () {
        if (!$(this).is(':checked')) {
            _$purchaseItemsTable.find('tr.js-expired').each(function () {
                const itemID = $(this).find("input[name='ItemID']").val();
                const variationID = $(this).find("input[name='VariationID']").val();
                if (itemID && variationID) {
                    $(this).remove();
                }
            });

            calculateItemsLineTotal();
            return;
        }

        // Get the supplier ID from the purchase order form
        const supplierID = _$purchaseOrderForm.find('select[name="SupplierID"]').val() || 0;

        const data = {
            SearchString: "",
            SearchType: "Name",
            ManufacturerID: parseInt(_$itemSearchForm.find('select[name="ManufacturerID"]').val()) || 0,
            SupplierID: parseInt(supplierID),
            NearToFinish: false,
            Finished: false,
            NearToExpire: false,
            Expired: true
        };

        try {
            const resp = await $.ajax({
                url: InventoryItemsUrl,
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(data)
            });

            if (resp.Status && Array.isArray(resp.Data) && resp.Data.length > 0) {
                $.each(resp.Data, function (index, item) {
                    addItemToItemsTable(item, "js-expired");
                });
            } else {
                toastr.info("No expired items found.", "", { timeOut: 1000 });
            }
        } catch (error) {
            toastr.error("Failed to apply filter due to a technical error.", "", { timeOut: 1000 });
        }
    });

    $(document).on("keydown", ".js-qty-input", function (event) {
        if (event.key === "Enter") {
            event.preventDefault();
            $(".js-search-item").focus();
        }
    });

    function addItemToItemsTable(data, jsClass = "") {
        const tableBody = _$purchaseItemsTable.find("tbody");

        const existingItem = tableBody.find(`tr`).toArray().some(row => {
            const itemID = $(row).find("input[name='ItemID']").val();
            const variationID = $(row).find("input[name='VariationID']").val();
            return itemID === data.ItemID.toString() && variationID === data.VariationID.toString();
        });

        if (existingItem) {
            toastr.info("This item is already added.", "", { timeOut: 1500 });
            return;
        }

        const rowHtml = `
        <tr class="${jsClass}">
            <td>${data.ItemName} - ${data.VariationName}</td>
            <td>
                <input type="hidden" name="ItemID" value="${data.ItemID}">
                <input type="hidden" name="VariationID" value="${data.VariationID}">
                <input type="hidden" name="MaxQty" value="${data.Quantity}">
                <input type="number" class="form-control js-qty-input" style="width:70%" name="Qty" placeholder="Enter Quantity" min="1" value="1">
            </td>
            <td>${data.CostPrice}</td>
            <td>
                <input type="text" class="form-control" style="width:100%" name="UnitCostWithTax" value="${(data.CostPrice * (1 + (data.Tax || 0) / 100)).toFixed(2)}" readonly>
            </td>
            <td>
                <input type="text" class="form-control" style="width:100%" name="LineTotal" value="${(data.CostPrice * (1 + (data.Tax || 0) / 100)).toFixed(2)}" readonly>
            </td>
            <td>
                <button class="btn btn-danger btn-sm tip js-remove-item-btn" title="Delete Item" type="button">
                    <i class="fa fa-trash"></i>
                </button>
            </td>
        </tr>`;

        tableBody.append(rowHtml);
        let newRow = tableBody.find("tr:last");
        let qtyInput = newRow.find(".js-qty-input");
        qtyInput.focus().select();
        tableBody.find(".js-remove-item-btn").off("click").on("click", function () {
            $(this).closest("tr").remove();
            calculateItemsLineTotal();
        });
        calculateItemsLineTotal();
    }

    _$purchaseItemsTable.on('change', 'input[name="LineTotal"]', function () {
        calculateItemsLineTotal();
    });

    _$purchaseItemsTable.on('keyup', 'input[name="Qty"]', function () {
        const $row = $(this).closest('tr');
        calculateLineTotal($row);
        calculateItemsLineTotal();
    });

    function calculateItemsLineTotal() {
        let total = 0;
        _$purchaseItemsTable.find('input[name="LineTotal"]').each(function () {
            const value = parseFloat($(this).val()) || 0;
            total += value;
        });
        $('#subTotal').val(total.toFixed(2));
        if (total > 0) {
            _$savePurchaseBtn.removeClass('disabled').prop('disabled', false);
            _$saveAndPrintPurchaseBtn.removeClass('disabled').prop('disabled', false);
        } else {
            _$savePurchaseBtn.addClass('disabled').prop('disabled', true);
            _$saveAndPrintPurchaseBtn.addClass('disabled').prop('disabled', true);
        }
    }

    function calculateLineTotal($row) {
        const quantity = parseFloat($row.find('input[name="Qty"]').val()) || 1;
        const costWithTax = parseFloat($row.find('input[name="UnitCostWithTax"]').val()) || 0;
        const lineTotal = quantity * costWithTax;
        $row.find('input[name="LineTotal"]').val(lineTotal.toFixed(2));
    }

    _$purchaseItemsTable.on('click', '.js-remove-item-btn', function () {
        $(this).closest('tr').remove();
        calculateItemsLineTotal();
    });

    _$savePurchaseBtn.on('click', async function (event) {
        const _$saveBtn = $(this);
        if (!_$purchaseOrderForm.valid()) {
            scrollToInvalidInput(_$purchaseOrderForm, event);
            return;
        }
        _$saveBtn.text('Saving...').prop('disabled', true);
        const url = SavePurchaseOrderUrl;
        let purchaseDetails = _$purchaseOrderForm.serializeObject();
        purchaseDetails.SubTotal = $('#subTotal').val();
        let data = {
            Order: purchaseDetails,
            OrderItems: readPurchaseItems(),
            '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message } = resp;
            if (status) {
                toastr.success(message, "", { timeOut: 1000 });
                bootbox.dialog({
                    title: 'Purchase Order Saved!',
                    message: 'Purchase Order has been saved Successfully, Do you want to add another?',
                    buttons: {
                        cancel: {
                            label: '<i class="fa fa-times"></i> No',
                            className: 'btn-default',
                            callback: function () {
                                window.location.href = PurchaseOrdersUrl;
                            }
                        },
                        confirm: {
                            label: '<i class="fa fa-check"></i> Yes',
                            className: 'btn-primary',
                            callback: function () {
                                window.location.reload();
                            }
                        },
                    }
                });
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        } catch (error) {
            toastr.error("An error occurred while saving the purchase.", "", { timeOut: 500 });
        } finally {
            _$saveBtn.text('Save Purchase').prop('disabled', false);
        }
    });

    _$saveAndPrintPurchaseBtn.on('click', async function (event) {
        const _$saveBtn = $(this);
        if (!_$purchaseOrderForm.valid()) {
            scrollToInvalidInput(_$purchaseOrderForm, event);
            return;
        }
        _$saveBtn.text('Saving...').prop('disabled', true);
        const url = SavePurchaseOrderUrl;
        let purchaseDetails = _$purchaseOrderForm.serializeObject();
        purchaseDetails.SubTotal = $('#subTotal').val();
        let data = {
            Order: purchaseDetails,
            OrderItems: readPurchaseItems(),
            '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: responseID } = resp;
            toastr.success(message, "", { timeOut: 1000 });
            if (status) {
                window.open(`${printPurchaseOrderUrl}?ID=${responseID}`, '_blank');
                bootbox.dialog({
                    title: 'Purchase Order Saved!',
                    message: 'Purchase Order has been saved Successfully, Do you want to add another?',
                    closeButton: false,
                    buttons: {
                        cancel: {
                            label: '<i class="fa fa-times"></i> No',
                            className: 'btn-default',
                            callback: function () {
                                window.location.href = PurchaseOrdersUrl;
                            }
                        },
                        confirm: {
                            label: '<i class="fa fa-check"></i> Yes',
                            className: 'btn-primary',
                            callback: function () {
                                window.location.reload();
                            }
                        },
                    }
                });
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        } catch (error) {
            toastr.error("An error occurred while saving the purchase.", "", { timeOut: 500 });
        } finally {
            _$saveBtn.text('Save Purchase').prop('disabled', false);
        }
    });

    function readPurchaseItems() {
        let itemsArray = [];
        _$purchaseItemsTable.find('tbody tr').each(function () {
            const row = $(this);
            const obj = {
                Name: row.find('td:eq(0)').text(),
                Qty: parseInt(row.find('td:eq(1) input[name="Qty"]').val()),
                ItemID: parseInt(row.find('td:eq(1) input[name="ItemID"]').val()),
                VariationID: parseInt(row.find('td:eq(1) input[name="VariationID"]').val()),
                UnitCost: parseFloat(row.find('td:eq(2)').text()),
                UnitCostWithTax: parseFloat(row.find('td:eq(3) input[name="UnitCostWithTax"]').val()),
                LineTotal: parseFloat(row.find('td:eq(4) input[name="LineTotal"]').val())
            };
            itemsArray.push(obj);
        });
        return itemsArray;
    }

    function formatDateForUpdate(dateString) {
        const timestamp = Date.parse(dateString);
        const date = new Date(timestamp);
        const day = ('0' + date.getDate()).slice(-2);
        const month = ('0' + (date.getMonth() + 1)).slice(-2);
        const year = date.getFullYear();
        return `${month}/${day}/${year}`;
    }
})(jQuery)