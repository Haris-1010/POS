$(document).ready(function () {

    var navListItems = $('ul.setup-panel li a'),
        allWells = $('.setup-content');

    allWells.hide();

    navListItems.click(function (e) {
        e.preventDefault();
        var $target = $($(this).attr('href')),
            $item = $(this).closest('li');

        if (!$item.hasClass('disabled')) {
            navListItems.closest('li').removeClass('active');
            $item.addClass('active');
            allWells.hide();
            $target.show();
        }
    });

    $('ul.setup-panel li.active a').trigger('click');

    // DEMO ONLY //
    $('#activate-step-1').on('click', function (e) {
        $("#info").text($(this).attr('id'));
        //$('ul.setup-panel li:eq(1)').removeClass('disabled');
        $('ul.setup-panel li a[href="#step-1"]').trigger('click');
        //$(this).remove();
    })

    $('#activate-step-2').on('click', function (e) {
        $("#info").text($(this).attr('id'));
        //$('ul.setup-panel li:eq(1)').removeClass('disabled');
        $('ul.setup-panel li a[href="#step-2"]').trigger('click');
        //$(this).remove();
    })
    $('#activate-step-3').on('click', function (e) {
        $("#info").text($(this).attr('id'));
        //$('ul.setup-panel li:eq(2)').removeClass('disabled');
        $('ul.setup-panel li a[href="#step-3"]').trigger('click');
        //$(this).remove();
    })
    $('#activate-step-4').on('click', function (e) {
        $("#info").text($(this).attr('id'));
        //$('ul.setup-panel li:eq(3)').removeClass('disabled');
        $('ul.setup-panel li a[href="#step-4"]').trigger('click');
        //$(this).remove();
    })
    $('button').on('click', function (e) {
        $("#info2").text($(this).attr('id'))
        var target = $(this).attr('id').split(':')[1];
        var temp = target.split("-")[1] - 1;
        $('ul.setup-panel li:eq(' + temp + ')').removeClass('disabled');
        $('ul.setup-panel li a[href="#' + target + '"]').trigger('click');
    });
});

function Validate(title) {
    var $button = $(this).closest('input-group-addon');

    $("#test").text(" validate: " + title.value);

    var $group = $(title).closest('.input-group'),
        $addon = $group.find('.input-group-addon'),
        $icon = $addon.find('span');

    if (title.value !== "") {
        $addon.addClass('success');
        $icon.attr('class', 'glyphicon glyphicon-ok');
    } else {
        $addon.addClass('danger');
        $icon.attr('class', 'glyphicon glyphicon-remove');
    }
}
