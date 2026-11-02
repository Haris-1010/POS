(function ($) {
    const _$saleInfoForm = $('#saleInfoForm'),
        _$customerSearchElement = $('.js-search-customer'),
        _$saveCustomerModal = $('#saveCustomerModal'),
        _$saveCustomerForm = _$saveCustomerModal.find('form');

    $(function () {
        _$saveCustomerModal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$saveCustomerForm.trigger('reset');
            _$saveCustomerForm.find('input[name="ID"]').val('0');
        }); 

        _$saveCustomerForm.validate({
            rules: {
                Name: {
                    required: true
                },
                Phone: {
                    required: false,
                    digits: true,
                    minlength: 10,
                    maxlength: 15
                }
            },
            messages: {
                Name: {
                    required: "Patient name is required"
                },
                Phone: {
                    digits: "Phone must contain only numbers",
                    minlength: "Phone must be at least 10 digits",
                    maxlength: "Phone cannot exceed 15 digits"
                }
            },
            errorElement: "span",
            errorPlacement: function (error, element) {
                error.addClass("text-danger");
                error.insertAfter(element);
            }
        });

        $.validator.addMethod("choosefindpatient", function (value, element) {
            return value.trim() !== "0" && value.trim() !== "";
        }, "Please Select a Customer");

        $.validator.addMethod("chooseSalePerson", function (value, element) {
            return value !== "0";
        }, "Please choose a Business Location");

        _$saleInfoForm.validate({
            rules: {
                SearchCustomer: {
                    choosefindpatient: true
                },
                SalePerson: {
                    chooseSalePerson: true
                }
            },
            messages: {
                SearchCustomer: {
                    choosefindpatient: "Please Select a Customer"
                },
                SalePerson: {
                    chooseSalePerson: "Please choose a SalePerson"
                }
            }
        });

        //_$saveCustomerModal.on('click', '.js-save-btn', async function () {
        //    const _$saveBtn = $(this);
        //    if (!_$saveCustomerForm.valid()) {
        //        return;
        //    }
        //    _$saveBtn.text('Saving...').prop('disabled', true);

        //    let data = _$saveCustomerForm.serializeObject();

        //    const url = savePatientUrl;
        //    try {
        //        const resp = await $.post(url, data);
        //        const { Status: status, Message: message, Data: responseData } = resp

        //        if (status) {
        //            toastr.success(message, "", { timeOut: 500 });
        //            _$saleInfoForm.find('input[name="CustomerID"]').val(responseData);
        //            _$saleInfoForm.find('input[name="CustomerType"]').val("Customer");
        //            _$saleInfoForm.find('input[name="SearchCustomer"]').val(data.Name);
        //            _$saveCustomerModal.modal('hide');
        //        } else {
        //            toastr.error(message, "", { timeOut: 500 });
        //        }
        //    } catch (error) {
        //        toastr.error("An error occurred while saving the customer.", "", { timeOut: 500 });
        //    } finally {
        //        _$saveBtn.text('Save').prop('disabled', false);
        //    }
        //});

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

                        results.unshift({
                            label: "Add New Patient",
                            value: "Add New Patient",
                            data: null
                        });

                        response(results);
                    }
                });
            },
            minLength: 2,
            select: function (event, ui) {
                if (ui.item.data === null) {
                    event.preventDefault();
                    _$saveCustomerModal.modal("show");
                } else {
                    let itemData = ui.item.data;
                    _$saleInfoForm.find('input[name="CustomerID"]').val(itemData.CustomerID);
                    _$saleInfoForm.find('input[name="CustomerType"]').val(itemData.CustomerType);
                    _$customerSearchElement.val(itemData.CustomerName);

                    // 🔹 Check if this patient already has invoices 

                    $.post(getLastInvoiceUrl, { customerId: itemData.CustomerID }, function (resp) {
                        if (resp.Status && resp.Data) {
                            let invoiceDate = new Date(resp.Data.InvoiceDate);
                            let options = { day: 'numeric', month: 'long', year: 'numeric' };
                            let formattedDate = invoiceDate.toLocaleDateString('en-US', options);

                            let formattedInvoiceNo = `${resp.Data.InvoiceNo}`;

                            // Insert alert message
                            $('#invoiceAlertBody').html(
                                `This patient already has an invoice on <b>${formattedDate}</b><br>(Invoice # ${formattedInvoiceNo}).`
                            );

                            $('#invoiceAlertFooter').html(`
            <button type="button" class="btn btn-secondary" id="invoiceAlertOkBtn">OK</button>
            <button type="button" class="btn btn-primary" id="checkInvoiceBtn">Check Invoice</button>
        `);

                            $('#invoiceAlertModal').modal('show');

                            $('#invoiceAlertOkBtn').off('click').on('click', function () {
                                $('#invoiceAlertModal').modal('hide');
                            });

                            $('#checkInvoiceBtn').off('click').on('click', function () {
                                console.log("checkInvoiceBtn clicked, CustomerID:", itemData.CustomerID);

                                // ✅ Redirect instead of Ajax
                                window.location.href = `${saleInvoicesUrl}?CustomerID=${itemData.CustomerID}`;
                            });
                        }
                    });



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
    })
})(jQuery)