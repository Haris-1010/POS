(function ($) {
    const _$saveItemModal = $('#saveItemModal'),
        _$saveItemForm = _$saveItemModal.find('form');

    let ItemAttributesPermissions = {
        canUpdate: "1",
        canDelete: "1"
    };

    $(function () {
        // Modal reset
        _$saveItemModal.on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open');
            $('.modal-backdrop').remove();
             
            _$saveItemForm[0].reset();
             
            _$saveItemForm.find('input[name="ID"]').val('');
             
            if (_$saveItemForm.data('validator')) {
                _$saveItemForm.validate().resetForm();  
                _$saveItemForm.find('.is-invalid, .error').removeClass('is-invalid error'); 
            }
             
            _$saveItemForm.find('select').each(function () {
                $(this).val(null).trigger('change');
            }); 
            _$saveItemForm.find('input.js-date-picker').datepicker('setDate', null);
        });


        // Open modal
        $('.js-open-attribute-modal').on('click', function () {
            const itemType = $(this).data('type');
            _$saveItemModal.find('.js-modal-title').text(`Create ${itemType}`);
            _$saveItemModal.find('input[name="Type"]').val(itemType);
            _$saveItemModal.modal('show');
        });

        // Save button
        _$saveItemModal.find('.js-save-btn').on('click', async function () {
            const _$saveBtn = $(this);
            if (!_$saveItemModal.find('form').valid()) return;

            _$saveBtn.text('Saving...').prop('disabled', true);
            let formData = _$saveItemModal.find('form').serializeObject();

            try {
                const resp = await $.post(SaveItemAttributeUrl, formData);
                if (resp.Status) {
                    toastr.success(resp.Message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                    _$saveItemModal.modal('hide');
                    getInventoryItems();
                } else {
                    toastr.error(resp.Message, "", { timeOut: 500 });
                    _$saveBtn.text('Save').prop('disabled', false);
                }
            } catch (error) {
                toastr.error("Failed to save Item Attribute due to a technical error.", "", { timeOut: 500 });
                _$saveBtn.text('Save').prop('disabled', false);
            }
        });

        // Load tables
        getInventoryItems();
    });

    async function getInventoryItems() {
        const buttonsArray = [
            {
                extend: "pageLength",
                className: "btn btn-default",
                titleAttr: 'Show rows',
                text: '<i class="fa fa-ellipsis-v"></i> Show rows',
                init: function (api, node) { $(node).removeClass('dt-button'); },
            }
        ];

        try {
            const resp = await $.ajax({ url: GetItemAttributesUrl, type: 'POST', dataType: 'json' });
            if (resp.Status && resp.Data) {
                const allData = resp.Data;

                window.categoriesData = allData.filter(i => i.Type === 'Category');
                window.formationsData = allData.filter(i => i.Type === 'Formation');
                window.unitsData = allData.filter(i => i.Type === 'Unit');
                window.subcategoriesData = allData.filter(i => i.Type === 'Subcategory');
                window.weightData = allData.filter(i => i.Type === 'WeightUnit');

                initializeDataTable('#categoriesTable', window.categoriesData);
                initializeDataTable('#formationsTable', window.formationsData);
                initializeDataTable('#unitsTable', window.unitsData);
                initializeDataTable('#subcategoryTable', window.subcategoriesData);
                initializeDataTable('#weightUnitsTable', window.weightData);

                // Bind search for default active tab on page load
                const defaultTabPane = $('.tab-pane.active');
                const defaultTable = defaultTabPane.find('table');
                if (defaultTable.length && $.fn.DataTable.isDataTable(defaultTable)) {
                    const dt = defaultTable.DataTable();
                    const searchInput = defaultTable.closest('.dataTables_wrapper').find('input[type="search"]');
                    searchInput.off('keyup').on('keyup', function () {
                        dt.search(this.value).draw();
                    });
                }
            }
        } catch (error) {
            toastr.error(`Error fetching inventory data: ${error}`, "", { timeOut: 500 });
        }

        function initializeDataTable(tableId, data) {
            if ($.fn.DataTable.isDataTable(tableId)) {
                $(tableId).DataTable().clear().destroy();
            }

            const table = $(tableId).DataTable({
                dom: 'Blfrtip',
                lengthChange: false,
                buttons: buttonsArray,
                searching: true,
                stateSave: false,
                lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
                pageLength: 10,
                paging: true,
                data: data,
                columns: [
                    { data: "Name" },
                    {
                        data: null,
                        render: function (data, type, row) {
                            return `
                            <div class="btn-group-sm action-btns" role="group">
                                ${ItemAttributesPermissions.canUpdate ? `<button class="btn btn-sm btn-warning js-edit" data-id="${row.ID}" data-name="${row.Name}" title="Edit"><i class="fa fa-edit"></i></button>` : ''}
                                ${ItemAttributesPermissions.canDelete ? `<button class="btn btn-sm btn-danger js-delete" data-id="${row.ID}" title="Delete"><i class="fa fa-trash"></i></button>` : ''}
                            </div>`;
                        }
                    }
                ],
                language: {
                    emptyTable: "No records found",
                    searchPlaceholder: "Search"
                },
                drawCallback: function () {
                    var api = this.api();
                    var info = api.page.info();
                    var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');

                    // Show/hide entire pagination if more than 1 page
                    pagination.toggle(info.pages > 1);

                    // Hide Previous button on first page
                    pagination.find('.paginate_button.previous').toggle(info.page > 0);

                    // Hide Next button on last page
                    pagination.find('.paginate_button.next').toggle(info.page < info.pages - 1);
                }

            });
             
            const searchInput = $(tableId + '_filter input');
            searchInput.off('keyup input').on('keyup input', function () {
                table.search(this.value).draw();
            });

            // Detect click on cross button and reset filter
            searchInput.on('search', function () {
                if (!this.value) { // Cross clicked and input cleared
                    table.search('').draw();
                }
            });


        }
    }

    // Tab switch event
    $('a[data-toggle="pill"]').on('shown.bs.tab', function (e) {
        $('div.tab-pane').each(function () {
            const tableId = $(this).find('table').attr('id');
            if (tableId && $.fn.DataTable.isDataTable('#' + tableId)) {
                $('#' + tableId).DataTable().search('').draw(); // Reset search
            }
        });

        const targetTabPane = $($(e.target).attr('href'));
        const targetTable = targetTabPane.find('table');
        if (targetTable.length && $.fn.DataTable.isDataTable(targetTable)) {
            const dt = targetTable.DataTable();
            const searchInput = targetTable.closest('.dataTables_wrapper').find('input[type="search"]');
            searchInput.off('keyup').on('keyup', function () {
                dt.search(this.value).draw();
            });
        }
    });

    // Edit item
    $(document).on('click', '.js-edit', function () {
        const _$this = $(this);
        const type = _$this.closest('.tab-pane').find('.btn-primary-custom').data('type');
        _$saveItemModal.find('form').find('input[name="ID"]').val(_$this.data('id'));
        _$saveItemModal.find('form').find('input[name="Title"]').val(_$this.data('name'));
        _$saveItemModal.find('form').find('input[name="Type"]').val(type);
        _$saveItemModal.modal('show');
    });

    // Delete item
    $(document).on('click', '.js-delete', function () {
        const inventoryID = $(this).data('id');
        bootbox.confirm({
            title: 'Delete Inventory Item',
            message: "Do you want to delete this Inventory Item?",
            size: 'small',
            buttons: {
                confirm: { label: 'Confirm', className: 'btn-success' },
                cancel: { label: 'Cancel', className: 'btn-danger' }
            },
            callback: async function (result) {
                if (result) {
                    try {
                        const resp = await $.post(DeleteItemAttributeUrl, { ID: inventoryID });
                        if (resp.Status) {
                            toastr.success(resp.Message, "", { timeOut: 500 });
                            getInventoryItems();
                        } else {
                            toastr.error(resp.Message, "", { timeOut: 500 });
                        }
                    } catch (error) {
                        toastr.error("Failed to delete manufacturer due to a technical error.", "", { timeOut: 500 });
                    }
                }
            }
        });
    });

})(jQuery);
