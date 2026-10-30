function Notify(Message) {
    console.log("Notification Called" + JSON.stringify(Message));
    $.niftyNoty({
        type: Message.MessageColor,
        container: Message.NotificationType,
        html: (function () {
            var context = {
                Title: Message.Title,
                Message: Message.Description,
                CustomHTML: Message.CustomHtml,
            };
            var source = $("#NotificationTemplate").html();
            var template = Handlebars.compile(source);
            var html = template(context);
            return html;
        })(),
        closeBtn: true,
        floating: {
            position: Message.Position,//top-right
            animationIn: "jello",
            animationOut: "fadeOut"
        },
        focus: true,
        timer: Message.Timer
    });
}