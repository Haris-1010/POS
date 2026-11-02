$(document).ready(function () {
    function autocollapse() {
        var $tabs = $('#tabs');
        var $collapsedMenu = $('#collapsed');
        var maxTabHeight = 50;

        if (!$tabs.length || !$collapsedMenu.length) return; // Prevent errors if elements are missing

        let tabsHeight = $tabs.innerHeight();
        let children = $tabs.children('li:not(:last-child)'); // Avoid repeated selection
        let collapsedItems = $collapsedMenu.children('li');

        // Collapse items if needed
        while (tabsHeight > maxTabHeight && children.length > 0) {
            $(children.last()).prependTo($collapsedMenu);
            tabsHeight = $tabs.innerHeight();
            children = $tabs.children('li:not(:last-child)'); // Update after moving an item
        }

        // Expand items back if space allows
        while (tabsHeight < maxTabHeight && collapsedItems.length > 0) {
            $(collapsedItems.first()).insertBefore($tabs.children('li:last-child'));
            tabsHeight = $tabs.innerHeight();
            collapsedItems = $collapsedMenu.children('li'); // Update after moving an item
        }

        // Final double-check to prevent edge cases
        if ($tabs.innerHeight() > maxTabHeight) {
            autocollapse();
        }
    }

    // Run on page load
    autocollapse();

    // Efficiently handle resize event with a throttle mechanism
    let resizeTimer;
    $(window).on('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(autocollapse, 100); // Throttle calls to improve performance
    });
});