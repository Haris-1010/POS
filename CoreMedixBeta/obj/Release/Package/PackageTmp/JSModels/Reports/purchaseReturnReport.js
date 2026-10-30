(function ($) {
    const _$filterForm = $('#filterForm'),
        _$searchItemElement = $('.js-search-item');

    let startDate, endDate;
    $(function () {
        _$filterForm.find('input[name="Date"]').daterangepicker({
            opens: 'right',
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

        _$searchItemElement.autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: SearchItemsUrl,
                    data: {
                        SearchString: request.term,
                        SearchType: "",
                        NearToExpire: false,
                        ManufacturerID: "0",
                        SupplierID: "0"
                    },
                    type: "POST",
                    success: function (data) {
                        let results = $.map(data.Data, function (item) {
                            let rackInfo = item.Rack ? ` - Rack(${item.Rack})` : '';
                            let expiryStatus = item.ExpiryStatus === "Expired" ? " [Expired]" : "";
                            return {
                                label: `${item.ItemName} - ${item.VariationName}`,
                                value: item.ItemName,
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
                setTimeout(function () {
                    _$searchItemElement.val(itemData.ItemName); //VariationID//ItemName
                    _$filterForm.find('input[name="ItemID"]').val(itemData.ItemID);
                    _$filterForm.find('input[name="VariationID"]').val(itemData.VariationID);
                }, 10);
                //addItemToItemsTable(itemData);
            },
            open: function () {
                $('.ui-autocomplete').css('z-index', 1050);
            }
        }).data("ui-autocomplete")._renderItem = function (ul, item) {
            return $("<li>")
                .append(`<div>${item.label}</div>`)
                .appendTo(ul);
        };

        startDate = moment();
        endDate = moment();
    });
})(jQuery)