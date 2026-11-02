
// Give Alphabets Class to fields that only take alphabets
//Give Numeric Class to fields that only take number
//



$('.Alphabets').each(function () {
    $(this).keydown(function (e, t) {
        try {
            if (window.event) {
                var charCode = window.event.keyCode;
            }
            else if (e) {
                var charCode = e.which;
            }
            else { return true; }
            if ((charCode > 7 && charCode < 47 || charCode > 64 && charCode < 91))
                return true;
            else
                return false;
        }
        catch (err) {
            alert(err.Description);
        }
    });
});


$('.AlphaNumeric').each(function () {
    $(this).keydown(function (e, t) {
        try {
            if (window.event) {
                var charCode = window.event.keyCode;
            }
            else if (e) {
                var charCode = e.which;
            }
            else { return true; }
            if ((charCode > 7 && charCode < 47 || charCode > 64 && charCode < 91 || charCode > 47 && charCode < 58 || charCode > 96 && charCode < 123)) {
                if (e.shiftKey && (charCode > 47 && charCode < 58)) {
                    return false;
                }
                return true;
            }
            else
                return false;
        }
        catch (err) {
            alert(err.Description);
        }
    });


});

$('.Numeric').each(function () {
    $(this).keydown(function (e) {
        if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 110, 190]) !== -1 ||
            (e.keyCode === 65 && (e.ctrlKey === true || e.metaKey === true)) ||
            (e.keyCode >= 35 && e.keyCode <= 40)) {
            return;
        }

        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    });



});

$('[class^="MaxLength-"]').each(function () {
    $(this).keypress(function (event) {
        var classname = $(this).attr('class');
        var size = parseInt(classname.split('-')[1]);
        var value1 = size - 1;
        var value2 = size + 2;
        if ($(this).val().length < size) {
            if (event.keyCode != '110' && event.charCode != '46') {
                $(this).attr('maxlength', value1);
            } else if (event.keyCode == '110' || event.charCode == '46') {
                $(this).attr('maxlength', value2);
            }
        }
    });
});