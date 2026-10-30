/* CoreMedix language switcher - English / Urdu (real Urdu script, RTL per translated element).
   Usage on any page: add data-i18n="key" to an element whose text should translate, and add
   the key to TRANSLATIONS below. Call window.CoreMedixI18n.t('key') from other scripts
   (e.g. chatbot.js) to read the current-language string for a key. */
(function () {
    'use strict';

    var STORAGE_KEY = 'cmLang';
    var MODES = ['en', 'ur'];

    var TRANSLATIONS = {
        // Top navigation
        nav_customer_mgmt: { en: 'Customer Management', ur: 'گاہک کا انتظام' },
        nav_customers: { en: 'Customers', ur: 'گاہک' },
        nav_inventory_mgmt: { en: 'Inventory Management', ur: 'انوینٹری کا انتظام' },
        nav_suppliers: { en: 'Suppliers', ur: 'سپلائرز' },
        nav_manufacturers: { en: 'Manufacturers', ur: 'تیار کنندگان' },
        nav_items: { en: 'Items', ur: 'اشیاء' },
        nav_sales: { en: 'Sales', ur: 'فروخت' },
        nav_invoices: { en: 'Invoices', ur: 'انوائسز' },
        nav_new_invoice: { en: 'New Invoice', ur: 'نیا انوائس' },
        nav_purchases: { en: 'Purchases', ur: 'خریداری' },
        nav_purchase_orders: { en: 'Purchase Orders', ur: 'خریداری آرڈرز' },
        nav_received_purchase: { en: 'Received Purchase', ur: 'موصول شدہ خریداری' },
        nav_reports: { en: 'Reports', ur: 'رپورٹس' },
        nav_settings: { en: 'Settings', ur: 'ترتیبات' },
        nav_user_mgmt: { en: 'User Management', ur: 'صارف کا انتظام' },
        nav_hot_keys: { en: 'Hot Keys', ur: 'ہاٹ کیز' },

        // Header
        header_new_sale: { en: 'New Sale', ur: 'نئی سیل' },
        header_logout: { en: 'Logout', ur: 'لاگ آؤٹ' },

        // Dashboard
        dash_welcome_back: { en: 'Welcome back,', ur: 'خوش آمدید،' },
        dash_snapshot_for: { en: 'Here is your POS snapshot for', ur: 'یہ ہے آپ کا POS خلاصہ، بتاریخ' },
        dash_settings_btn: { en: 'Settings', ur: 'ترتیبات' },
        dash_showing: { en: 'Showing', ur: 'دکھا رہے ہیں' },
        dash_range_today: { en: 'Today', ur: 'آج' },
        dash_range_yesterday: { en: 'Yesterday', ur: 'گزشتہ روز' },
        dash_range_lastweek: { en: 'Last Week', ur: 'پچھلا ہفتہ' },
        dash_range_thisweek: { en: 'This Week', ur: 'اس ہفتے' },
        dash_range_lastmonth: { en: 'Last Month', ur: 'پچھلا مہینہ' },
        dash_range_thismonth: { en: 'This Month', ur: 'اس مہینے' },
        dash_range_alltime: { en: 'All Time', ur: 'تمام وقت' },
        dash_range_custom: { en: 'Custom Date', ur: 'مخصوص تاریخ' },
        dash_pick_dates: { en: 'Pick dates', ur: 'تاریخیں منتخب کریں' },
        dash_card_sales: { en: 'Sales', ur: 'فروخت' },
        dash_card_invoices: { en: 'Total Invoices', ur: 'کل انوائسز' },
        dash_card_profit: { en: 'Profit', ur: 'منافع' },
        dash_card_customers: { en: 'Total Customers', ur: 'کل گاہک' },
        dash_card_stock_value: { en: 'Stock Value', ur: 'اسٹاک کی قیمت' },
        dash_card_expiring: { en: 'Expiring Soon', ur: 'میعاد ختم ہونے والا' },
        dash_card_stock_sold: { en: 'Stock Sold Value', ur: 'فروخت شدہ اسٹاک' },
        dash_card_receivable: { en: 'Amount Receivable', ur: 'وصولی قابل رقم' },
        dash_analytics: { en: 'Analytics', ur: 'تجزیہ' },
        dash_sales_profit_title: { en: 'Sales & Profit', ur: 'فروخت اور منافع' },
        dash_monthly_sales_title: { en: 'Monthly Sales', ur: 'ماہانہ فروخت' },
        dash_monthly_sales_sub: { en: 'Last 6 months revenue', ur: 'پچھلے 6 مہینوں کی آمدنی' },
        dash_top_products_title: { en: 'Top Products', ur: 'بہترین پراڈکٹس' },
        dash_top5: { en: 'Top 5', ur: 'بہترین 5' },
        dash_inventory_health_title: { en: 'Inventory Health', ur: 'انوینٹری کی صحت' },
        dash_inventory_health_sub: { en: 'Total products vs. low stock & expiring soon', ur: 'کل پراڈکٹس بمقابلہ کم اسٹاک اور میعاد ختم ہونے والے' },
        dash_live: { en: 'Live', ur: 'لائیو' },
        dash_settings_title: { en: 'Dashboard Setting', ur: 'ڈیش بورڈ کی ترتیبات' },
        dash_cards_options: { en: 'Cards Options', ur: 'کارڈز کے آپشنز' },
        dash_update_settings_btn: { en: 'Update Settings', ur: 'ترتیبات اپڈیٹ کریں' }
    };

    function readMode() {
        try {
            var m = window.localStorage.getItem(STORAGE_KEY);
            return MODES.indexOf(m) >= 0 ? m : 'en';
        } catch (e) {
            return 'en';
        }
    }

    function writeMode(mode) {
        try {
            window.localStorage.setItem(STORAGE_KEY, mode);
        } catch (e) { /* ignore */ }
    }

    function t(key) {
        var entry = TRANSLATIONS[key];
        if (!entry) return null;
        var mode = readMode();
        return entry[mode] || entry.en;
    }

    function syncMenu(mode) {
        var options = document.querySelectorAll('.cm-lang-option');
        for (var i = 0; i < options.length; i++) {
            var el = options[i];
            if (el.getAttribute('data-lang-mode') === mode) el.classList.add('active');
            else el.classList.remove('active');
        }

        var labels = document.querySelectorAll('.js-lang-icon-label');
        for (var j = 0; j < labels.length; j++) {
            labels[j].textContent = mode === 'ur' ? 'UR' : 'EN';
        }
    }

    function apply() {
        var mode = readMode();
        var isUrdu = mode === 'ur';
        document.documentElement.setAttribute('lang', mode);

        var nodes = document.querySelectorAll('[data-i18n]');
        for (var i = 0; i < nodes.length; i++) {
            var node = nodes[i];
            var text = t(node.getAttribute('data-i18n'));
            if (text === null) continue;
            node.textContent = text;
            // Only the translated fragment flips direction - the surrounding layout (icons,
            // flex rows, sidebar position) stays put so nothing else on the page shifts.
            node.setAttribute('dir', isUrdu ? 'rtl' : 'ltr');
            node.classList.toggle('cm-i18n-ur', isUrdu);
        }

        syncMenu(mode);
        return mode;
    }

    function setMode(mode) {
        if (MODES.indexOf(mode) < 0) mode = 'en';
        writeMode(mode);
        apply();
        try {
            document.dispatchEvent(new CustomEvent('cm-lang-changed', { detail: { mode: mode } }));
        } catch (e) { /* older browsers without CustomEvent constructor - ignore */ }
    }

    window.CoreMedixI18n = {
        apply: apply,
        setMode: setMode,
        getMode: readMode,
        t: t
    };

    apply();

    function onReady() {
        document.addEventListener('click', function (e) {
            var target = e.target.closest ? e.target.closest('.cm-lang-option') : null;
            if (!target) return;
            e.preventDefault();
            setMode(target.getAttribute('data-lang-mode'));
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', onReady);
    } else {
        onReady();
    }
})();
