(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable'),
        _$customerSearchElement = $('.js-search-customer');

    let startDate; // = moment().startOf('month');
    let endDate; // = moment().endOf('month');
    $(function () {
        _$filterForm.find('input[name="Date"]').daterangepicker({
            opens: 'right',
            autoUpdateInput: false,
            ranges: {
                'Today': [moment(), moment()],
                'Yesterday': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
                'Last 7 Days': [moment().subtract(6, 'days'), moment()],
                'Last 30 Days': [moment().subtract(29, 'days'), moment()],
                'This Month': [moment().startOf('month'), moment().endOf('month')],
                'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
            }
        });

        _$filterForm.find('input[name="Date"]').on('apply.daterangepicker', function (ev, picker) {
            startDate = picker.startDate;
            endDate = picker.endDate;

            if (startDate && endDate) {
                $(this).val(startDate.format('MM/DD/YYYY') + ' - ' + endDate.format('MM/DD/YYYY'));
            } else {
                $(this).val('');
            }
        });

        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getDeletedInvoicesReport('', true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();

            _$filterForm.find('input[name="CustomerID"]').val('');
            _$filterForm.find('input[name="CustomerType"]').val('');
            _$filterForm.find('select[name="LocationID"]').val('').trigger('change');
            _$filterForm.find('select[name="SortBy"]').val('ID').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('ASC').trigger('change');
            _$filterForm.find('select[name="SalePersonUsername"]').val('').trigger('change');

            _$filterForm.find('input[name="Date"]').val('');
            getDeletedInvoicesReport('', false);
        });

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

                        

                        response(results);
                    }
                });
            },
            minLength: 2,
            select: function (event, ui) {
                if (ui.item.data === null) {
                    event.preventDefault(); 
                } else {
                    let itemData = ui.item.data;
                    _$filterForm.find('input[name="CustomerID"]').val(itemData.CustomerID);
                    _$filterForm.find('input[name="CustomerType"]').val(itemData.CustomerType);
                    _$customerSearchElement.val(itemData.CustomerName);
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

        getDeletedInvoicesReport('', false)
    });

    async function getDeletedInvoicesReport(groupByColumn, isSearch = false) {
        let currencySymbol = $("#currency").val();

        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            if (startDate && endDate) {
                formData.StartDate = startDate.format('YYYY-MM-DD');
                formData.EndDate = endDate.format('YYYY-MM-DD');
            }
            formData.GroupByColumn = groupByColumn;
        } else {
            formData = {
                //StartDate: startDate.format('YYYY-MM-DD'),
                //EndDate: endDate.format('YYYY-MM-DD'),
            };
        }

        _$table.DataTable().destroy();
        const table = _$table.DataTable({
            dom: 'Blfrtip',
            buttons: [
                'copy',
                {
                    extend: 'csv',
                    text: 'Excel'
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
            pageLength: 10,
            processing: true,
            ordering: false,
            serverSide: true,
            lengthChange: true,
            searching: true,
            start: 10,
            paging: true,
            "ajax": {
                url: GetDeletedInvoicesReportUrl,
                type: "POST",
                data: formData,
                dataSrc: function (json) {
                    var return_data = new Array();
                    try {
                        json = jQuery.parseJSON(JSON.stringify(json));
                        var d = $.parseJSON(json.data);
                        json.data = d;
                        for (var i = 0; i < d.length; i++) {
                            return_data.push({
                                'InvoiceNo': d[i].InvoiceNo,
                                'DeletedOn': moment(d[i].DeletedOn).format('DD MMM YYYY, hh:mm A'), 
                                'Amount': `${addCommasToNumber(d[i].Amount)}`,
                                'Customer': d[i].Customer
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "InvoiceNo" },
                { data: "DeletedOn" }, 
                { data: "Amount" },
                { data: "Customer" }
            ],

            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Customer and Invoice No"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);

                var api = this.api();
                renderReportFooterNote(api);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();

                calculateAndDisplayFooter(api, 3, 'Amount', currencySymbol + ' ', true);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Customer and Invoice No')
            .addClass('seachbar-tooltip');
    }
})(jQuery)