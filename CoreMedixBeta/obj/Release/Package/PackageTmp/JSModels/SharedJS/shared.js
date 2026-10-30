$(function () {
    $('.js-select2').select2({ width: '99%' });
    $('.js-date-picker').datepicker({
        dateFormat: 'mm/dd/yyyy'
    }).datepicker("setDate", new Date());

    $('body').on('init.dt', function (e, ctx) {
        var api = new $.fn.dataTable.Api(ctx);
        $('.dataTables_filter input[type="search"]')
            .unbind()
            .bind('input', (tableSearchDelay(function (e) {
                api.search($(this).val()).draw();
                return;
            }, 1000)));
    });

    $('[data-toggle="tooltip"]').tooltip();
});

function startLoading(targetId = 'body', message = 'Loading...', theme = 'dark') {
    const $target = $(targetId);
    const loadingOptions = {
        theme,
        message: `<span class="fa fa-spinner fa-spin" style="font-size:40px;"></span> ${message}`
    };
    $target.loading(loadingOptions);
}

function stopLoading(targetId = 'body') {
    $(targetId).loading('stop');
}

function tableSearchDelay(callback, ms) {
    var timer = 0;
    return function () {
        var context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(function () {
            callback.apply(context, args);
        }, ms || 0);
    };
}

$.fn.serializeObject = function () {
    var o = {};
    var a = this.serializeArray();
    $.each(a, function () {
        if (o[this.name]) {
            if (!o[this.name].push) {
                o[this.name] = [o[this.name]];
            }
            o[this.name].push(this.value || '');
        } else {
            o[this.name] = this.value || '';
        }
    });
    return o;
};

