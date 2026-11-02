(function ($) {
    const _$itemInfoForm = $('#itemInfoForm'),
        _$manufacturerSearchElement = $('.js-search-manufacturer'),
        _$saveManufacturerModal = $('#saveManufacturerModal'),
        _$saveManufacturerForm = _$saveManufacturerModal.find('form'),

        _$supplierSearchElement = $('.js-search-supplier'),
        _$saveSupplierModal = $('#saveSupplierModal'),
        _$saveSupplierForm = _$saveSupplierModal.find('form');
         

     

    $(function () {
        $('[data-bs-toggle="tooltip"]').tooltip();
        _$itemInfoForm.validate({
            ignore: [],  
            rules: {
                ManufacturerID: { required: true },
                SupplierName: { required: true } 
                 
            },
            messages: {
                ManufacturerName: "This field is required",
                SupplierName: "This field is required" 
            }, 
            errorPlacement: function (error, element) {
                if (element.attr("name") === "SupplierID") {
                    error.insertAfter($(".js-search-supplier")); // supplier textbox ke niche
                }
                else if (element.attr("name") === "ManufacturerID") {
                    error.insertAfter($(".js-search-manufacturer")); // manufacturer textbox ke niche
                }
                else if (element.hasClass("js-select2")) {
                    error.insertAfter(element.next('.select2'));
                }
                else {
                    error.insertAfter(element);
                }
            }
        });


      


        $('.js-form-city').select2({
            placeholder: 'Select City',
            width: '100%',
            dropdownParent: $('#saveManufacturerModal')
        });

        $('.js-form-citySup').select2({
            placeholder: 'Select City',
            width: '100%',
            dropdownParent: $('#saveSupplierModal') // Important line!
        });

        _$saveManufacturerForm.validate();
        _$saveManufacturerModal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$saveManufacturerForm.trigger('reset');
            _$saveManufacturerForm.find('input[name="ID"]').val('0');
        });

        _$saveManufacturerModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveManufacturerForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            const url = saveManufacturerUrl;
            const data = _$saveManufacturerForm.serializeObject(); 

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message, Data: id } = resp;
                if (status) {
                    _$itemInfoForm.find('input[name="ManufacturerID"]').val(id);
                    _$manufacturerSearchElement.val(data.Name);
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);

                    _$saveManufacturerModal.modal('hide');
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save manufacturer due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        //Munufactured search auto complete if not exist then create new manufacturer
        _$manufacturerSearchElement.autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: searchManufacturerUrl,
                    data: { SearchString: request.term },
                    type: "POST",
                    success: function (data) {
                        let results = $.map(data.Data, function (item) {
                            return {
                                label: item.Name,
                                value: item.Name,
                                data: item
                            };
                        });

                        results.unshift({
                            label: "Add New Manufacturer",
                            value: "Add New Manufacturer",
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
                    _$saveManufacturerModal.modal("show");
                } else {
                    let itemData = ui.item.data;
                    _$manufacturerSearchElement.val(itemData.Name);
                    _$itemInfoForm.find('input[name="ManufacturerID"]').val(itemData.ID);
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

        //Supllier Modal Starting ....
        _$saveSupplierModal.validate();
        _$saveSupplierModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop').remove();
            _$saveSupplierForm.trigger('reset');
            _$saveSupplierForm.find('input[name="ID"]').val('');
        });

        _$saveSupplierModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveSupplierForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);

            let formData = _$saveSupplierForm.serializeObject(); 

            const url = saveSupplierUrl;
            const data = formData;

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message, Data: id } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    _$itemInfoForm.find('input[name="SupplierID"]').val(id);
                    _$supplierSearchElement.val(data.Name);

                    _$saveSupplierModal.modal('hide');
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save supplier due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        //Suppliers search auto complete if not exist then create new supplier
        _$supplierSearchElement.autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: searchSupplierUrl,
                    data: { SearchString: request.term },
                    type: "POST",
                    success: function (data) {
                        let results = $.map(data.Data, function (item) {
                            return {
                                label: item.Name,
                                value: item.Name,
                                data: item
                            };
                        });

                        results.unshift({
                            label: "Add New Supplier",
                            value: "Add New Supplier",
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
                    _$saveSupplierModal.modal("show");
                } else {
                    let itemData = ui.item.data;
                    _$supplierSearchElement.val(itemData.Name);
                    _$itemInfoForm.find('input[name="SupplierID"]').val(itemData.ID);
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
        _$itemInfoForm.find('select[name="MedicineSalt"]').select2({
            placeholder: "Add medicine salts",
            tags: true,
            createTag: function (params) {
                var term = $.trim(params.term);
                if (term === '') {
                    return null;
                }
                return {
                    id: term,
                    text: term,
                    newTag: true
                };
            }
        });

        /* IN CASE OF EDIT POPULATE THE SALTS DD */
        renderSaltTags();
    });

    _$itemInfoForm.find('select[name="ProductType"]').on('change', function (e) {
        if ($(this).val() == 'Medicine') {
            $('.js-salt-input').removeAttr('hidden');
        }
        else {
            $('.js-salt-input').attr('hidden', true);
        }
    });

    _$itemInfoForm.on('click', '.js-save-btn', async function () {
        const _$saveBtn = $(this);
        if (!_$itemInfoForm.valid()) {
            return;
        }
        _$saveBtn.text('Saving...').prop('disabled', true);

        const url = saveInventoryItemrUrl;
        const data = _$itemInfoForm.serializeObject(); 

        let medicineSaltValues = data.MedicineSalt,
            SupplierIdsValues = data.SupplierID;

        if (typeof medicineSaltValues === 'string') {
            medicineSaltValues = [medicineSaltValues];
        } else if (!Array.isArray(medicineSaltValues)) {
            medicineSaltValues = [];
        }
        if (typeof SupplierIdsValues === 'string') {
            SupplierIdsValues = [SupplierIdsValues];
        } else if (!Array.isArray(SupplierIdsValues)) {
            SupplierIdsValues = [];
        }
        data.MedicineSalt = medicineSaltValues.join(',');
        data.SupplierID = SupplierIdsValues.join(',');

        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: responseData } = resp;
            if (status) {
                toastr.success(message, "", { timeOut: 500 });
                setTimeout(() => {
                    window.location.href = `${urlManufacturer}?ItemID=${responseData.ID}&ManufacturerID=${responseData.ManufacturerID}`;
                }, 300); 
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        } catch (error) {
            toastr.error("Failed to save Item due to a technical error.", "", { timeOut: 500 });
        } finally {
            _$saveBtn.text('Save Item').prop('disabled', false);
        }
    });

    _$itemInfoForm.find('.js-search-manufacturer').on('change', function (e) {
        let manufacturerID = _$itemInfoForm.find('input[name="ManufacturerID"]').val();
        let $brandSelect = _$itemInfoForm.find('select[name="BrandID"]');
        $brandSelect.html('<option value="">Loading...</option>').prop('disabled', true);

        const url = getBrandShortList;
        const data = { ManufacturerID: manufacturerID };
        $.post(url, data, function (resp) {
            const { Status: status, Message: message, Data: responseDate } = resp;
            if (status) {
                $brandSelect.html('<option value="">Select Brand</option>').prop('disabled', false);
                for (var i = 0; i < responseDate.length; i++) {
                    var option = `<option value="${responseDate[i].ID}">${responseDate[i].Title}</option>`;
                    $brandSelect.append(option);
                }
            } else {
                $brandSelect.html('<option value="">Select Brand</option>').prop('disabled', false);
            }
        }).fail(function () {
            $brandSelect.html('<option value="">Select Brand</option>').prop('disabled', false);
        });
    });

    function renderSaltTags() {
        const medicineSaltValues = _$itemInfoForm.find('input[name="MedicineSaltHidden"]').val()?.trim();
        if (!medicineSaltValues) return;

        const saltsArray = medicineSaltValues.split(',').map(salt => salt.trim());
        const $saltSelect2 = _$itemInfoForm.find('select[name="MedicineSalt"]');

        $saltSelect2.select2({
            tags: true,
            createTag: params => ({ id: $.trim(params.term), text: $.trim(params.term), newTag: true })
        });

        $saltSelect2.append(saltsArray.map(tag => new Option(tag, tag, false, true))).trigger('change');
    }
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