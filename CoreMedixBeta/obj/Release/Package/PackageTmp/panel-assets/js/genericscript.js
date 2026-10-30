function readURL(input) {
    var id = $(input).attr("data-src");
    if (id) {
        if (input.files && input.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                $(id).attr('src', e.target.result);
            }
            reader.readAsDataURL(input.files[0]);
        }
    }
}
function timeSince(date) {
    var seconds = Math.floor((new Date() - date) / 1000);
    var interval = Math.floor(seconds / 31536000);
    if (interval > 1) {
        return interval + " years ago";
    }
    interval = Math.floor(seconds / 2592000);
    if (interval > 1) {
        return interval + " months ago";
    }
    interval = Math.floor(seconds / 86400);
    if (interval > 1) {
        return interval + " days ago";
    }
    interval = Math.floor(seconds / 3600);
    if (interval > 1) {
        return interval + " hours ago";
    }
    interval = Math.floor(seconds / 60);
    if (interval > 1) {
        return interval + " minutes ago";
    }
    return Math.floor(seconds) + " seconds ago";
}
function DateNow() {
    var today = new Date();
    var dd = today.getDate();
    var mm = today.getMonth() + 1; //January is 0!
    var yyyy = today.getFullYear();
    var time = formatAMPM(today);
    if (dd < 10) { dd = '0' + dd }
    if (mm < 10) { mm = '0' + mm }
    today = mm + '/' + dd + '/' + yyyy + ' ' + time;
    return today;
}
function formatAMPM(date) {
    var hours = date.getHours();
    var minutes = date.getMinutes();
    var ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12;
    hours = hours ? hours : 12; // the hour '0' should be '12'
    minutes = minutes < 10 ? '0' + minutes : minutes;
    var strTime = hours + ':' + minutes + ' ' + ampm;
    return strTime;
};
function IsNullOrWhiteSpace(input) {

    return !input || !input.trim();
}

$.validator.addMethod('filesize', function (value, element, param) {
    return this.optional(element) || (element.files[0].size <= param)
});

$.validator.addMethod("roles", function (value, elem, param) {
    if ($(".roles:checkbox:checked").length > 0) { return true; }
    else { return false; }
}, "You must select at least one!");


$.validator.addMethod("roles2", function (value, elem, param) {
    if ($(".roles2:checkbox:checked").length > 0) { return true; }
    else { return false; }
}, "You must select at least one!");


$.validator.addMethod("regex", function (value, element, regexp) {
    var re = new RegExp(regexp);
    return this.optional(element) || re.test(value);
},
    "Please check your input.");


$.validator.addMethod("greaterThan", function (value, element, params) {
    if (!/Invalid|NaN/.test(new Date(value))) {
        return new Date(value) > new Date(params);
    }
    return isNaN(value) && isNaN(params) || (Number(value) > Number(params));
}, 'Must be greater than {0}.');
window.Handlebars.registerHelper('select', function (value, options) {
    var $el = $('<select />').html(options.fn(this));
    $el.find('[value="' + value + '"]').attr({ 'selected': 'selected' });
    return $el.html();
});
$(window).load(function () {
    // Animate loader off screen
    $(".se-pre-con").fadeOut("slow");;
});

$(document).on("change", "input[type=file]", function () {
    console.log("triggered");
    readURL(this);
});