function addCommasToNumber(number) {
    return number.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

function formatNumber(number) {
    if (!number || isNaN(number)) {
        return 0;
    }

    number = number.toString().replace(/,/g, '');

    if (!number || isNaN(number)) {
        return 0;
    }

    if (number % 1 !== 0) {
        return parseFloat(number).toLocaleString(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    }
    return parseInt(number, 10).toLocaleString();
}
const ExcelExportRegistry = {};
function registerExcelExport(tableId, reloadFunction, pageLimit = 1000) {
    ExcelExportRegistry[tableId] = { reloadFunction, pageLimit };
}

function parseNumber(value) {
    let cleanValue = value.replace(/[^0-9.]/g, '');

    if (cleanValue.includes('.')) {
        return parseFloat(parseFloat(cleanValue).toFixed(2));
    }
    return parseInt(cleanValue, 10) || 0;
}

function calculateAndDisplayFooter(api, columnIndex, propertyName, prefix = '', showAvg = false) {
    var data = api.data().toArray();
    var values = data.map(row => parseNumber(row[propertyName].toString()));
    if (values.length === 0) {
        var footerContent = `N/A`;

        $(api.column(columnIndex).footer()).html(footerContent);
        return;
    }
    var sum = values.reduce((acc, val) => acc + val, 0);
    var min = Math.min(...values);
    var max = Math.max(...values);
    var avg = showAvg ? (sum / values.length) : null;

    // Create footer content
    var footerContent = `
        <div style="white-space: nowrap; font-size:.8rem !important;">Sum: ${prefix}${formatNumber(sum)}</div>
        <div style="white-space: nowrap; font-size:.8rem !important;">Min: ${prefix}${formatNumber(min)}</div>
        <div style="white-space: nowrap; font-size:.8rem !important;">Max: ${prefix}${formatNumber(max)}</div>
        ${showAvg ? `<div style="white-space: nowrap; font-size:.8rem !important;">Avg: ${prefix}${formatNumber(avg)}</div>` : ''}`;

    // Set footer HTML
    $(api.column(columnIndex).footer()).html(footerContent);
}

function scrollToInvalidInput(formSelector, event) {
    let $form = $(formSelector); // Get the form
    let $firstInvalidInput = formSelector.find(".error:first"); // Find first invalid input

    if ($firstInvalidInput.length > 0) {
        event.preventDefault(); // Stop form submission
        $firstInvalidInput.focus(); // Move focus to the first invalid field
        $('html, body').animate({ scrollTop: $firstInvalidInput.offset().top - 50 }, 1000, "swing"); // Smooth scroll to input
    }
}

function generateFilterSummaryHtml(filterFormSelector = '#filterForm') {
    const $form = $(filterFormSelector);
    let summaryHtml = `<div style="font-size: 12px;">`;

    const totalStockValue = $('#totalStockValue').val();
    const totalStockQuantity = $('#totalStockQuantity').val();
    if (totalStockValue && totalStockValue.trim() !== '') {
        summaryHtml += `<div style="font-size: 12px; float: right;"><span style="margin-right: 10px;"><strong>Total Stock Amount:</strong> ${totalStockValue}</span>
        <strong>Total Stock:</strong> ${totalStockQuantity}</span>
        </div>`;
    }

    $form.find('input, select, textarea').each(function () {
        const $el = $(this);
        const type = $el.attr('type');
        const name = $el.attr('name');
        //const label = $form.find(`label[for="${$el.attr('id')}"]`).text().trim() || name;
        let value = '';

        if (!name || name === '__RequestVerificationToken' || type === 'hidden') return; // Skip hidden/system fields

        // Handle checkboxes and radios
        if ((type === 'checkbox' || type === 'radio')) {
            if (!$el.is(':checked')) return;
            value = $form.find(`label[for="${$el.attr('id')}"]`).text().trim();
        }
        // Handle select
        else if ($el.is('select')) {
            value = $el.find('option:selected').text();
            if (!$el.val()) return; // skip unselected dropdown
        }
        // Handle date range or text inputs
        else {
            value = $el.val();
        }

        // Skip if value is null, undefined, or empty string
        if (!value || value.trim() === '') return;

        let label = $el.closest('.form-group').find('label').first().text().trim();

        // Fallback: try previous sibling label
        if (!label) {
            label = $el.prevAll('label').first().text().trim();
        }

        // Skip if label still not found
        if (!label) return;

        // Clean up and add to summary
        if (value && value.trim() !== '') {
            summaryHtml += ` <span style="margin-right: 10px;"><strong>${label}:</strong> ${value}</span>`;
        }
    });

    summaryHtml += `</div>`;
    return summaryHtml;
}

function getGenericPrintButton(filterFormSelector = '#filterForm') {
    return {
        extend: 'print',
        text: 'Print',
        //footer: true, // To get footer values like SUM, MIN AND MAX etc.. just uncomment footer: true
        customize: function (win) {
            // Print styling
            $(win.document.body).css('font-size', '12pt');
            $(win.document.body).find('table')
                .addClass('compact')
                .css('font-size', '12pt')
                //.css('white-space', 'nowrap')
                .css('width', '100%');

            // 🛠️ Add style to force footer divs to break lines
            const style = `
                            .print-footer-block {
                                display: block !important;
                                margin-bottom: 4px !important;
                                font-size: 12pt !important;
                                white-space: normal !important;
                                overflow-wrap: break-word !important;
                                word-break: break-word !important;
                            }

                            tfoot th, tfoot td {
                                white-space: normal !important;
                                vertical-align: top !important;
                                font-size: 12pt !important;
                            }
                        `;
            const $styleEl = $('<style>').text(style);
            $(win.document.head).append($styleEl);

            // Add filter summary (if any)
            const filterSummary = generateFilterSummaryHtml(filterFormSelector);
            if (filterSummary) {
                $(win.document.body).prepend(filterSummary);
            }
            console.log($(win.document.head).html());
        }
    };
}

function renderReportFooterNote(tableApi, messagePrefix = " Calculations are based on the visible rows ") {
    const noteId = 'report-note';
    const pageInfo = tableApi.page.info();
    const visibleRowsCount = pageInfo.end - pageInfo.start;
    const tableWrapper = $(tableApi.table().container()).closest('.dataTables_wrapper');

    // Remove old note if exists
    tableWrapper.find(`#${noteId}`).remove();

    // Append dynamic note
    tableWrapper.append(`
        <div id="${noteId}" class="mt-2 small text-center pt-1">
            <strong>Note:</strong> ${messagePrefix} <strong>(${visibleRowsCount})</strong> in the table, according to the filters you've applied.
        </div>
    `);
}

$(document).ready(function () {

    // Hot Keys modal open
    $(document).on('click', '.js-shortcut-keys', function (e) {
        e.preventDefault();

        bootbox.dialog({
            title: 'Available Hot Keys',
            message: `
                <div>
                    <ul>
                        <li><strong>Alt + N:</strong> Open Invoice Creation Page</li>
                        <li><strong>Alt + P:</strong> Open Purchase Order Creation Page</li>
                        <li><strong>Alt + I:</strong> Open Item Creation Page</li>
                        <li><strong>Alt + R:</strong> Open Reports Page</li>
                    </ul>
                </div>
            `,
            buttons: {
                close: {
                    label: 'Close',
                    className: 'btn-secondary'
                }
            }
        });
    });
     
    $(document).on('click', '.sidebar-search-results .list-group-item', function () {
        let text = $(this).text().trim().toLowerCase();

        if (text.includes("hot keys")) {
            $('.js-shortcut-keys').trigger('click');  
        }
    });

    $(document).on('keydown', function (e) {
        if (e.altKey) {
            switch (e.key.toUpperCase()) {
                case 'N':
                    window.location.href = hotkeyUrls.newInvoice;
                    break;
                case 'P':
                    window.location.href = hotkeyUrls.newPurchase;
                    break;
                case 'I':
                    window.location.href = hotkeyUrls.newItem;
                    break;
                case 'R':
                    window.location.href = hotkeyUrls.reports;
                    break;
            }
        }
    });

});

var BulkFile = (function () {
    var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

    function isAvailable() {
        return typeof XLSX !== 'undefined';
    }

    function ensureAvailable() {
        if (isAvailable()) return true;
        toastr.error('Bulk file support is not loaded. Please refresh the page.', '', { timeOut: 3000 });
        return false;
    }

    function normalizeKey(key) {
        return String(key === null || key === undefined ? '' : key).replace(/\s+/g, ' ').trim().toLowerCase();
    }

    function findValue(row, aliases) {
        if (!row) return '';

        for (var i = 0; i < aliases.length; i++) {
            var target = normalizeKey(aliases[i]);
            for (var key in row) {
                if (Object.prototype.hasOwnProperty.call(row, key) && normalizeKey(key) === target) {
                    return row[key];
                }
            }
        }

        return '';
    }

    function pad(value) {
        return value < 10 ? '0' + value : String(value);
    }

    function formatDate(value) {
        if (value instanceof Date && !isNaN(value.getTime())) {
            return pad(value.getDate()) + '-' + MONTHS[value.getMonth()] + '-' + value.getFullYear();
        }
        if (value === null || value === undefined) return '';
        return String(value).trim();
    }

    function downloadTemplate(options) {
        if (!ensureAvailable()) return;

        var headers = options.headers || [];
        var aoa = [headers];

        (options.rows || []).forEach(function (row) {
            aoa.push(row);
        });

        var sheet = XLSX.utils.aoa_to_sheet(aoa);
        sheet['!cols'] = headers.map(function (header) {
            return { wch: Math.max(16, String(header).length + 6) };
        });

        var workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, sheet, options.sheetName || 'Template');
        XLSX.writeFile(workbook, options.fileName || 'template.xlsx');
    }

    function parse(file, onSuccess, onError) {
        if (!ensureAvailable()) return;
        if (!file) {
            if (onError) onError('Please choose a file first.');
            return;
        }

        var name = String(file.name || '').toLowerCase();
        var isTextFile = /\.(csv|txt)$/.test(name);
        var reader = new FileReader();

        reader.onerror = function () {
            if (onError) onError('Unable to read the selected file.');
        };

        reader.onload = function (event) {
            try {
                var workbook = isTextFile
                    ? XLSX.read(event.target.result, { type: 'string', cellDates: true })
                    : XLSX.read(new Uint8Array(event.target.result), { type: 'array', cellDates: true });

                var sheet = workbook.Sheets[workbook.SheetNames[0]];
                var rows = XLSX.utils.sheet_to_json(sheet, { defval: '', raw: true });

                if (!rows.length) {
                    if (onError) onError('The selected file appears to be empty.');
                    return;
                }

                onSuccess(rows);
            } catch (error) {
                if (onError) onError('Unable to read the file. Please use the downloaded template.');
            }
        };

        if (isTextFile) reader.readAsText(file);
        else reader.readAsArrayBuffer(file);
    }

    return {
        isAvailable: isAvailable,
        findValue: findValue,
        formatDate: formatDate,
        downloadTemplate: downloadTemplate,
        parse: parse
    };
})();
