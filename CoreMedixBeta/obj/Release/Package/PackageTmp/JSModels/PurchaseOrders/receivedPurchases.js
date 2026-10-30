(function ($) {
    const _$purchasesTable = $('#purchasesTable'),
        _$filterForm = $("#filterForm"),
        receivedPurchasesPermissionForm = $('#ReceivedPurchasesPermissionsForm').serializeObject();
    let receivedPurchasesPermission = {
        canPrint: "1",
    };

    let startDate, endDate;

    $(function () {
        _$filterForm.find('input[name="Date"]').daterangepicker({
            opens: 'right',
            startDate: moment().subtract(29, 'days'),
            endDate: moment(),
            ranges: {
                'Today': [moment(), moment()],
                'Yesterday': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
                'Last 7 Days': [moment().subtract(6, 'days'), moment()],
                'Last 30 Days': [moment().subtract(29, 'days'), moment()],
                'This Month': [moment().startOf('month'), moment().endOf('month')],
                'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
            }
        }, function (start, end, label) {
            startDate = start;
            endDate = end;
        });

        _$filterForm.on('click', '.js-apply-filter-btn', function () {
            getPurchaseList(true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            _$filterForm.find('.js-select2').val(null).trigger('change');
            startDate = moment().subtract(29, 'days');
            endDate = moment();
            getPurchaseList(false);
        });

        startDate = moment().subtract(29, 'days');
        endDate = moment();

        getPurchaseList(false);
    })

    async function getPurchaseList(isSearch = false) {
        let currencySymbol = $("#currency").val();
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

        _$purchasesTable.DataTable().destroy();
        const table = _$purchasesTable.DataTable({
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
                url: GetPagedPurchasesUrl,
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
                                'ReferenceNo': d[i].ReferenceNo,
                                'Date': d[i].PurchaseDate,
                                'Supplier': d[i].SuplierName,
                                'SubTotalAfterTax': `${addCommasToNumber(d[i].SubTotalAfterTax)}`,
                                'PaidAmount': `${addCommasToNumber(d[i].PaidAmount)}`,
                                'RemainingAmount': `${addCommasToNumber(d[i].RemainingAmount)}`,
                                'Actions': `<div class="btn-group-sm action-btns">
                                    ${receivedPurchasesPermission.canPrint ? `
                                        <a class="btn btn-sm btn-dark text-white" 
                                           title="Print Purchase" 
                                           data-toggle="tooltip"
                                           href="${printPurchaseUrl}?ID=${d[i].ID}" 
                                           target="_blank">
                                           <i class="fas fa-print"></i>
                                        </a>
                                    ` : ''}
                                </div>`

                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "ReferenceNo" },
                { data: "Date" },
                { data: "Supplier" },
                { data: "SubTotalAfterTax" },
                { data: "PaidAmount" },
                { data: "RemainingAmount" },
                { data: "Actions" }
            ],
            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search By Refrence No"
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            },
            footerCallback: function (tfoot, data, start, end, display) {
                var api = this.api();
                calculateAndDisplayFooter(api, 3, 'SubTotalAfterTax', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 4, 'PaidAmount', currencySymbol + ' ', true);
                calculateAndDisplayFooter(api, 5, 'RemainingAmount', currencySymbol + ' ', true);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search By Reference No')
            .addClass('seachbar-tooltip');
    }
})(jQuery)