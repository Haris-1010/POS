(function ($) {
    const _$dashboardSettingsTable = $('#dashboardSettingsTable');
    const _$savedbSettingsBtn = $('.js-save-btn');

    $(function () {
        _$savedbSettingsBtn.on("click", async function () {
            const _$thisBtn = $(this);
            _$thisBtn.text('Saving...').prop('disabled', true);

            const url = SaveDashBoardSettingUrl;
            const data = {
                DashboardSettings: readDashBoardItems(),
            };

            $.post(url, data, function (resp) {
                const { Status: status, Message: message, Data: response } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 2000 });
                    _$thisBtn.text('Update Settings').prop('disabled', false);
                    window.location.reload();
                }
                else {
                    toastr.error(message, "", { timeOut: 1000 });
                    _$thisBtn.text('Update Settings').prop('disabled', false);
                }
            }).fail(function () {
                toastr.error("Unable to process your request due to some technical error", "", { timeOut: 500 });
                _$thisBtn.text('Update Settings').prop('disabled', false);
            });
        });

        $('.js-close-sidebar').click(function () {
            $('.control-sidebar').hide();
        });



        // Each dashboard card opens the most relevant report. Keyed by CardID (titles can be
        // renamed/translated). Date-based reports get the range currently shown on the dashboard.
        $(document).on('click', '.card-clickable', function () {
            const cardId = parseInt($(this).data('card-id'), 10);
            const range = window.dashboardRange || {};
            const withRange = function (url) {
                return (range.from && range.to)
                    ? url + '?StartDate=' + encodeURIComponent(range.from) + '&EndDate=' + encodeURIComponent(range.to)
                    : url;
            };

            const cardRedirects = {
                1: function () { return withRange(reportSaleUrl); },              // Sales
                2: function () { return withRange(saleInvoicesUrl); },            // Total Invoices
                3: function () { return withRange(profitMarginReportUrl); },      // Profit
                4: function () { return customerLedgerUrl; },                     // Total Customers
                5: function () { return stockValuationReportUrl; },               // Stock Value
                6: function () { return expiryReportUrl; },                       // Expiring Soon
                7: function () { return itemWiseSaleReportUrl; },                 // Stock Sold Value
                8: function () { return customerOutstandingReportUrl; }           // Amount Receivable
            };

            const getUrl = cardRedirects[cardId];
            if (getUrl) {
                window.location.href = getUrl();
            }
        });
    });

    function readDashBoardItems() {
        let itemsArray = [];
        _$dashboardSettingsTable.find('tbody tr').each(function () {
            const row = $(this);
            const isVisible = row.find('input[type="checkbox"]').is(':checked');
            const obj = {
                ID: parseInt(row.find('input[name="ID"]').val()),
                CardID: parseFloat(row.find('input[name="CardID"]').val()),
                Title: row.find('input[name="Title"]').val(),
                IsVisible: isVisible,
                Order: parseFloat(row.find('input[name="Order"]').val()),
                UserID: row.find('input[name="UserID"]').val(),
            };
            itemsArray.push(obj);
        });
        return itemsArray;
    }
 
})(jQuery);