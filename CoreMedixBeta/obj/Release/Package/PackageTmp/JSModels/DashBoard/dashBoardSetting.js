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



        $('.card-clickable').on('click', function () {
            const cardTitle = $(this).data('card-title');

            const cardRedirects = {
                "Today’s Sales": function () {
                    const today = new Date();
                    const yyyy = today.getFullYear();
                    const mm = String(today.getMonth() + 1).padStart(2, '0');
                    const dd = String(today.getDate()).padStart(2, '0');
                    const startDate = `${yyyy}-${mm}-${dd}`;
                    const endDate = `${yyyy}-${mm}-${dd}`;
                    window.location.href = `${reportSaleUrl}?StartDate=${startDate}&EndDate=${endDate}`;
                },

                "Today Total Invoices": function () {
                    const today = new Date();
                    const yyyy = today.getFullYear();
                    const mm = String(today.getMonth() + 1).padStart(2, '0');
                    const dd = String(today.getDate()).padStart(2, '0');
                    const startDate = `${yyyy}-${mm}-${dd}`;
                    const endDate = `${yyyy}-${mm}-${dd}`;
                    window.location.href = `${saleInvoicesUrl}?StartDate=${startDate}&EndDate=${endDate}`;
                },

                "Today’s Profit": function () {
                    const today = new Date();
                    const yyyy = today.getFullYear();
                    const mm = String(today.getMonth() + 1).padStart(2, '0');
                    const dd = String(today.getDate()).padStart(2, '0');
                    const startDate = `${yyyy}-${mm}-${dd}`;
                    const endDate = `${yyyy}-${mm}-${dd}`;
                    window.location.href = `${profitMarginReportUrl}?StartDate=${startDate}&EndDate=${endDate}`;
                },

                "Total Patients Today": function () {
                    window.location.href = dailySalesReportUrl;
                },

                "Stock Value": function () {
                    window.location.href = stockValuationReportUrl;
                },

                "Today’s Stock Sold Value": function () {
                    window.location.href = stockValuationReportUrl;
                },

                "Expiring Soon": function () {
                    var Expire = 'ExpiringSoon';
                    window.location.href = `${inventoryItemsUrl}?FilterType=${Expire}`;
                }
            }; 

            if (cardRedirects[cardTitle]) {
                const url = cardRedirects[cardTitle]();
                if (url) { 
                    window.location.href = url;
                }
            } else {
                console.error("No redirect defined for card:", cardTitle);
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