(function ($) {
    const _$purchaseOrderForm = $('#purchaseOrderForm'),
        _$purchaseItemsTable = $('#purchaseItemsTable'),
        _$purchaseCalculationsForm = $('#purchaseCalculationsForm'),
        _$paymentItemsTable = $('#paymentItemsTable');

    const _$addPaymentBtn = $('#addPaymentBtn');

    const _$savePurchaseBtn = $('#savePurchaseBtn'),
        _$saveAndPrintPurchaseBtn = $('#saveAndPrintPurchaseBtn');

    const _$addVariationModal = $('#addVariationModal'),
        _$addVariationForm = _$addVariationModal.find('form');

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

        _$addVariationModal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$addVariationForm.trigger('reset');
            _$addVariationForm.find('input[name="ID"]').val('0');
            _$addVariationForm.find('input[name="ExpiryDate"]').datepicker('setDate', new Date());
            _$addVariationModal.find('.js-modal-title').text("Add Variation");
        });

        setUpFieldsInCaseOfUpdate();
        calculateItemsLineTotal();
    })

    function setUpFieldsInCaseOfUpdate() {
        const purchaseOrderID = parseInt(_$purchaseOrderForm.find('input[name="ID"]').val()) || 0;
        if (purchaseOrderID <= 0) {
            return;
        }

        let purchaseDate = _$purchaseOrderForm.find('input[name="PurchaseDateInCaseOfUpdate"]').val();
        if (purchaseDate) {
            _$purchaseOrderForm.find('input[name="PurchaseReceivedAt"]').datepicker('setDate', formatDateForUpdate(purchaseDate));
        }

        _$paymentItemsTable.find('tbody tr').each(function () {
            const row = $(this);
            let oldDate = row.find('input[name="Date"]').val();
            row.find('input[name="Date"]').datepicker('setDate', formatDateForUpdate(oldDate));
        });

        calculateItemsLineTotal();
    }

    _$purchaseItemsTable.on('change', 'input[name="LineTotal"]', function () {
        calculateItemsLineTotal()
    });

    _$purchaseItemsTable.on('keyup', 'input[name="Qty"]', function () {
        const $row = $(this).closest('tr');
        calculateUnitCostBeforeAndAfterTax2($row);
        calculateItemsLineTotal();
        calculateQtyTotal($row);
    });
    _$purchaseItemsTable.on('keyup', 'input[name="BonusQty"]', function () {
        const $row = $(this).closest('tr');
        calculateQtyTotal($row);
    });

    //Update Cost prices & Expiry dates of item variations during purchase items
    _$purchaseItemsTable.on('click', '.js-edit', async function () {
        let Id = $(this).data('id');
        const url = GetVariationByIDUrl;
        const data = {
            ID: Id,
            '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        };

        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: response } = resp;

            if (status) {
                // Assign values for all inputs (optional for hidden ones)
                _$addVariationForm.find('input[name="ID"]').val(response.ID);
                _$addVariationForm.find('input[name="ItemID"]').val(response.ItemID);
                _$addVariationForm.find('input[name="VariationName"]').val(response.VariationName);
                _$addVariationForm.find('input[name="UPC"]').val(response.UPC);
                _$addVariationForm.find('input[name="SKU"]').val(response.SKU);
                _$addVariationForm.find('input[name="OpeningStock"]').val(response.OpeningStock);
                _$addVariationForm.find('input[name="OpeningStockRate"]').val(response.OpeningStockRate);
                _$addVariationForm.find('input[name="NoOfPillsInPack"]').val(response.NoOfPillsInPack);
                _$addVariationForm.find('input[name="SellingPrice"]').val(response.SellingPrice);
                _$addVariationForm.find('input[name="CostPrice"]').val(response.CostPrice);
                _$addVariationForm.find('input[name="ReorderPoint"]').val(response.ReorderPoint);
                _$addVariationForm.find('input[name="ExpiryDate"]').datepicker('setDate', formatDate(response.ExpiryDate, true));
                _$addVariationForm.find('input[name="Batch"]').val(response.Batch);
                _$addVariationForm.find('input[name="Rack"]').val(response.Rack);
                _$addVariationForm.find('input[name="Margin"]').val(response.Margin);
                _$addVariationForm.find('input[name="Tax"]').val(response.Tax);
                _$addVariationForm.find('input[name="SearchAlternate"]').val(response.SearchAlternate);

                // Assign dropdown values if needed (optional)
                _$addVariationForm.find('select[name="FormationID"]').val(response.FormationID);
                _$addVariationForm.find('select[name="WeightUnitID"]').val(response.WeightUnitID);
                _$addVariationForm.find('select[name="UnitID"]').val(response.UnitID);
                _$addVariationForm.find('select[name="CategoryID"]').val(response.CategoryID);
                _$addVariationForm.find('select[name="SubCategoryID"]').val(response.SubCategoryID);

                // Assign alternate items
                renderAlternateItemsTags(response.AlternateItems);

                // Show modal
                _$addVariationModal.modal("show");
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        } catch (error) {
            toastr.error("An error occurred", "", { timeOut: 500 });
        }
    });

    function renderAlternateItemsTags(tags) {
        const itemTags = tags?.trim();
        if (!itemTags) return;

        const tagsArray = itemTags.split(',').map(salt => salt.trim());

        _$alternateItemElement.select2({
            tags: true,
            createTag: params => ({ id: $.trim(params.term), text: $.trim(params.term), newTag: true })
        });

        _$alternateItemElement.append(tagsArray.map(tag => new Option(tag, tag, false, true))).trigger('change');
    }

    _$addVariationModal.on('click', '.js-save-btn', async function () {
        const _$saveBtn = $(this);
        if (!_$addVariationForm.valid()) return;

        $('button').prop('disabled', true);
        _$saveBtn.text('Saving...').prop('disabled', true);

        const url = SaveItemVariationUrl;
        const data = _$addVariationForm.serializeObject();

        // Handle AlternateItems select2 values
        let alternateItemValues = data.AlternateItems;

        if (typeof alternateItemValues === 'string') {
            alternateItemValues = [alternateItemValues];
        } else if (!Array.isArray(alternateItemValues)) {
            alternateItemValues = [];
        }
        data.AlternateItems = alternateItemValues.join(',');
        data.__RequestVerificationToken = $("input[name='__RequestVerificationToken']").val();

        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message } = resp;

            if (status) {
                toastr.success(message, "", { timeOut: 500 });
                _$addVariationModal.modal('hide');
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        } catch (error) {
            toastr.error("Failed to save Variation due to a technical error.", "", { timeOut: 500 });
        } finally {
            _$saveBtn.text('Save').prop('disabled', false);
            $('button').prop('disabled', false);
        }
    });

    function calculateItemsLineTotal() {
        let total = 0;
        _$purchaseItemsTable.find('input[name="LineTotal"]').each(function () {
            const value = parseFloat($(this).val()) || 0;
            total += value;
        });
        _$purchaseCalculationsForm.find('input[name="SubTotal"]').val(total.toFixed(2));
        _$purchaseCalculationsForm.find('input[name="SubtotalAfterDiscount"]').val(total.toFixed(2));
        _$purchaseCalculationsForm.find('input[name="SubtotalAfterTax"]').val(total.toFixed(2));
        _$purchaseCalculationsForm.find('input[name="RemainingAmount"]').val(total.toFixed(2));
        calculateTotals();
        totalPaidAmount();
        if (total > 0) {
            _$addPaymentBtn.removeClass('disabled').prop('disabled', false);
            _$savePurchaseBtn.removeClass('disabled').prop('disabled', false);
            _$saveAndPrintPurchaseBtn.removeClass('disabled').prop('disabled', false);
        } else {
            _$addPaymentBtn.addClass('disabled').prop('disabled', true);
            _$savePurchaseBtn.addClass('disabled').prop('disabled', true);
            _$saveAndPrintPurchaseBtn.addClass('disabled').prop('disabled', true);
        }
    }

    function calculateUnitCostBeforeAndAfterTax2($row) {
        if (!($row instanceof jQuery)) {
            $row = $($row);
        }

        var costPrice = 0;
        var taxValue = 0;

        if ($row.find('input[name="ItemTax"]').val() != undefined) {
            costPrice = parseFloat($row.find('input[name="UnitCost"]').val()) || 0;
            taxValue = parseFloat($row.find('input[name="ItemTax"]').val()) || 0;
        } else {
            costPrice = parseFloat($row[0].CostPrice) || 0;
            taxValue = parseFloat($row[0].Tax) || 0;
        }

        let unitCostBeforeTax = costPrice;
        unitCostBeforeTax = Math.max(0, unitCostBeforeTax);
        const unitCostWithTax = unitCostBeforeTax + (unitCostBeforeTax * taxValue) / 100;
        $row.find('input[name="UnitCostBefore"]').val(unitCostBeforeTax.toFixed(2));
        $row.find('input[name="UnitCostWithTax"]').val(unitCostWithTax.toFixed(2));

        calculateLineTotal($row);
        return {
            unitCostBeforeTax,
            unitCostWithTax,
        };
    }

    function calculateLineTotal($row) {
        const quantity = parseFloat($row.find('input[name="Qty"]').val()) || 0;
        const costWithTax = parseFloat($row.find('input[name="UnitCostWithTax"]').val()) || 0;
        const lineTotal = quantity * costWithTax;
        $row.find('input[name="LineTotal"]').val(lineTotal.toFixed(2));
    }
    function calculateQtyTotal($row) {
        const quantity = parseFloat($row.find('input[name="Qty"]').val()) || 0;
        const bonusQty = parseFloat($row.find('input[name="BonusQty"]').val()) || 0;
        const TotalQty = quantity + bonusQty;
        $row.find('input[name="TotalQty"]').val(TotalQty);
    }

    _$purchaseCalculationsForm.find('select[name="DiscountID"],select[name="DiscountType"], input[name="DiscountAmount"], select[name="TaxID"]').on('input change', function () {
        calculateTotals();
        totalPaidAmount();
    });

    function calculateTotals() {
        const $discountInput = _$purchaseCalculationsForm.find('input[name="DiscountAmount"]');
        const discountValue = parseFloat($discountInput.val()) || 0;

        const $subtotalInput = _$purchaseCalculationsForm.find('input[name="SubTotal"]');
        const subtotal = parseFloat($subtotalInput.val()) || 0;

        const discountType = _$purchaseCalculationsForm.find('select[name="DiscountType"]').val();
        const $subtotalAfterDiscount = _$purchaseCalculationsForm.find('input[name="SubtotalAfterDiscount"]');

        const $taxInput = _$purchaseCalculationsForm.find('select[name="TaxID"]');
        const taxPercentage = parseFloat($taxInput.find('option:selected').data('percentage')) || 0;

        const $totalAfterTax = _$purchaseCalculationsForm.find('input[name="SubtotalAfterTax"]');
        const $RemainingAmount = _$purchaseCalculationsForm.find('input[name="RemainingAmount"]');

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

    _$addPaymentBtn.on('click', function () {
        const totalSubTotalAfterTax = parseFloat(_$purchaseCalculationsForm.find('input[name="SubtotalAfterTax"]').val()) || 0;
        const paymentTableBodyEl = _$paymentItemsTable.find('tbody');
        const HTML = `<tr>
                      <td>
                          <input type="text" class="form-control js-datepicker" name="Date" placeholder="Enter date" />
                      </td>
                      <td>
                          <input type="number" class="form-control" name="PaidAmount" placeholder="Enter amount" value="${totalSubTotalAfterTax.toFixed("2")}" min="0" />
                      </td>
                      <td>
                        <select class="form-control  js-select2" name="PaymentMode">
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
                         <button class="btn btn-danger btn-sm tip js-delete mt-1" title="Delete Payment" type="button">
                            <i class="fa fa-trash"></i>
                        </button>
                      </td>
                      </tr>`;
        paymentTableBodyEl.append(HTML);
        paymentTableBodyEl.find('tr:last-child select.js-select2').select2({ width: '99%' });
        paymentTableBodyEl.find('tr:last-child input.js-datepicker').datepicker('setDate', new Date());
        totalPaidAmount();
    })

    _$paymentItemsTable.on('click', '.js-delete', function () {
        $(this).closest('tr').remove();
        calculateTotals();
        totalPaidAmount();
    });

    _$purchaseItemsTable.on('click', '.js-remove-item-btn', function () {
        $(this).closest('tr').remove();
        calculateItemsLineTotal();
        totalPaidAmount();
    });

    _$paymentItemsTable.on('keyup', 'input[name="PaidAmount"]', function () {
        totalPaidAmount();
    });

    function totalPaidAmount() {
        let subTotal = _$purchaseCalculationsForm.find('input[name="SubtotalAfterTax"]').val();
        let total = 0;
        _$paymentItemsTable.find('input[name="PaidAmount"]').each(function () {
            const value = parseFloat($(this).val()) || 0;
            total += value;
        });

        let isValid = true;
        let errorMessage = '';
        if (total > subTotal) {
            isValid = false;
            errorMessage = 'Paid Amount cannot exceed the Subtotal.';
        }
        else {
            _$purchaseCalculationsForm.find('input[name="PaidAmount"]').val(total.toFixed(2));
            total = subTotal - total;
            _$purchaseCalculationsForm.find('input[name="RemainingAmount"]').val(total.toFixed(2));
        }

        if (!isValid) {
            _$purchaseCalculationsForm.find('input[name="PaidAmount"]').addClass('is-invalid').removeClass('is-valid');
            _$purchaseCalculationsForm.find('input[name="PaidAmount"]').next('.error-message').remove();
            _$purchaseCalculationsForm.find('input[name="PaidAmount"]').after(`<span class="error-message text-danger">${errorMessage}</span>`);
        } else {
            _$purchaseCalculationsForm.find('input[name="PaidAmount"]').addClass('is-valid').removeClass('is-invalid');
            _$purchaseCalculationsForm.find('input[name="PaidAmount"]').next('.error-message').remove();
            _$purchaseCalculationsForm.find('input[name="PaidAmount"]').after(`<span class="error-message text-danger">${errorMessage}</span>`);
        }
        return isValid;
    }

    _$savePurchaseBtn.on('click', async function () {
        const _$saveBtn = $(this);
        if (!_$purchaseOrderForm.valid()) {
            return;
        }
        _$saveBtn.text('Saving...').prop('disabled', true);
        const url = SavePurchaseUrl;

        let purchaseDetails = _$purchaseOrderForm.serializeObject();
        let purchasePaymentDetails = _$purchaseCalculationsForm.serializeObject();
        Object.assign(purchaseDetails, purchasePaymentDetails);

        if (purchasePaymentDetails.ShippingDetails) {
            purchasePaymentDetails.ShippingDetails = purchasePaymentDetails.ShippingDetails.trim();
        }
        if (purchasePaymentDetails.AdditionalNotes) {
            purchasePaymentDetails.AdditionalNotes = purchasePaymentDetails.AdditionalNotes.trim();
        }

        let data = {
            Purchase: purchaseDetails,
            PurchaseItems: readPurchaseItems(),
            PurchasePayments: readPaymentItems(),
            '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message } = resp;
            if (status) {
                bootbox.dialog({
                    title: 'Invoice Saved!',
                    message: message,
                    closeButton: false, // Hides the close (×) button
                    buttons: {
                        cancel: {
                            label: '<i class="fa fa-ok"></i> Ok',
                            className: 'btn-primary',
                            callback: function () {
                                window.location.href = PurchaseOrdersUrl;
                            }
                        },
                    }
                });
                //toastr.success(message, "", { timeOut: 1000 });
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        } catch (error) {
            toastr.error("An error occurred while saving the purchase.", "", { timeOut: 500 });
        } finally {
            _$saveBtn.text('Save Purchase').prop('disabled', false);
        }
    });

    _$saveAndPrintPurchaseBtn.on('click', async function () {
        const _$saveBtn = $(this);
        if (!_$purchaseOrderForm.valid()) {
            return;
        }
        _$saveBtn.text('Saving...').prop('disabled', true);
        const url = SavePurchaseUrl;

        let purchaseDetails = _$purchaseOrderForm.serializeObject();
        let purchasePaymentDetails = _$purchaseCalculationsForm.serializeObject();
        Object.assign(purchaseDetails, purchasePaymentDetails);

        let data = {
            Purchase: purchaseDetails,
            PurchaseItems: readPurchaseItems(),
            PurchasePayments: readPaymentItems(),
            '__RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: responseID } = resp;
            if (status) {
                window.open(`${printPurchaseUrl}?ID=${responseID}`, '_blank'); 
                bootbox.dialog({
                    title: 'Invoice Saved!',
                    message: message,
                    closeButton: false, // Hides the close (×) button
                    buttons: {
                        cancel: {
                            label: '<i class="fa fa-ok"></i> Ok',
                            className: 'btn-primary',
                            callback: function () {
                                window.location.href = PurchaseOrdersUrl;
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
        return _$purchaseItemsTable.find('tbody tr').map(function () {
            const row = $(this);
            return {
                Name: row.find('td:eq(0)').text(),
                Qty: +row.find('input[name="Qty"]').val() || 0,
                BonusQty: +row.find('input[name="BonusQty"]').val() || 0,
                ItemID: +row.find('input[name="ItemID"]').val() || 0,
                VariationID: +row.find('input[name="VariationID"]').val() || 0,
                UnitCost: +row.find('input[name="UnitCost"]').val() || 0.00,
                ItemTax: +row.find('input[name="ItemTax"]').val() || 0.00,
                UnitCostBeforeTax: +row.find('input[name="UnitCostBefore"]').val() || 0.00,
                UnitCostPriceInclusiveTax: +row.find('input[name="UnitCostWithTax"]').val() || 0.00,
                LineTotal: +row.find('input[name="LineTotal"]').val() || 0.00
            };
        }).get();
    }

    function readPaymentItems() {
        return _$paymentItemsTable.find('tbody tr').map(function () {
            return {
                Date: formatDateToISO($(this).find('input[name="Date"]').val()),
                PaidAmount: +$(this).find('input[name="PaidAmount"]').val() || 0.00,
                PaymentMode: $(this).find('select[name="PaymentMode"]').val(),
                AttachmentURL: $(this).find('input[name="AttachmentURL"]').val() || "",
            };
        }).get();
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

    function viewFile(path) {
        var regex = new RegExp("(.*?)\.(docx|doc|pdf)$");
        if (!(regex.test(path.toLowerCase()))) {
            var imobj = { 'src': path, 'title': 'Purchase Attachment' };
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

    function formatDate(dateString, forDatePicker = false) {
        if (forDatePicker) {
            const timestamp = parseInt(dateString.replace(/\/Date\((\d+)\)\//, '$1'), 10);
            const date = new Date(timestamp);
            const day = ('0' + date.getDate()).slice(-2);
            const month = ('0' + (date.getMonth() + 1)).slice(-2);
            const year = date.getFullYear();
            return `${month}/${day}/${year}`;
        } else {
            const date = new Date(dateString);
            const options = { day: '2-digit', month: 'short', year: 'numeric' };
            return date.toLocaleDateString('en-US', options);
        }
    }
})(jQuery)