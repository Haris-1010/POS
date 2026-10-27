(function ($) {
    const _$saleItemsTable = $('#saleItemsTable'),
        _$paymentItemsTable = $('#paymentItemsTable'),
        _$saleInfoForm = $('#saleInfoForm');

    const _$itemSearchForm = $('#itemSearchForm'),
        _$searchItemElement = _$itemSearchForm.find('.js-search-item');

    const _$saleCalculationsForm = $('#saleCalculationsForm');
    const _$addPaymentBtn = $('#addPaymentBtn');

    const _$saveInvoiceBtn = $('#saveInvoiceBtn'),
        _$saveAndPrintInvoiceBtn = $('#saveAndPrintInvoiceBtn');

    const permissionsForm = $('#permissionsForm').serializeObject();
    let permissions = {
        isEMRIntegration: "1",
    };
    let isBarcodeScan = false;

    $(function () {
        $('.js-search-item').on('keypress', function (e) {
            if (e.which === 13) {
                const inputVal = $(this).val().trim();
                if (/^\d{10,13}$/.test(inputVal)) {
                    isBarcodeScan = true;
                } else {
                    isBarcodeScan = false;
                }

                e.preventDefault();
                $(this).autocomplete("search");
            }
        });
        _$searchItemElement.focus();
        $.validator.addMethod("chooseCustomer", function (value, element) {
            return value.trim() !== "0" && value.trim() !== "";
        }, "Please Select a Customer");

        $.validator.addMethod("chooseSalePerson", function (value, element) {
            return value !== "0";
        }, "Please choose a Sales Person");

        _$saleInfoForm.validate({
            rules: {
                SearchCustomer: {
                    chooseCustomer: true
                },
                SalePerson: {
                    chooseSalePerson: true
                }
            },
            messages: {
                SearchCustomer: {
                    chooseCustomer: "Please Select a Customer"
                },
                SalePerson: {
                    chooseSalePerson: "Please choose a SalePerson"
                }
            }
        });
        getItemsOfReferralOrder();
        setUpFieldsInCaseOfUpdate();
    })

    async function getItemsOfReferralOrder() {
        if (referralOrderID <= 0) {
            return;
        }
        const data = {
            ReferralOrderID: referralOrderID,
        };

        const url = GetItemsByReferralOrderUrl;

        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: responseData } = resp;

            if (status && Array.isArray(responseData) && responseData.length > 0) {
                $.each(responseData, function (index, item) {
                    addItemToItemsTable(item);
                });
            }
        } catch (error) {
            toastr.error("Failed to apply filter due to a technical error.", "", { timeOut: 500 });
        }
    }

    function setUpFieldsInCaseOfUpdate() {
        const invoiceID = parseInt(_$saleInfoForm.find('input[name="ID"]').val()) || 0;
        if (invoiceID <= 0) {
            return;
        }

        let invoiceDate = _$saleInfoForm.find('input[name="InvoiceDateInCaseOfUpdate"]').val();
        if (invoiceDate) {
            _$saleInfoForm.find('input[name="InvoiceDate"]').datepicker('setDate', formatDateForUpdate(invoiceDate));
        }

        _$paymentItemsTable.find('tbody tr').each(function () {
            const row = $(this);
            let oldDate = row.find('td:eq(0) input[name="Date"]').val();
            row.find('td:eq(0) input[name="Date"]').datepicker('setDate', formatDateForUpdate(oldDate));
        });

        _$paymentItemsTable.find('tbody').find(".js-remove-payment-btn").off("click").on("click", function () {
            $(this).closest("tr").remove();
            totalPaidAmount();
        });

        _$saleItemsTable.find("tbody").find(".js-remove-item-btn").off("click").on("click", function () {
            $(this).closest("tr").remove();
            calculateSaleItemsTotal();
            totalPaidAmount();
        });

        _$saveInvoiceBtn.prop("disabled", false);
    }

    _$searchItemElement.autocomplete({
        source: function (request, response) {
            $.ajax({
                url: SearchItemsUrl,
                data: {
                    SearchString: request.term, 
                    SearchType: _$itemSearchForm.find('select[name="SearchType"]').val(),
                    NearToExpire: _$itemSearchForm.find('input[name="NearToExpire"]').is(':checked') ? true : false,
                    ManufacturerID: _$itemSearchForm.find('select[name="ManufacturerID"]').val(),
                    SupplierID: _$itemSearchForm.find('select[name="SupplierID"]').val() 
                },
                type: "POST",
                success: function (data) {
                    let results = $.map(data.Data, function (item) {

                        let rackInfo = item.Rack ? ` - Rack(${item.Rack})` : '';
                        let expiryStatus = item.ExpiryStatus === "Expired" ? " [Expired]" : "";
                         
                        let expiryInfo = '';
                        if (item.ExpiryDate) { 
                            let timestamp = parseInt(item.ExpiryDate.replace(/\D/g, ''), 10);
                            let dateObj = new Date(timestamp); 
                            expiryInfo = ` - Exp: ${dateObj.toLocaleDateString('en-GB')}`;
                        }

                        return {
                            label: `${item.ItemName} - ${item.VariationName} - Qty(${item.Quantity}) ${rackInfo} ${expiryStatus}  ${item.Formation ? ' - ' + item.Formation : ''} ${expiryInfo}`,
                            value: item.ItemName,
                            data: item
                        };
                    }); 

                    if (isBarcodeScan) {
                        if (results.length === 1) {
                            addItemToItemsTable(results[0].data);
                            _$searchItemElement.removeClass("ui-autocomplete-loading");
                            _$searchItemElement.val('').focus();
                            isBarcodeScan = false;
                            return;
                        }

                        _$searchItemElement.removeClass("ui-autocomplete-loading");
                        response(results);
                        isBarcodeScan = false;
                        return;
                    }
                    response(results);
                }
            });
        },
        minLength: 2,
        delay: 700,
        select: function (event, ui) {
            let itemData = ui.item.data;
            setTimeout(function () {
                _$searchItemElement.val('');
            }, 10);
            addItemToItemsTable(itemData);
        },
        open: function () {
            $('.ui-autocomplete').css('z-index', 1050);
        }
    }).data("ui-autocomplete")._renderItem = function (ul, item) {
        return $("<li>")
            .append(`<div>${item.label}</div>`)
            .appendTo(ul);
    };

    function calculateItemsLineTotal() {
        let total = 0;
        _$saleItemsTable.find('input[name="LineTotal"]').each(function () {
            const value = parseFloat($(this).val()) || 0;
            total += value;
        });
        $('#subTotal').val(total.toFixed(2));
        if (total > 0) {
            _$saveInvoiceBtn.removeClass('disabled').prop('disabled', false);
            _$saveAndPrintInvoiceBtn.removeClass('disabled').prop('disabled', false);
        } else {
            _$saveInvoiceBtn.addClass('disabled').prop('disabled', true);
            _$saveAndPrintInvoiceBtn.addClass('disabled').prop('disabled', true);
        }
    }

    _$itemSearchForm.find('input[name="NearToExpire"]').on('click', async function () {
        if (!$(this).is(':checked')) {
            _$saleItemsTable.find('tr.js-near-to-expire').each(function () {
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

        const data = {
            SearchString: "",
            SearchType: "Name",
            ManufacturerID: parseInt(_$itemSearchForm.find('select[name="ManufacturerID"]').val()) || 0,
            SupplierID: 0,
            NearToFinish: false,
            Finished: false,
            NearToExpire: true,
            Expired: false
        };

        try {
            const resp = await $.ajax({
                url: SearchItemsUrl,
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
    //_$itemSearchForm.find('input[name="NearToExpire"]').on('click', async function () {
    //    if (!$(this).is(':checked')) {
    //        _$saleItemsTable.find('tr.js-near-to-expire').each(function () {
    //            const itemID = $(this).find("input[name='ItemID']").val();
    //            const variationID = $(this).find("input[name='VariationID']").val();

    //            if (itemID && variationID) {
    //                $(this).remove();
    //            }
    //        });

    //        calculateItemsLineTotal();
    //        totalPaidAmount();
    //        return;
    //    }

    //    const url = SearchItemsUrl;

    //    try {
    //        const resp = await $.post(url, {
    //        });

    //        const { Status: status, Message: message, Data: responseData } = resp;

    //        if (status && Array.isArray(responseData) && responseData.length > 0) {
    //            $.each(responseData, function (index, item) {
    //                addItemToItemsTable(item, "js-near-to-expire");
    //            });
    //        }
    //    } catch (error) {
    //        toastr.error("Failed to apply filter due to a technical error.", "", { timeOut: 500 });
    //    }
    //});

    $(document).on("keydown", ".js-qty-input", function (event) {
        if (event.key === "Enter") { // || event.key === "Tab"
            event.preventDefault(); // Stop default tab behavior
            $(".js-search-item").focus(); // Move focus back to search
        }
    });

    // Add Item to Invoice Items Table
    function addItemToItemsTable(data, jsClass = "") {
        if (data.ExpiryStatus == "Expired") {
            toastr.error("This item is Expired.", "", { timeOut: 2000 });
            return;
        }

        if (data.Quantity == 0) {
            toastr.error("This item quantity is zero. Please update item stock for sale.", "", { timeOut: 2000 });
            return;
        }
        const tableBody = _$saleItemsTable.find("tbody");

        const existingItem = tableBody.find(`tr`).toArray().some(row => {
            const itemID = $(row).find("input[name='ItemID']").val();
            const variationID = $(row).find("input[name='VariationID']").val();
            return itemID === data.ItemID.toString() && variationID === data.VariationID.toString();
        });

        if (existingItem) {
            const existingRow = tableBody.find(`tr`).filter(function () {
                const itemID = $(this).find("input[name='ItemID']").val();
                const variationID = $(this).find("input[name='VariationID']").val();
                return itemID === data.ItemID.toString() && variationID === data.VariationID.toString();
            });

            const qtyInput = existingRow.find("input[name='Qty']");
            const maxQty = parseInt(existingRow.find("input[name='MaxQty']").val());
            let currentQty = parseInt(qtyInput.val());

            if (isBarcodeScan) {
                if (currentQty < maxQty) {
                    qtyInput.val(currentQty + 1);
                } else {
                    toastr.warning("Reached maximum quantity.", "", { timeOut: 1500 });
                }
                calculateSaleItemsTotal();
            } else {
                toastr.info("This item is already added.", "", { timeOut: 2000 });
            }

            return;
        }

        const rowHtml = `
        <tr class="${jsClass}">
            <td>${data.ItemName} - ${data.VariationName}</td>
            <td>${data.Rack || ''}</td>
            <td>
                <input type="hidden" name="ItemID" value="${data.ItemID}">
                <input type="hidden" name="VariationID" value="${data.VariationID}">
                <input type="hidden" name="MaxQty" value="${data.Quantity}">
                <input type="number" class="form-control js-qty-input" style="width:70%" name="Qty" placeholder="Enter Quantity" value="1" min="1" max="${data.Quantity}">
            </td>
            <td>${data.SellingPrice}</td>
            <td>
                <div class="d-flex border-0">
                    <select class="form-control js-discount-list" name="DiscountID" data-rowindex="${data.VariationID}" style="min-width:5rem !important; font-size:.8rem !important;">
                    ${generateDiscountDropDown()}
                    </select>
                    <select class="form-control js-discount-type" name="DiscountType" data-rowindex="${data.VariationID}" style="display:none;margin-inline:5px !important; min-width:5rem !important; font-size:.8rem !important;">
                        <option value="value">In Value</option>
                        <option value="percentage">Percentage</option>
                    </select>
                    <input type="number" class="form-control" style="width:60%;display:none;" name="Discount" placeholder="Enter Discount" min="0" value="0">
                </div>
            </td>
            <td>
                <input type="hidden" name="ItemTax" value="${data.Tax || 0}">
                <input type="text" class="form-control" style="width:100%" name="UnitCostWithTax" value="${(data.SellingPrice * (1 + (data.Tax || 0) / 100)).toFixed(2)}" readonly>
            </td>
            <td>
                <input type="text" class="form-control" style="width:100%" name="LineTotal" value="${(data.SellingPrice * (1 + (data.Tax || 0) / 100)).toFixed(2)}" readonly>
            </td>
            <td>
                <button class="btn btn-danger btn-sm tip js-remove-item-btn" title="Delete Item" type="button">
                    <i class="fa fa-trash"></i>
                </button>
            </td>
        </tr>`;

        tableBody.append(rowHtml);

        // Move focus to the newly added quantity input
        let newRow = tableBody.find("tr:last"); // Get the last added row
        let qtyInput = newRow.find(".js-qty-input"); // Get quantity input
        qtyInput.focus().select(); // Focus on the quantity input

        tableBody.find(".js-remove-item-btn").off("click").on("click", function () {
            $(this).closest("tr").remove();
            calculateSaleItemsTotal();
            totalPaidAmount();
        });
        calculateSaleItemsTotal();
    }

    function generateDiscountDropDown() {
        let discountOptions = `<option value="0" data-discount-type="select" data-discount-value="0">Select Discount</option>`;
        discountOptions += discountsList.map(function (discount) {
            return `<option value="${discount.ID}" data-discount-type="${discount.DiscountType}" data-discount-value="${discount.DiscountValue}">${discount.DiscountName}</option>`;
        }).join('');
        discountOptions += `<option value="0" data-discount-type="custom" data-discount-value="0">Custom</option>`;
        return discountOptions;
    }

    // Add Payment Item to Payments Table
    _$addPaymentBtn.on('click', function () {
        const subTotalAfterTax = parseFloat(_$saleCalculationsForm.find('input[name="SubtotalAfterTax"]').val()) || 0;
        const tableBodyEl = _$paymentItemsTable.find('tbody');
        const HTML = `<tr>
                           <td>
                               <input type="text" class="form-control js-datepicker" name="Date" placeholder="Enter date" />
                           </td>
                           <td>
                               <input type="number" class="form-control" name="PaymentAmount" placeholder="Enter amount" value="${subTotalAfterTax.toFixed("2")}" min="0" />
                           </td>
                           <td>
                               <select class="form-control js-select2 js-payment-mode" name="PaymentMode">
                                        <option value="Cash">Cash</option>
                                        <option value="JazzCash">JazzCash</option>
                                        <option value="EasyPaisa">EasyPaisa</option>
                                        <option value="Card">Card</option>
                               </select>
                           </td>
                           <td>
                               <input type="file" class="form-control js-attachment" name="Attachment" />
                               <input type="hidden" class="js-attachment-url" name="AttachmentURL" />
                           </td>
                           <td class="actions-td">
                              <button class="btn btn-danger btn-sm mt-1 tip js-remove-payment-btn" title="Delete Payment" type="button">
                                     <i class="fa fa-trash"></i>
                               </button>
                           </td>
                      </tr>`;
        tableBodyEl.append(HTML);
        tableBodyEl.find('tr:last-child input.js-datepicker').datepicker('setDate', new Date());
        tableBodyEl.find('tr:last-child select.js-select2').select2({ width: '99%' });
        totalPaidAmount();
        tableBodyEl.find(".js-remove-payment-btn").off("click").on("click", function () {
            $(this).closest("tr").remove();
            totalPaidAmount();
        });
    });

    //$(document).on('change','.js-payment-mode', function () {
    //    const selectedPaymentMode = $(this).val();
    //    const taxDropdown = _$saleCalculationsForm.find("select[name='TaxID']");
    //    let matchedOption = null;

    //    taxDropdown.find("option").each(function () {
    //        const paymentModes = $(this).data("payment-mode");
    //        if (paymentModes) {
    //            const paymentModesArray = paymentModes.split(",");
    //            if (paymentModesArray.includes(selectedPaymentMode)) {
    //                matchedOption = $(this);
    //                return false;
    //            }
    //        }
    //    });

    //    if (matchedOption) {
    //        taxDropdown.val(matchedOption.val()).trigger("change");
    //    }
    //})
    // region Save Invoices
    _$saveInvoiceBtn.on("click", function (event) {
        if (!_$saleInfoForm.valid()) {
            scrollToInvalidInput(_$saleInfoForm, event);
            return false; // Prevent further execution
        }
        const _$thisBtn = $(this);
        _$thisBtn.text('Saving..').prop("disabled", true);

        let data = {
            SaleInvoice: _$saleInfoForm.serializeObject(),
            SaleInvoiceFinancialDetails: _$saleCalculationsForm.serializeObject(),
            SaleInvoiceItems: readInvoiceItems(),
            SaleInvoicePayments: readInvoicePayments(),
        };
        const url = SaveSaleUrl;

        $.post(url, data, function (resp) {
            const { Status: status, Message: message, Data: responseID } = resp;
            _$thisBtn.text('Save Invoice').prop("disabled", false);

            if (status) {
                bootbox.dialog({
                    title: 'Invoice Saved!',
                    message: 'Invoice has been saved Successfully, Do you want to add another invoice',
                    closeButton: false, // Hides the close (×) button
                    buttons: {
                        cancel: {
                            label: '<i class="fa fa-times"></i> No',
                            className: 'btn-default',
                            callback: function () {
                                window.location.href = saleInvoicesUrl;
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
        }).fail(function () {
            _$thisBtn.text('Save Invoice').prop("disabled", false);
            toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
        });
    });

    _$saveAndPrintInvoiceBtn.on("click", function (event) {
        if (!_$saleInfoForm.valid()) {
            scrollToInvalidInput(_$saleInfoForm, event);
            return false;
        }
        const _$thisBtn = $(this);
        _$thisBtn.text('Saving..').prop("disabled", true);

        let data = {
            SaleInvoice: _$saleInfoForm.serializeObject(),
            SaleInvoiceFinancialDetails: _$saleCalculationsForm.serializeObject(),
            SaleInvoiceItems: readInvoiceItems(),
            SaleInvoicePayments: readInvoicePayments(),
        };
        const url = SaveSaleUrl;

        $.post(url, data, function (resp) {
            const { Status: status, Message: message, Data: responseID } = resp;
            _$thisBtn.text('Save & Print').prop("disabled", false);
            if (status) {
                window.open(`${printSaleInvoiceThermalUrl}?ID=${responseID}`, '_blank') 
                bootbox.dialog({
                    title: 'Invoice Saved!',
                    message: 'Invoice has been saved Successfully, Do you want to add another invoice',
                    closeButton: false, // Hides the close (×) button
                    buttons: {
                        cancel: {
                            label: '<i class="fa fa-times"></i> No',
                            className: 'btn-default',
                            callback: function () {
                                window.location.href = saleInvoicesUrl;
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
                /*window.location.href = `/Pharmacy/Print/PrintSaleInvoiceThermalV2?ID=${responseID}`;*/
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        }).fail(function () {
            _$thisBtn.text('Save & Print').prop("disabled", false);
            toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
        });
    });

    function readInvoiceItems() {
        let itemsArray = [];
        _$saleItemsTable.find('tbody tr').each(function () {
            const row = $(this);
            const obj = {
                ItemID: parseInt(row.find('td:eq(2) input[name="ItemID"]').val()),
                VariationID: parseInt(row.find('td:eq(2) input[name="VariationID"]').val()),
                Name: row.find('td:eq(0)').text(),
                Qty: parseFloat(row.find('td:eq(2) input[name="Qty"]').val()),
                UnitCost: parseFloat(row.find('td:eq(3)').text()),
                DiscountID: row.find('td:eq(4) select[name="DiscountID"]').val(),
                DiscountType: row.find('td:eq(4) select[name="DiscountType"]').val(),
                Discount: parseFloat(row.find('td:eq(4) input[name="Discount"]').val()),
                Tax: parseFloat(row.find('td:eq(5) input[name="ItemTax"]').val()) || 0.00,
                ItemTax: parseFloat(row.find('td:eq(5) input[name="ItemTax"]').val()) || 0.00,
                UnitCostWithTax: parseFloat(row.find('td:eq(5) input[name="UnitCostWithTax"]').val()) || 0.00,
                UnitSalePriceInclusiveTax: parseFloat(row.find('td:eq(5) input[name="UnitCostWithTax"]').val()),
                LineTotal: parseFloat(row.find('td:eq(6) input[name="LineTotal"]').val())
            };
            itemsArray.push(obj);
        });
        return itemsArray;
    }

    function readInvoicePayments() {
        let itemsArray = [];

        _$paymentItemsTable.find('tbody tr').each(function () {
            const row = $(this);

            const obj = {
                Date: formatDateToISO(row.find('input[name="Date"]').val()),
                PaidAmount: parseFloat(row.find('input[name="PaymentAmount"]').val()) || 0,
                PaymentMode: row.find('select[name="PaymentMode"]').val(),
                AttachmentURL: row.find('input[name="AttachmentURL"]').val() || "",
            };

            itemsArray.push(obj);
        });

        return itemsArray;
    }

    //$(document).on("change", 'input[name="Attachment"]', function () {
    //    if (this.files.length > 0) {
    //        const file = this.files[0];
    //        const reader = new FileReader();
    //        reader.onload = (e) => {
    //            $(this).closest("tr").find('input[name="AttachmentURL"]').val(e.target.result.split(',')[1]);
    //        };
    //        reader.readAsDataURL(file);
    //    }
    //});

    $(document).on("change", 'input[name="Attachment"]', function () {
        if (this.files.length > 0) {
            const file = this.files[0];
            const objectURL = URL.createObjectURL(file);
            const reader = new FileReader();

            reader.onload = (e) => {
                const base64Data = e.target.result.split(',')[1];

                $(this).closest("td").find('input[name="AttachmentURL"]').val(base64Data);

                let actionTd = $(this).closest("td").next(".actions-td");

                let eyeButton = actionTd.find(".js-view-img-base64-btn");
                if (eyeButton.length === 0) {
                    let newButton = `<button class="btn btn-secondary btn-sm tip js-view-img-base64-btn"
                                            title="View Image" type="button" data-url="${objectURL}">
                                        <i class="fa fa-eye text-white"></i>
                                    </button>`;
                    actionTd.prepend(newButton);
                } else {
                    eyeButton.attr("data-url", objectURL);
                }
            };

            reader.readAsDataURL(file);
        }
    });

    $(document).on("click", ".js-view-img-base64-btn", function () {
        let imageUrl = $(this).attr("data-url");
        if (imageUrl) {
            viewFile(imageUrl);
        }
    });

    $(document).on("click", '.js-view-img-btn', function () {
        const path = $(this).closest('tr').find('input[name="AttachmentURL"]').val();
        viewFile(path);
    });

    _$saleItemsTable.on('keyup', 'input[name="Discount"]', function () {
        const $row = $(this).closest('tr');
        const isValid = validateDiscount($row);
        if (isValid) {
            calculateUnitCostWithTax($row);
            calculateSaleItemsTotal();
        }
    });

    _$saleItemsTable.on('change', 'input[name="LineTotal"]', function () {
        const $row = $(this).closest('tr');
        calculateLineTotal($row);
        calculateSaleItemsTotal()
    });

    _$saleItemsTable.on('change', '.js-discount-list', function () {
        const $row = $(this).closest('tr');
        const selectedDiscount = $(this).find('option:selected');
        const discountType = selectedDiscount.data('discount-type');
        const showFields = discountType === 'custom';

        if (!showFields) {
            $row.find('.js-discount-type, input[name="Discount"]').toggle(showFields);
            $row.find('input[name="Discount"]').val(selectedDiscount.data('discount-value'));
            $row.find('.js-discount-type').val(discountType);
            if (validateDiscount($row)) {
                calculateUnitCostWithTax($row);
                calculateSaleItemsTotal();
            }
        } else {
            $row.find('.js-discount-type, input[name="Discount"]').toggle(showFields);
        }
    });

    _$saleItemsTable.on('change', '.js-discount-type', function () {
        const $row = $(this).closest('tr');
        const isValid = validateDiscount($row);
        if (isValid) {
            calculateUnitCostWithTax($row);
            calculateSaleItemsTotal();
        }
    });

    _$saleItemsTable.on('keyup', 'input[name="Qty"]', function () {
        let $row = $(this).closest('tr');
        let $qtyInput = $(this);
        let qty = $qtyInput.val().trim();

        let maxQty = parseFloat($row.find('input[name="MaxQty"]').val());

        // If qty is empty or 0, set it to 1 and update the variable
        if (qty === '' || parseFloat(qty) === 0) {
            $qtyInput.val(1);
            qty = 1; // Ensures further calculations use the updated value
        }

        if (qty > maxQty) {
            toastr.info(`Quantity cannot be greater than item stock.`, "", { timeOut: 2000 });
            $qtyInput.val(maxQty);
            qty = maxQty;
        }

        validateDiscount($row);
        calculateUnitCostWithTax($row);
        calculateSaleItemsTotal();
    });

    function calculateSaleItemsTotal() {
        let total = 0;
        _$saleItemsTable.find('input[name="LineTotal"]').each(function () {
            const value = parseFloat($(this).val()) || 0;
            total += value;
        });
        _$saleCalculationsForm.find('input[name="SubTotal"]').val(total.toFixed(2));
        _$saleCalculationsForm.find('input[name="SubtotalAfterDiscount"]').val(total.toFixed(2));
        _$saleCalculationsForm.find('input[name="SubtotalAfterTax"]').val(total.toFixed(2));
        _$saleCalculationsForm.find('input[name="RemainingAmount"]').val(total.toFixed(2));
        calculateTotals();
        totalPaidAmount();
        if (total > 0) {
            _$addPaymentBtn.removeClass('disabled').prop('disabled', false);
            _$saveInvoiceBtn.removeClass('disabled').prop('disabled', false);
            _$saveAndPrintInvoiceBtn.removeClass('disabled').prop('disabled', false);
        } else {
            _$addPaymentBtn.addClass('disabled').prop('disabled', true);
            _$saveInvoiceBtn.addClass('disabled').prop('disabled', true);
            _$saveAndPrintInvoiceBtn.addClass('disabled').prop('disabled', true);
        }
    }

    function validateDiscount($row) {
        const $discountInput = $row.find('input[name="Discount"]');
        const discountValue = parseFloat($discountInput.val()) || 0;
        const unitCost = parseFloat($row.find('td:eq(3)').text()) || 0;
        const quantity = parseInt($row.find('input[name="Qty"]').val()) || 1;
        const discountType = $row.find('.js-discount-type').val();
        const $parentDiv = $discountInput.closest('div');
        const $errorMessage = $parentDiv.siblings('.error-message');

        // Reset previous error state
        $parentDiv.siblings('.error-message').remove();
        $discountInput.removeClass('is-invalid').removeClass('is-valid');

        if (!$discountInput.val() || discountType === 'select') {
            return true;
        }

        let isValid = true;
        let errorMessage = '';

        if (discountType === 'percentage') {
            if (discountValue < 0 || discountValue > 100) {
                isValid = false;
                errorMessage = 'Percentage discount must be between 0 and 100.';
            }
        } else if (discountType === 'value') {
            if (discountValue < 0 || discountValue > unitCost * quantity) {
                isValid = false;
                errorMessage = 'Value discount cannot exceed the unit cost or be negative.';
            }
        }

        if (!isValid) {
            $discountInput.addClass('is-invalid');
            $parentDiv.after(`<span class="error-message text-danger">${errorMessage}</span>`);
        } else {
            $discountInput.addClass('is-valid');
        }

        return isValid;
    }

    function calculateUnitCostWithTax($row) {
        if (!($row instanceof jQuery)) {
            $row = $($row);
        }
        let quantity = 1;
        let costPrice = 0;
        let discountType = "";
        let discountValue = 0;
        let taxValue = 0;

        if ($row.find('input[name="ItemTax"]').val() != undefined) {
            quantity = parseInt($row.find('input[name="Qty"]').val()) || 1;
            costPrice = parseFloat($row.find('td:eq(3)').text()) || 0;
            discountType = $row.find('.js-discount-type').val();
            discountValue = parseFloat($row.find('input[name="Discount"]').val()) || 0;
            taxValue = parseFloat($row.find('input[name="ItemTax"]').val()) || 0;
        } else {
            quantity = $row[0].Qty || 1;
            costPrice = parseFloat($row[0].SellingPrice) || 0;
            discountType = $row[0].DiscountType;
            discountValue = parseFloat($row[0].Discount) || 0;
            taxValue = parseFloat($row[0].ItemTax) || 0;
        }

        let finalCost = costPrice;

        if (discountType === 'percentage') {
            finalCost -= (finalCost * discountValue) / 100;
        } else if (discountType === 'value') {
            //if (quantity > 1) {
            //    let discountPerItem = discountValue / quantity;
            //    finalCost -= discountPerItem;
            //} else {
            //    finalCost -= discountValue;
            //}
            finalCost -= discountValue;
        } else {
            finalCost = costPrice;
        }

        finalCost = Math.max(0, finalCost);

        const unitCostWithTax = finalCost >= 0
            ? finalCost + (finalCost * taxValue) / 100
            : costPrice + (costPrice * taxValue) / 100;

        $row.find('input[name="UnitCostWithTax"]').val(unitCostWithTax.toFixed(2));
        calculateLineTotal($row);

        return {
            unitCostWithTax,
        };
    }

    function calculateLineTotal($row) {
        const quantity = parseInt($row.find('input[name="Qty"]').val()) || 1;
        const costWithTax = parseFloat($row.find('input[name="UnitCostWithTax"]').val()) || 0;
        const lineTotal = quantity * costWithTax;
        $row.find('input[name="LineTotal"]').val(lineTotal.toFixed(2));
    }

    _$saleCalculationsForm.find('select[name="DiscountID"], select[name="DiscountType"], input[name="DiscountAmount"], select[name="TaxID"]').on('input change', function () {
        calculateTotals();
        totalPaidAmount();
    });

    function calculateTotals() {
        const $discountInput = _$saleCalculationsForm.find('input[name="DiscountAmount"]');
        const discountValue = parseFloat($discountInput.val()) || 0;

        const $subtotalInput = _$saleCalculationsForm.find('input[name="SubTotal"]');
        const subtotal = parseFloat($subtotalInput.val()) || 0;

        const discountType = _$saleCalculationsForm.find('select[name="DiscountType"]').val();
        const $subtotalAfterDiscount = _$saleCalculationsForm.find('input[name="SubtotalAfterDiscount"]');

        const $taxInput = _$saleCalculationsForm.find('select[name="TaxID"]');
        const taxPercentage = parseFloat($taxInput.find('option:selected').data('percentage')) || 0;

        const $totalAfterTax = _$saleCalculationsForm.find('input[name="SubtotalAfterTax"]');
        const $RemainingAmount = _$saleCalculationsForm.find('input[name="RemainingAmount"]');

        let isValid = true;
        let errorMessage = '';
        if (discountType === 'percentage') {
            if (discountValue < 0 || discountValue > 100) {
                isValid = false;
                errorMessage = 'Percentage discount must be between 0 and 100.';
            }
        } else if (discountType === 'value') {
            if (discountValue < 0 || discountValue > subtotal) {
                isValid = false;
                errorMessage = 'Value discount cannot exceed the subtotal or be negative.';
            }
        }

        if (!isValid) {
            $discountInput.addClass('is-invalid').removeClass('is-valid');
            $discountInput.next('.error-message').remove();
            $discountInput.after(`<span class="error-message text-danger">${errorMessage}</span>`);
        } else {
            $discountInput.addClass('is-valid').removeClass('is-invalid');
            $discountInput.next('.error-message').remove();
        }

        if (!isValid) return;

        const subtotalAfterDiscount =
            discountType === 'percentage'
                ? subtotal - (subtotal * discountValue) / 100
                : subtotal - discountValue;
        $subtotalAfterDiscount.val(subtotalAfterDiscount.toFixed(2));

        const totalAfterTax = subtotalAfterDiscount + (subtotalAfterDiscount * taxPercentage) / 100;
        $totalAfterTax.val(totalAfterTax.toFixed(2));
        $RemainingAmount.val(totalAfterTax.toFixed(2));
    }

    // Payment Calculations

    _$paymentItemsTable.on('keyup', 'input[name="PaymentAmount"]', function () {
        totalPaidAmount();
    })

    function totalPaidAmount() {
        let subTotal = parseFloat(_$saleCalculationsForm.find('input[name="SubtotalAfterTax"]').val()) || 0;
        let total = 0;

        _$paymentItemsTable.find('input[name="PaymentAmount"]').each(function () {
            const value = parseFloat($(this).val()) || 0;
            total += value;
        });

        if (total > subTotal) {
            const returnAmount = total - subTotal;
            _$saleCalculationsForm.find('input[name="ReturnAmount"]').val(returnAmount.toFixed(2));
            _$saleCalculationsForm.find('input[name="PaidAmount"]').val(subTotal.toFixed(2));
            _$saleCalculationsForm.find('input[name="RemainingAmount"]').val('0.00');
        } else {
            const remainingAmount = subTotal - total;
            _$saleCalculationsForm.find('input[name="ReturnAmount"]').val('0.00');
            _$saleCalculationsForm.find('input[name="PaidAmount"]').val(total.toFixed(2));
            _$saleCalculationsForm.find('input[name="RemainingAmount"]').val(remainingAmount.toFixed(2));
        }
    }

    function viewFile(path) {
        var regex = new RegExp("(.*?)\.(docx|doc|pdf)$");
        if (!(regex.test(path.toLowerCase()))) {
            var imobj = { 'src': path, 'title': 'Sale' };
            var photos = [];
            photos.push(imobj);
            var options = {
                draggable: true,
                resizable: true,
                movable: true,
                keyboard: true,
                title: true,
                footToolbar: ['zoomIn', 'zoomOut', 'fullscreen', 'actualSize', 'rotateRight'],
                zIndex: 999999999991090,
            };
            new PhotoViewer(photos, options);
        }
    }

    function formatDateToISO(dateString) {
        const date = new Date(dateString);
        const yyyy = date.getFullYear();
        const MM = String(date.getMonth() + 1).padStart(2, '0');
        const dd = String(date.getDate()).padStart(2, '0');
        const HH = String(date.getHours()).padStart(2, '0');
        const mm = String(date.getMinutes()).padStart(2, '0');
        const ss = String(date.getSeconds()).padStart(2, '0');

        return `${yyyy}-${MM}-${dd} ${HH}:${mm}:${ss}`;
    }

    function formatDateForUpdate(dateString) {
        const timestamp = Date.parse(dateString);
        const date = new Date(timestamp);
        const day = ('0' + date.getDate()).slice(-2);
        const month = ('0' + (date.getMonth() + 1)).slice(-2);
        const year = date.getFullYear();
        return `${month}/${day}/${year}`;
    }

    // #endregion
})(jQuery)