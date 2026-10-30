/* CoreMedix theme controller — light / dark / auto(evening) + header switch */
(function () {
    'use strict';

    var STORAGE_KEY = 'cmTheme';
    var MODES = ['light', 'dark', 'auto'];

    function readMode() {
        try {
            var m = window.localStorage.getItem(STORAGE_KEY);
            return MODES.indexOf(m) >= 0 ? m : 'auto';
        } catch (e) {
            return 'auto';
        }
    }

    function writeMode(mode) {
        try {
            window.localStorage.setItem(STORAGE_KEY, mode);
        } catch (e) { /* ignore */ }
    }

    function isNight() {
        var h = new Date().getHours();
        return h >= 18 || h < 6;
    }

    function resolveTheme(mode) {
        if (mode === 'dark') return 'dark';
        if (mode === 'light') return 'light';
        return isNight() ? 'dark' : 'light';
    }

    function chartFontColor(theme) {
        return theme === 'dark' ? '#e7eaf3' : '#12141c';
    }

    function chartGridColor(theme) {
        return theme === 'dark' ? 'rgba(255,255,255,0.09)' : 'rgba(0,0,0,0.08)';
    }

    function setChartDefaults(theme) {
        if (typeof window.Chart === 'undefined' || !Chart.defaults) return;
        var font = chartFontColor(theme);
        var grid = chartGridColor(theme);
        try {
            Chart.defaults.global.defaultFontColor = font;

            var scale = Chart.defaults.scale;
            if (scale) {
                if (scale.ticks) {
                    scale.ticks.fontColor = font;
                    scale.ticks.backdropColor = theme === 'dark' ? 'rgba(0,0,0,0)' : 'rgba(255,255,255,0.75)';
                }
                if (scale.gridLines) {
                    scale.gridLines.color = grid;
                    scale.gridLines.zeroLineColor = grid;
                }
                if (scale.pointLabels) scale.pointLabels.fontColor = font;
                if (scale.angleLines) scale.angleLines.color = grid;
            }

            if (Chart.defaults.legend && Chart.defaults.legend.labels) {
                Chart.defaults.legend.labels.fontColor = font;
            }
            if (Chart.defaults.title) Chart.defaults.title.color = font;
            if (Chart.defaults.tooltips) {
                Chart.defaults.tooltips.titleFontColor = theme === 'dark' ? '#ffffff' : '#ffffff';
                Chart.defaults.tooltips.bodyFontColor = theme === 'dark' ? '#e7eaf3' : '#12141c';
            }
        } catch (e) { /* ignore */ }
    }

    function repaintExistingCharts(theme) {
        if (typeof window.Chart === 'undefined' || !Chart.instances) return;
        var font = chartFontColor(theme);
        var grid = chartGridColor(theme);
        var ids = Object.keys(Chart.instances);
        for (var i = 0; i < ids.length; i++) {
            var chart = Chart.instances[ids[i]];
            if (!chart || !chart.options) continue;
            var opts = chart.options;
            try {
                if (opts.legend && opts.legend.labels) opts.legend.labels.fontColor = font;
                if (opts.title && opts.title.display) opts.title.color = font;
                if (opts.tooltips) {
                    opts.tooltips.titleFontColor = '#ffffff';
                    opts.tooltips.bodyFontColor = font;
                }
                ['xAxes', 'yAxes'].forEach(function (axisKey) {
                    var axes = opts.scales && opts.scales[axisKey];
                    if (!axes) return;
                    axes.forEach(function (axis) {
                        if (axis.ticks) axis.ticks.fontColor = font;
                        if (axis.gridLines) {
                            axis.gridLines.color = grid;
                            axis.gridLines.zeroLineColor = grid;
                        }
                    });
                });
                if (opts.scales && opts.scales.r && opts.scales.r.ticks) {
                    opts.scales.r.ticks.fontColor = font;
                }
                chart.update(0);
            } catch (e) { /* ignore this chart */ }
        }
    }

    function syncMenu(theme, mode) {
        var options = document.querySelectorAll('.cm-theme-option');
        for (var i = 0; i < options.length; i++) {
            var el = options[i];
            var elMode = el.getAttribute('data-theme-mode');
            if (elMode === mode) el.classList.add('active');
            else el.classList.remove('active');
        }

        var icons = document.querySelectorAll('.js-theme-icon');
        var iconClass = theme === 'dark' ? 'fa-moon' : 'fa-sun';
        for (var j = 0; j < icons.length; j++) {
            var icon = icons[j];
            icon.classList.remove('fa-moon', 'fa-sun', 'fa-clock');
            icon.classList.add(iconClass);
        }
    }

    function apply() {
        var mode = readMode();
        var theme = resolveTheme(mode);
        var root = document.documentElement;
        root.setAttribute('data-theme', theme);
        root.setAttribute('data-theme-mode', mode);
        setChartDefaults(theme);
        syncMenu(theme, mode);
        return theme;
    }

    function setMode(mode) {
        if (MODES.indexOf(mode) < 0) mode = 'auto';
        writeMode(mode);
        var theme = apply();
        repaintExistingCharts(theme);
    }

    window.CoreMedixTheme = {
        apply: apply,
        setMode: setMode,
        getMode: readMode,
        getTheme: function () { return resolveTheme(readMode()); }
    };

    apply();

    function onReady() {
        document.addEventListener('click', function (e) {
            var target = e.target.closest ? e.target.closest('.cm-theme-option') : null;
            if (!target) return;
            e.preventDefault();
            setMode(target.getAttribute('data-theme-mode'));
        });

        // Item / patient search dropdown ka smooth open animation
        if (window.jQuery) {
            jQuery(document).on('autocompleteopen', 'input, textarea, select', function () {
                var $input = jQuery(this);
                var $menu = null;
                try {
                    $menu = $input.autocomplete('widget');
                } catch (e) { $menu = null; }
                if (!$menu || !$menu.length) {
                    var inst = $input.data('ui-autocomplete');
                    if (inst && inst.menu && inst.menu.element) $menu = inst.menu.element;
                }
                if (!$menu || !$menu.length) return;
                $menu.removeClass('cm-menu-anim');
                if ($menu[0]) { var _reflow = $menu[0].offsetWidth; }
                $menu.addClass('cm-menu-anim');
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', onReady);
    } else {
        onReady();
    }

    // Auto mode ke liye har minute theme dobara check karo (shaam 6 baje switch)
    setInterval(function () {
        if (readMode() === 'auto') apply();
    }, 60000);
})();
