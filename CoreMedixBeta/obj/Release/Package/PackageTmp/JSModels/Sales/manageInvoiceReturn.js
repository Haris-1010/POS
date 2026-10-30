(function ($) {
    const _$saleItemsTable = $('#saleItemsTable'),
        _$paymentItemsTable = $('#paymentItemsTable'),
        _$saleInfoForm = $('#saleInfoForm');

    const _$saleCalculationsForm = $('#saleCalculationsForm');
    const _$addPaymentBtn = $('#addPaymentBtn'),
        _$returnPaymentBtn = $('#returnPaymentBtn');

    const _$saveInvoiceBtn = $('#saveInvoiceBtn'),
        _$saveAndPrintInvoiceBtn = $('#saveAndPrintInvoiceBtn');

    const permissionsForm = $('#permissionsForm').serializeObject();

    $(function () {
        $('[data-bs-toggle="tooltip"]').tooltip();
        $.validator.addMethod("checkPaymentLimit", function (value, element) {
            let totalExistingAmount = 0;

            $(".js-payment-amount").each(function () {
                if (this !== element) {
                    totalExistingAmount += parseFloat($(this).val()) || 0;
                }
            });

            let enteredAmount = parseFloat(value) || 0;
            return enteredAmount + totalExistingAmount <= totalExistingAmount;
        }, "Total return amount cannot exceed the already paid amount.");

        $("#paymentForm").validate({
            rules: {
                PaymentAmount: {
                    required: true,
                    number: true,
                    min: 0,
                    checkPaymentLimit: true
                }
            },
            messages: {
                PaymentAmount: {
                    required: "Please enter an amount.",
                    number: "Please enter a valid number.",
                    min: "Amount cannot be negative."
                }
            }
        });
        setUpFieldsInCaseOfUpdate();
    })

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
            totalPaidAmount(true);
        });

        _$saleItemsTable.find("tbody").find(".js-remove-item-btn").off("click").on("click", function () {
            $(this).closest("tr").remove();
            calculateSaleItemsTotal();
            totalPaidAmount(true);
        });

        _$saveInvoiceBtn.prop("disabled", false);
    }

    // region Save Invoices
    _$saveInvoiceBtn.on("click", function () {
        if (!_$saleInfoForm.valid()) {
            return false;
        }
        if (!validateReturnQty()) {
            toastr.error("Please enter return Quantity.", "Validation Error");
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
            _$thisBtn.text('Save Invoice').prop("disabled", false);

            if (status) {
                toastr.success(message, "", { timeOut: 500 });
                window.location.href = saleInvoicesUrl;
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        }).fail(function () {
            _$thisBtn.text('Save Invoice').prop("disabled", false);
            toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
        });
    });

    _$saveAndPrintInvoiceBtn.on("click", function () {
        if (!_$saleInfoForm.valid()) {
            return false;
        }
        if (!validateReturnQty()) {
            toastr.error("Please enter return Quantity.", "Validation Error");
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
                window.open(`${printSaleInvoiceThermalUrl}?ID=${responseID}`, '_blank');
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
            _$thisBtn.text('Save & Print').prop("disabled", false);
            toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
        });
    });

    function readInvoiceItems() {
        let itemsArray = [];
        _$saleItemsTable.find('tbody tr').each(function () {
            const row = $(this);
            const obj = {
                ItemID: parseInt(row.find('input[name="ItemID"]').val()),
                VariationID: parseInt(row.find('input[name="VariationID"]').val()),
                Name: row.find('td:eq(0)').text(),
                Qty: parseFloat(row.find('input[name="Qty"]').val()),
                UnitCost: parseFloat(row.find('td:eq(4)').text()),
                DiscountID: row.find('input[name="DiscountID"]').val(),
                DiscountType: row.find('input[name="DiscountType"]').val(),
                Discount: parseFloat(row.find('input[name="Discount"]').val()),
                Tax: parseFloat(row.find('input[name="ItemTax"]').val()) || 0.00,
                UnitSalePriceInclusiveTax: parseFloat(row.find('input[name="UnitCostWithTax"]').val()),
                LineTotal: parseFloat(row.find('input[name="LineTotal"]').val()),
                TransactionStatus: row.find('input[name="TransactionStatus"]').val(),
                RetQty: row.find('input[name="RetQty"]').val()
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
                TransactionType: row.find('input[name="TransactionType"]').val() || 0,
                PaymentMode: row.find('select[name="PaymentMode"]').val(),
                AttachmentURL: row.find('input[name="AttachmentURL"]').val() || "",
            };

            itemsArray.push(obj);
        });

        return itemsArray;
    }

    $(document).on("change", 'input[name="Attachment"]', function () {
        if (this.files.length > 0) {
            const file = this.files[0];
            const reader = new FileReader();
            reader.onload = (e) => {
                $(this).closest("tr").find('input[name="AttachmentURL"]').val(e.target.result.split(',')[1]);
            };
            reader.readAsDataURL(file);
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
    function adjustDiscountOnReturn($row) {
        let saleQty = parseFloat($row.find("input[name='SaleQty']").val()) || 0;
        let finalQty = parseFloat($row.find("input[name='Qty']").val()) || 0;
        let discountType = $("#InvoiceDiscountType").val();
        let discountAmountBox = $("input[name='DiscountAmount']");
        let originalDiscount = parseFloat($("#OriginalDiscountAmount").val()) || 0;
        if (saleQty > 0) {
            if (discountType === "value") {
                let proportionalDiscount = (originalDiscount / saleQty) * finalQty;
                discountAmountBox.val(proportionalDiscount.toFixed(2));
            }
        }
    }
    _$saleItemsTable.on('change', 'input[name="ReturnQty"]', function () {
        var $row = $(this).closest("tr");
        var qty = parseFloat($row.find("input[name='SaleQty']").val()) || 0;
        var returnQty = parseFloat($(this).val()) || 0;

        if (returnQty > 0) {
            $row.find("input[name='RetQty']").val(returnQty);
        } else {
            $row.find("input[name='RetQty']").val(0);
        }

        if (returnQty > qty && returnQty > 0) {
            $(this).addClass("is-invalid");
            if (!$row.find(".error-message").length) {
                $(this).after('<div class="text-danger error-message">Return quantity cannot exceed available quantity.</div>');
            }
        } else {
            $(this).removeClass("is-invalid");
            $row.find(".error-message").remove();
            $row.find("input[name='Qty']").val(qty - returnQty);
            adjustDiscountOnReturn($row);
            validateDiscount($row);
            calculateUnitCostWithTax($row);
            calculateSaleItemsTotal();
        }
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
        totalPaidAmount(true);
        if (total > 0) {
            _$addPaymentBtn.removeClass('disabled').prop('disabled', false);
            _$saveInvoiceBtn.removeClass('disabled').prop('disabled', false);
            _$saveAndPrintInvoiceBtn.removeClass('disabled').prop('disabled', false);
        } else {
            _$addPaymentBtn.addClass('disabled').prop('disabled', true);
            _$saveInvoiceBtn.removeClass('disabled').prop('disabled', false);
            _$saveAndPrintInvoiceBtn.removeClass('disabled').prop('disabled', false);
        }
    }

    function validateDiscount($row) {
        const $discountInput = $row.find('input[name="Discount"]');
        const discountValue = parseFloat($discountInput.val()) || 0;
        const unitCost = parseFloat($row.find('td:eq(4)').text()) || 0;
        const quantity = parseInt($row.find('input[name="Qty"]').val()) || 0;
        const discountType = $row.find('.input[name="DiscountType"]').val();
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
            quantity = parseInt($row.find('input[name="Qty"]').val()) || 0;
            costPrice = parseFloat($row.find('td:eq(4)').text()) || 0;
            discountType = $row.find('.input[name="DiscountType"]').val();
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
            if (quantity > 1) {
                let discountPerItem = discountValue / quantity;
                finalCost -= discountPerItem;
            } else {
                finalCost -= discountValue;
            }
        } else {
            finalCost = costPrice;
        }

        finalCost = Math.max(0, finalCost);

        const unitCostWithTax = finalCost > 0
            ? finalCost + (finalCost * taxValue) / 100
            : costPrice + (costPrice * taxValue) / 100;

        $row.find('input[name="UnitCostWithTax"]').val(unitCostWithTax.toFixed(2));
        calculateLineTotal($row);

        return {
            unitCostWithTax,
        };
    }

    function calculateLineTotal($row) {
        const quantity = parseInt($row.find('input[name="Qty"]').val()) || 0;
        const costWithTax = parseFloat($row.find('input[name="UnitCostWithTax"]').val()) || 0;
        const lineTotal = quantity * costWithTax;
        $row.find('input[name="LineTotal"]').val(lineTotal.toFixed(2));
    }

    _$saleCalculationsForm.find('select[name="DiscountID"], select[name="DiscountType"], input[name="DiscountAmount"], select[name="TaxID"]').on('input change', function () {
        calculateTotals();
        totalPaidAmount(true);
    });

    function calculateTotals() {
        const $discountInput = _$saleCalculationsForm.find('input[name="DiscountAmount"]');
        const discountValue = parseFloat($discountInput.val()) || 0;

        const $subtotalInput = _$saleCalculationsForm.find('input[name="SubTotal"]');
        const subtotal = parseFloat($subtotalInput.val()) || 0;

        const discountType = _$saleCalculationsForm.find('input[name="DiscountType"]').val();
        const $subtotalAfterDiscount = _$saleCalculationsForm.find('input[name="SubtotalAfterDiscount"]');

        const $taxInput = _$saleCalculationsForm.find('input[name="TaxID"]');
        const taxPercentage = parseFloat(_$saleCalculationsForm.find('input[name="TaxPercentage"]').val()) || 0;

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
        totalPaidAmount(false);
    })

    function totalPaidAmount(isReturn = true) {
        let subTotal = parseFloat(_$saleCalculationsForm.find('input[name="SubtotalAfterTax"]').val()) || 0;
        let total = 0;

        _$paymentItemsTable.find('input[name="PaymentAmount"]').each(function () {
            const value = parseFloat($(this).val()) || 0;
            total += value;
        });

        if (total > subTotal) {
            const returnAmount = total - subTotal;
            if (isReturn) {
                _$saleCalculationsForm.find('input[name="ReturnAmount"]').val(returnAmount.toFixed(2));
            }
            _$saleCalculationsForm.find('input[name="PaidAmount"]').val(subTotal.toFixed(2));
            _$saleCalculationsForm.find('input[name="RemainingAmount"]').val('0.00');
        } else {
            const remainingAmount = subTotal - total;
            _$saleCalculationsForm.find('input[name="ReturnAmount"]').val('0.00');
            _$saleCalculationsForm.find('input[name="PaidAmount"]').val(total.toFixed(2));
            _$saleCalculationsForm.find('input[name="RemainingAmount"]').val(remainingAmount.toFixed(2));
        }
    }

    // Add Payment Item to Payments Table
    _$returnPaymentBtn.on('click', function () {
        const tableBodyEl = _$paymentItemsTable.find('tbody');
        const HTML = `<tr>
                           <td>
                               <input type="text" class="form-control js-datepicker" name="Date" placeholder="Enter date" />
                           </td>
                           <td>
                               <input type="text" class="form-control" name="TransactionType" value="Return" readonly/>
                           </td>
                           <td>
                               <input type="number" class="form-control js-payment-amount" name="PaymentAmount" placeholder="Enter amount" value="0" min="0" />
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
        tableBodyEl.find(".js-remove-payment-btn").off("click").on("click", function () {
            $(this).closest("tr").remove();
            validateReturnAmount();
            totalPaidAmount(true);
        });
        tableBodyEl.find(".js-payment-amount").off("input").on("input", function () {
            validateReturnAmount();
        });
    });
    //find(".js-payment-amount").off("input").on("input", function () {
    ////    validateReturnAmount();
    ////});
    function validateReturnAmount() {
        let totalPaidAmount = 0;
        _$paymentItemsTable.find("input[name='PaymentAmount']")
            .not(".js-payment-amount")
            .filter(function () {
                return $(this).closest("tr").find("input[name='TransactionType']").val() !== "Return";
            })
            .each(function () {
                totalPaidAmount += parseFloat($(this).val()) || 0;
            });

        const newReturnAmount = parseFloat($(".js-payment-amount").last().val()) || 0;

        if (newReturnAmount > totalPaidAmount) {
            $(".js-payment-amount").last().val(totalPaidAmount.toFixed(2));
            toastr.error("Total return amount cannot exceed the total paid amount.", "Invalid Amount");
        } else {
        }
    }

    function validateReturnQty() {
        let isValid = true;

        _$saleItemsTable.find("tr").each(function () {
            const _$returnQty = parseFloat(_$saleItemsTable.find("input[name='ReturnQty']").val()) || 0;
            const _$qty = parseFloat(_$saleItemsTable.find("input[name='SaleQty']").val()) || 0;

            if (_$returnQty >= 0 && _$returnQty <= _$qty) {
                $(this).find("input[name='ReturnQty']").removeClass("error");
            } else {
                $(this).find("input[name='ReturnQty']").addClass("error");
                isValid = false;
            }
        });
        return isValid;
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