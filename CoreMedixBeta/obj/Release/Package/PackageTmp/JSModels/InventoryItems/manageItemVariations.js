(function ($) {
    const _$variationsTable = $('#variationsTable');

    const _$addVariationModal = $('#addVariationModal'),
        _$addVariationForm = _$addVariationModal.find('form');

    const _$saveItemAttributeModal = $('#saveItemAttributeModal'),
        _$saveItemAttributeForm = _$saveItemAttributeModal.find('form');

    const _$searchItemElement = _$addVariationForm.find('input[name="SearchAlternate"]');
    let _$alternateItemElement = _$addVariationForm.find('select[name="AlternateItems"]');

    $(function () {
        _$addVariationForm.validate();
        _$addVariationModal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$addVariationForm.trigger('reset');
            _$addVariationForm.find('input[name="ID"]').val('0');
            _$addVariationForm.find('input[name="ExpiryDate"]').datepicker('setDate', new Date());
            _$addVariationModal.find('.js-modal-title').text("Add Variation"); 
            $('#printBarcodeLabelBtn').hide();
            _$alternateItemElement.val(null).trigger('change');
            _$addVariationForm.find('select[name="AlternateItems"]').val(null).trigger('change'); 

        });
        _$saveItemAttributeModal.on('hidden.bs.modal', function () {
            _$saveItemAttributeModal.find('form').trigger('reset');
            _$saveItemAttributeModal.find('form').find('input[name="ID"]').val('0');
        });

        _$addVariationForm.on('click', '.js-add-property-btn', function () {
            var itemType = $(this).data('type');
            _$saveItemAttributeModal.find('.js-modal-title').text(`Create ${itemType}`);
            _$saveItemAttributeForm.find('input[name="Type"]').val(itemType);
            _$saveItemAttributeModal.modal('show');
        });

        _$saveItemAttributeModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveItemAttributeForm.valid()) {
                return;
            }
            _$saveBtn.text('Saving...').prop('disabled', true);
            const url = saveItemAttribute;
            const data = _$saveItemAttributeForm.serializeObject();

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    fetchAttributeData(data.Type);
                    _$saveItemAttributeModal.find('.js-close-modal').click();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save ItemAttribute due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        _$addVariationModal.on('click', '.js-save-btn', async function () {
            const _$saveBtn = $(this);
            if (!_$addVariationForm.valid()) {
                return;
            }
            $('button').prop('disabled', true);
            _$saveBtn.text('Saving...').prop('disabled', true);

            const url = saveItemVariation;
            const data = _$addVariationForm.serializeObject();

            let alternateItemValues = data.AlternateItems;

            if (typeof alternateItemValues === 'string') {
                alternateItemValues = [alternateItemValues];
            } else if (!Array.isArray(alternateItemValues)) {
                alternateItemValues = [];
            }
            data.AlternateItems = alternateItemValues.join(','); 
            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message, Data: responseData } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$addVariationModal.modal('hide');
                    getVariations()
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

        // Autofocus barcode input when modal opens
        _$addVariationModal.on('shown.bs.modal', function () {
            $('#itemUPC').focus();
        });

        // Listen for barcode scan (simulate scan on 'Enter' key)
        $('#itemUPC').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();

                var scannedCode = $(this).val();

                // Fill it in the UPC field
                $('#itemUPC').val(scannedCode);

                // Clear and refocus for next scan
                //$(this).val('').focus();
            }
        });

        getVariations();
    });

    _$saveItemAttributeModal.on('shown.bs.modal', function () {
        _$addVariationModal.removeAttr('tabindex');
        $(this).css('z-index', parseInt(_$addVariationModal.css('z-index')) + 50);
        $(this).find('input:first').focus();
    });

    _$saveItemAttributeModal.on('hidden.bs.modal', function () {
        _$addVariationModal.attr('tabindex', '-1');
        _$addVariationModal.focus();
    });

    _$searchItemElement.autocomplete({
        source: function (request, response) {
            $.ajax({
                url: searchItemsUrl,
                data: {
                    SearchString: request.term, 
                    SearchType: "Variation"
                },
                type: "POST",
                success: function (data) {
                    let results = $.map(data.Data, function (item) {
                        return {
                            label: `${item.VariationName}`,
                            value: item.VariationName,
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
            let existingValues = _$alternateItemElement.val() || [];
            if (!existingValues.includes(itemData.VariationName)) {
                let newOption = new Option(itemData.VariationName, itemData.VariationName, true, true);
                _$alternateItemElement.append(newOption).trigger('change');
            }
            _$searchItemElement.val('').trigger('reset');
        },
        open: function () {
            $('.ui-autocomplete').css('z-index', 1050);
        }
    }).data("ui-autocomplete")._renderItem = function (ul, item) {
        return $("<li>")
            .append(`<div>${item.label}</div>`)
            .appendTo(ul);
    };

    $('#generateBarcodeBtn').on('click', function () {
        $.ajax({
            url: generateBarcodeUl, // Example endpoint
            type: 'GET',
            success: function (resp) {
                const { Status: status, Message: message, Data: barcode } = resp;
                if (status) {
                    $('#itemUPC').val(barcode);
                    toastr.success(message, "", { timeOut: 500 });
                    $('#printBarcodeLabelBtn').show();
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            },
            error: function () {
                toastr.error("Failed to generate barcode due to a technical error.", "", { timeOut: 500 });
            }
        });
    });

    $('#printBarcodeLabelBtn').on('click', function () {
        const upc = $('#itemUPC').val();
        if (!upc) {
            toastr.warning("Enter or generate a barcode first.", "", { timeOut: 500 });
            return;
        }
        if (window.POSBarcode) {
            const name = _$addVariationForm.find('input[name="VariationName"]').val() || '';
            const price = _$addVariationForm.find('input[name="SellingPrice"]').val() || '';
            POSBarcode.printLabel(upc, name, price ? parseFloat(price).toFixed(2) : '');
        } else {
            toastr.error("Barcode library not loaded.", "", { timeOut: 500 });
        }
    });

    $('#itemUPC').on('input', function () {
        $('#printBarcodeLabelBtn').toggle(!!$(this).val());
    });

    _$alternateItemElement.select2({
        placeholder: "Add Alternate Items",
        tags: true,
        createTag: function () {
            return null;
        }
    });

    _$alternateItemElement.on('select2:opening', function (e) {
        e.preventDefault();
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

    _$variationsTable.on('click', '.js-delete', function () {
        let Id = $(this).data('id');
        bootbox.confirm({
            title: 'Delete Variation',
            message: "Do you want to delete this variation?",
            size: 'small',
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
            callback: async function (result) {
                if (result) {
                    const url = DeleteVariationUrl;
                    const data = {
                        ID: Id
                    };
                    try {
                        const resp = await $.post(url, data);
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, "", { timeOut: 500 });
                            getVariations();
                        } else {
                            toastr.error(message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("An error occurred while deleting the variation.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });

    _$variationsTable.on('click', '.js-edit', async function () {
        let Id = $(this).data('id');
        const url = GetVariationByIDUrl;
        const data = {
            ID: Id
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: response } = resp;
            if (status) {
                _$addVariationForm.find('input[name="ID"]').val(response.ID);
                _$addVariationForm.find('input[name="ItemID"]').val(response.ItemID);
                _$addVariationForm.find('input[name="VariationName"]').val(response.VariationName);
                _$addVariationForm.find('input[name="SKU"]').val(response.SKU);
                _$addVariationForm.find('select[name="FormationID"]').val(response.FormationID);
                _$addVariationForm.find('select[name="UnitID"]').val(response.UnitID);
                _$addVariationForm.find('select[name="CategoryID"]').val(response.CategoryID);
                _$addVariationForm.find('select[name="SubCategoryID"]').val(response.SubCategoryID);
                _$addVariationForm.find('input[name="UPC"]').val(response.UPC);
                $('#printBarcodeLabelBtn').toggle(!!response.UPC);
                _$addVariationForm.find('input[name="OpeningStock"]').val(response.OpeningStock);
                _$addVariationForm.find('input[name="NoOfPillsInPack"]').val(response.NoOfPillsInPack);
                _$addVariationForm.find('input[name="ReorderPoint"]').val(response.ReorderPoint);
                _$addVariationForm.find('input[name="OpeningStockRate"]').val(response.OpeningStockRate);
                _$addVariationForm.find('input[name="Batch"]').val(response.Batch); 
                _$addVariationForm.find('input[name="Weight"]').val(response.Weight);
                _$addVariationForm.find('select[name="WeightUnitID"]').val(response.WeightUnitID);
                _$addVariationForm.find('input[name="Tax"]').val(response.Tax);
                _$addVariationForm.find('input[name="SellingPrice"]').val(response.SellingPrice);
                _$addVariationForm.find('input[name="CostPrice"]').val(response.CostPrice);
                _$addVariationForm.find('input[name="Margin"]').val(response.Margin);
                _$addVariationForm.find('input[name="ExpiryDate"]').datepicker('setDate', formatDate(response.ExpiryDate, true));
                renderAlternateItemsTags(response.AlternateItems);
                _$addVariationModal.find('.js-modal-title').text("Update Variation");
                _$addVariationModal.modal("show");
            } else {
                toastr.error(message, "", { timeOut: 500 });
            }
        } catch (error) {
            toastr.error("An error occurred", "", { timeOut: 500 });
        }
    });

    _$variationsTable.on('click', '.js-print-barcode', function () {
        const upc = $(this).data('upc');
        const name = $(this).data('name');
        const price = $(this).data('price');
        if (!upc) {
            toastr.warning("This variation has no barcode.", "", { timeOut: 500 });
            return;
        }
        if (window.POSBarcode) {
            const priceStr = price ? parseFloat(price).toFixed(2) : '';
            POSBarcode.printLabel(upc, name, priceStr);
        } else {
            toastr.error("Barcode library not loaded.", "", { timeOut: 500 });
        }
    });

    async function getVariations() {
        let itemID = _$addVariationForm.find('input[name="ItemID"]').val();

       

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

        _$variationsTable.DataTable().destroy();
        const table = _$variationsTable.DataTable({
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
            start: 10,
            paging: true,
            "ajax": {
                url: GetPagedVariationsUrl,
                type: "POST",
                data: {
                    ItemID: itemID
                },
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    var d = $.parseJSON(json.data);
                    json.data = d;
                    var return_data = new Array();
                    for (var i = 0; i < d.length; i++) {
                        let qty = parseFloat(d[i].OpeningStock) || 0;
                        let noOfPillsInPack = parseFloat(d[i].NoOfPillsInPack) || 1;
                        let availablePacks = (noOfPillsInPack > 0) ? (qty / noOfPillsInPack) : 0;

                        return_data.push({
                            Name: d[i].VariationName,
                            SKU: d[i].SKU,
                            Formation: d[i].Formation,
                            Weight: d[i].Weight,
                            Qty: d[i].OpeningStock,
                            NoOfPillsInPack: d[i].NoOfPillsInPack,
                            AvailablePacks: availablePacks,
                            OpeningStockRate: d[i].OpeningStockRate,
                            ReorderPoint: d[i].ReorderPoint,
                            SellingPrice: d[i].SellingPrice,
                            CostPrice: d[i].CostPrice,
                            ExpiryDate: formatDate(d[i].ExpiryDate),
                            Category: d[i].Category,
                            SubCategory: d[i].SubCategory,
                            Unit: d[i].Unit,
                            UPC: d[i].UPC,
                            Batch: d[i].Batch, 
                            Margin: d[i].Margin,
                            Tax: d[i].Tax,
                            Actions: `<div class="btn-group-sm action-btns">
                                         <button class="btn btn-default btn-sm js-edit" title="Edit" data-id="${d[i].ID}" data-toggle="tooltip">
                                             <i class="fa fa-edit"></i>
                                         </button>
                                         <button class="btn btn-sm btn-info js-print-barcode" title="Print Barcode Label" data-id="${d[i].ID}" data-upc="${d[i].UPC || ''}" data-name="${d[i].VariationName || ''}" data-price="${d[i].SellingPrice || ''}" data-toggle="tooltip" ${d[i].UPC ? '' : 'disabled'}>
                                             <i class="fas fa-barcode"></i>
                                         </button>
                                         <button class="btn btn-sm btn-danger js-delete" data-toggle="tooltip" data-id="${d[i].ID}" title="Delete Variation">
                                             <i class="fa fa-trash"></i>
                                         </button>
                                         <button class="btn btn-sm btn-success js-image" data-toggle="tooltip" data-id="${d[i].ID}" title="Add Images">
                                            <i class="fas fa-image"></i>
                                         </button>
                                     </div>`
                        });
                    }
                    return return_data;
                },
            },
            "columns": [
                { "data": "Name" },
                { "data": "SKU" },
                { "data": "Formation" },
                { "data": "Weight" },
                { "data": "Qty" },
                { "data": "NoOfPillsInPack" },
                { "data": "AvailablePacks" },
                { "data": "OpeningStockRate" },
                { "data": "ReorderPoint" },
                { "data": "SellingPrice" },
                { "data": "CostPrice" },
                { "data": "ExpiryDate" },
                { "data": "Category" },
                { "data": "SubCategory" },
                { "data": "Unit" },
                { "data": "UPC" },
                { "data": "Batch" }, 
                { "data": "Margin" },
                { "data": "Tax" },
                { "data": "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Name"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search By Name')
            .addClass('seachbar-tooltip');
    }

    function fetchAttributeData(type) {
        var button = $('.js-add-property-btn[data-type="' + type + '"]');
        var _$select = button.closest('.input-group').find('select');
        var propertyType = type;

        let url = GetItemAttributesUrl;

        return $.post(url)
            .done(function (resp) {
                const { Status: status, Message: message, Data: data } = resp;
                if (status) {
                    if (data && data.length > 0) {
                        const filteredData = data.filter(item => item.Type === propertyType);
                        const optionArray = $.map(filteredData, function (item) {
                            return `<option value="${item.ID}">${item.Name}</option>`;
                        });
                        optionArray.unshift(`<option value="0">Select ${propertyType}</option>`);
                        _$select.html(optionArray.join(''));
                    } else {
                        _$select.html(`<option value="0">Select ${propertyType}</option>`);
                    }
                } else {
                    _$select.html(`<option value="0">Select ${propertyType}</option>`);
                }
            })
            .fail(function () {
                _$select.html(`<option value="0">Select ${propertyType}</option>`);
            });
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