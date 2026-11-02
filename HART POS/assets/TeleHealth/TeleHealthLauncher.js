function launchTeleHealthPopup() {
    var $link = $('a[onclick="launchTeleHealthPopup();"]');
    if ($link.length === 0) return;

    var originalHTML = $link.html();

    // Show loading UI
    $link.html(`
        <span style="display: inline-flex; align-items: center; gap: 6px;">
            <i class="fa fa-spinner fa-spin" style="font-size: 14px;"></i>
            <span>Please wait...</span>
        </span>
    `).css({
        'pointer-events': 'none',
        'opacity': '0.6'
    });

    $.ajax({
        url: '/TeleHealth/GetRoomUrl',
        type: 'POST',
        dataType: 'json',
        success: function (data) {
            try {
                if (data.error) throw new Error(data.error);
                if (!data.roomUrl) throw new Error("No roomUrl returned.");

                const popupWidth = 1024;
                const popupHeight = 720;
                const left = (screen.width - popupWidth) / 2;
                const top = (screen.height - popupHeight) / 2;

                window.open(
                    data.roomUrl,
                    "TeleHealthVideoCall",
                    `width=${popupWidth},height=${popupHeight},top=${top},left=${left},resizable=yes,scrollbars=yes`
                );

               
            } catch (e) {
                alert("❌ Error: " + e.message);
                $link.html(originalHTML).css({
                    'pointer-events': '',
                    'opacity': ''
                });
            }
        },
        error: function (xhr, status, error) {
            alert("❌ Failed to launch TeleConsultation:\n" + error);
            $link.html(originalHTML).css({
                'pointer-events': '',
                'opacity': ''
            });
        },
        complete: function () {
         
        }
    });
}
