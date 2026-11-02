/**
 * @author Alexander Manzyuk <admsev@gmail.com>
 * Copyright (c) 2012 Alexander Manzyuk - released under MIT License
 * https://github.com/admsev/jquery-play-sound
 * Usage: $.playSound('http://example.org/sound');
**/

(function ($) {

    $.extend({
        playSound: function () {
            return $(
              '<audio autoplay="autoplay" style="display:none;">'
                + '<source src="' + arguments[0] + '" />'
                + '<embed src="' + arguments[0] + '" hidden="true" autostart="true" loop="false" class="playSound" />'
              + '</audio>'
            ).appendTo('body');
        }
    });

})(jQuery);