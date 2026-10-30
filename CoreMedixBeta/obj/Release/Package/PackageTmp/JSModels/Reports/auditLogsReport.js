(function ($) {
    const _$filterForm = $('#filterForm'),
        _$table = $('#reportTable');

    let startDate;
    let endDate;
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
            getAuditLogsReport(true);
        });

        _$filterForm.on('click', '.js-clear-filter-btn', function () {
            _$filterForm[0].reset();
            startDate = null;
            endDate = null;

            _$filterForm.find('select[name="Action"]').val('').trigger('change');
            _$filterForm.find('select[name="Username"]').val('').trigger('change');
            _$filterForm.find('select[name="SortBy"]').val('CreatedOn').trigger('change');
            _$filterForm.find('select[name="SortByOrder"]').val('DESC').trigger('change');
            _$filterForm.find('input[name="Date"]').val('');

            getAuditLogsReport(false);
        });

        getAuditLogsReport(false);
    });

    async function getAuditLogsReport(isSearch = false) {
        let formData = {};

        if (isSearch) {
            formData = _$filterForm.serializeObject();
            if (startDate && endDate) {
                formData.StartDate = startDate.format('YYYY-MM-DD');
                formData.EndDate = endDate.format('YYYY-MM-DD');
            }
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
                url: GetAuditLogsReportUrl,
                type: "POST",
                data: formData,
                dataSrc: function (json) {
                    var return_data = new Array();
                    try {
                        json = jQuery.parseJSON(JSON.stringify(json));
                        var d = $.parseJSON(json.data);
                        json.data = d;
                        for (var i = 0; i < d.length; i++) {
                            var entity = d[i].EntityType || '';
                            if (d[i].EntityID) {
                                entity += ' #' + d[i].EntityID;
                            }
                            return_data.push({
                                'DateTime': d[i].CreatedOn ? moment(d[i].CreatedOn).format('DD MMM YYYY, hh:mm A') : '',
                                'Username': d[i].Username || '',
                                'Action': d[i].Action ? `<span class="badge badge-info">${d[i].Action}</span>` : '',
                                'Entity': entity,
                                'Details': d[i].Details || '',
                                'IPAddress': d[i].IPAddress || ''
                            });
                        }
                    } catch (ex) {
                    }
                    return return_data;
                }
            },
            "columns": [
                { data: "DateTime" },
                { data: "Username" },
                { data: "Action" },
                { data: "Entity" },
                { data: "Details" },
                { data: "IPAddress" }
            ],

            "language": {
                emptyTable: "No records found",
                searchPlaceholder: "Search by user, details, IP..."
            },
            drawCallback: function (settings) {
                var pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            }
        });
        $('.dataTables_filter input')
            .attr('title', 'Search by user, details, IP...')
            .addClass('seachbar-tooltip');
    }
})(jQuery)
