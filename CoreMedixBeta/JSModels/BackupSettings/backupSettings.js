(function ($) {
    const _$settingsForm = $('#backupSettingsForm');
    const _$frequencySelect = $('.js-frequency-select');
    const _$customFrequencyWrap = $('.js-custom-frequency-wrap');
    const _$customFrequency = $('.js-custom-frequency');
    const _$folderPathInput = $('.js-backup-folder-path');
    const _$folderTestResult = $('.js-folder-test-result');
    const _$saveBtn = $('.js-save-settings');
    const _$testFolderBtn = $('.js-test-folder');
    const _$backupNowBtn = $('.js-run-backup-now');
    const _$historyTable = $('#backupHistoryTable');
    const _$detailsModal = $('#backupDetailsModal');
    const _$restoreModal = $('#restoreBackupModal');

    const antiForgeryToken = () => $('input[name="__RequestVerificationToken"]').val();

    $(function () {
        toggleCustomFrequency();
        _$frequencySelect.on('change', toggleCustomFrequency);

        _$saveBtn.on('click', saveSettings);
        _$testFolderBtn.on('click', testFolder);
        _$backupNowBtn.on('click', runBackupNow);
        $('.js-refresh-history').on('click', function () {
            reloadHistory();
            refreshStatusSummary();
        });

        initHistoryTable();
        bindHistoryActions();
        bindRestoreActions();
    });

    function toggleCustomFrequency() {
        const isCustom = _$frequencySelect.val() === '0';
        _$customFrequencyWrap.toggle(isCustom);
    }

    function collectSettingsPayload() {
        let frequency = parseInt(_$frequencySelect.val(), 10);
        if (frequency === 0) {
            frequency = parseInt(_$customFrequency.val(), 10);
        }
        return {
            ID: parseInt(_$settingsForm.find('input[name="ID"]').val(), 10) || 0,
            IsEnabled: $('#IsEnabled').is(':checked'),
            FrequencyMinutes: frequency,
            BackupFolderPath: $.trim(_$folderPathInput.val()),
            RetentionDays: parseInt(_$settingsForm.find('input[name="RetentionDays"]').val(), 10),
            __RequestVerificationToken: antiForgeryToken()
        };
    }

    async function saveSettings() {
        const payload = collectSettingsPayload();

        if (!payload.BackupFolderPath) {
            toastr.error('Backup folder path is required.', '', { timeOut: 1000 });
            return;
        }
        if (!payload.FrequencyMinutes || isNaN(payload.FrequencyMinutes) || payload.FrequencyMinutes < 5 || payload.FrequencyMinutes > 10080) {
            toastr.error('Backup frequency must be between 5 and 10,080 minutes.', '', { timeOut: 1000 });
            return;
        }
        if (isNaN(payload.RetentionDays) || payload.RetentionDays < 0 || payload.RetentionDays > 3650) {
            toastr.error('Retention must be between 0 and 3650 days.', '', { timeOut: 1000 });
            return;
        }

        _$saveBtn.prop('disabled', true).html('<i class="far fa-spinner fa-spin"></i> Saving...');

        try {
            const resp = await $.post(SaveBackupSettingsUrl, payload);
            const { Status: status, Message: message } = resp;
            if (status) {
                toastr.success(message, '', { timeOut: 1500 });
                refreshStatusSummary();
            } else {
                toastr.error(message, '', { timeOut: 2500 });
            }
        } catch (error) {
            toastr.error('Failed to save backup settings due to a technical error.', '', { timeOut: 1000 });
        } finally {
            _$saveBtn.prop('disabled', false).html('<i class="far fa-save"></i> Save Settings');
        }
    }

    async function testFolder() {
        const path = $.trim(_$folderPathInput.val());
        if (!path) {
            toastr.error('Enter a backup folder path first.', '', { timeOut: 1000 });
            return;
        }

        _$testFolderBtn.prop('disabled', true);
        _$folderTestResult.removeClass('text-success text-danger').text('Testing folder...');

        try {
            const resp = await $.post(TestBackupFolderUrl, { BackupFolderPath: path });
            const { Status: status, Message: message } = resp;
            _$folderTestResult
                .addClass(status ? 'text-success' : 'text-danger')
                .text(message);
            if (status) {
                toastr.success(message, '', { timeOut: 1500 });
            } else {
                toastr.error(message, '', { timeOut: 2500 });
            }
        } catch (error) {
            _$folderTestResult.addClass('text-danger').text('Failed to test the folder due to a technical error.');
            toastr.error('Failed to test the folder due to a technical error.', '', { timeOut: 1000 });
        } finally {
            _$testFolderBtn.prop('disabled', false);
        }
    }

    async function runBackupNow() {
        if (_$backupNowBtn.prop('disabled')) {
            return;
        }

        bootbox.confirm({
            title: 'Run Backup Now',
            message: 'A full database backup will be created immediately in the configured backup folder. Continue?',
            size: 'small',
            buttons: {
                confirm: { label: 'Yes, Back Up Now', className: 'btn-success' },
                cancel: { label: 'Cancel', className: 'btn-secondary' }
            },
            callback: async function (confirmed) {
                if (!confirmed) return;

                _$backupNowBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Backing up...');

                try {
                    const resp = await $.post(RunBackupNowUrl, {});
                    const { Status: status, Message: message } = resp;
                    if (status) {
                        toastr.success(message, '', { timeOut: 2500 });
                    } else {
                        toastr.error(message, '', { timeOut: 4000 });
                    }
                } catch (error) {
                    toastr.error('Failed to start the backup due to a technical error.', '', { timeOut: 1500 });
                } finally {
                    _$backupNowBtn.prop('disabled', false).html('<i class="fas fa-database"></i> Backup Now');
                    reloadHistory();
                    refreshStatusSummary();
                }
            }
        });
    }

    async function refreshStatusSummary() {
        try {
            const resp = await $.post(GetStatusSummaryUrl, {});
            if (!resp.Status || !resp.Data) return;

            const d = resp.Data;
            $('.js-backup-status-badge')
                .attr('class', 'badge js-backup-status-badge ' + (d.StatusBadgeClass || 'badge-secondary'))
                .text(d.StatusText || '');
            $('.js-last-backup-time').text(d.LastBackupTimeFormatted || 'Never');
            $('.js-next-backup-time').text(d.NextBackupTimeFormatted || 'Not scheduled');
            $('.js-latest-backup-file').text((d.LastBackupFileName || 'None') + (d.LastBackupFileSize && d.LastBackupFileSize !== '-' ? ' (' + d.LastBackupFileSize + ')' : ''));
            $('.js-last-success-time').text((d.LastSuccessTime && d.LastSuccessTime !== '0001-01-01T00:00:00') ? formatDateOrNull(d.LastSuccessTime) : 'Never');
            $('.js-folder-path').text(d.Settings && d.Settings.BackupFolderPath ? d.Settings.BackupFolderPath : '');
            $('.js-folder-freespace').text(d.FolderFreeSpace || '-');
            $('.js-folder-error').text(d.FolderError || '');
        } catch (error) {
            // Status refresh is best effort; the page still shows the last loaded values.
        }
    }

    function formatDateOrNull(value) {
        if (!value) return '';
        try {
            if (value.indexOf('T') > -1 && value.indexOf('-') > -1) {
                return value.replace('T', ' ').substring(0, 19);
            }
        } catch (e) { }
        return value;
    }

    function statusBadge(status) {
        switch ((status || '').toLowerCase()) {
            case 'success': return '<span class="badge badge-success">Success</span>';
            case 'failed': return '<span class="badge badge-danger">Failed</span>';
            case 'running': return '<span class="badge badge-warning">Running</span>';
            case 'deleted': return '<span class="badge badge-secondary">Deleted</span>';
            default: return '<span class="badge badge-secondary">' + (status || '') + '</span>';
        }
    }

    function initHistoryTable() {
        const buttonsArray = [
            {
                extend: 'pageLength',
                className: 'btn btn-default',
                titleAttr: 'Show rows',
                text: '<i class="fa fa-ellipsis-v"></i> Show rows',
                init: function (api, node) {
                    $(node).removeClass('dt-button');
                }
            }
        ];

        _$historyTable.DataTable({
            dom: 'Blfrtip',
            lengthChange: false,
            buttons: buttonsArray,
            processing: false,
            serverSide: true,
            searchable: true,
            stateSave: true,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            pageLength: 10,
            paging: true,
            ajax: {
                url: GetPagedBackupHistoryUrl,
                type: 'POST',
                dataSrc: function (json) {
                    json = jQuery.parseJSON(JSON.stringify(json));
                    const d = $.parseJSON(json.data);
                    json.data = d;
                    const return_data = [];
                    for (let i = 0; i < d.length; i++) {
                        const row = d[i];
                        const detailsBtn = `<button class="btn btn-default btn-sm js-details" data-id="${row.ID}" title="View backup details"><i class="fa fa-info-circle"></i></button>`;
                        const downloadBtn = row.Status !== 'Deleted'
                            ? `<a class="btn btn-default btn-sm js-download" href="${DownloadBackupUrl}?id=${row.ID}" title="Download backup file"><i class="fa fa-download"></i></a>`
                            : '';
                        const deleteBtn = (CanManageBackups && row.Status !== 'Deleted' && row.Status !== 'Running')
                            ? `<button class="btn btn-sm btn-danger js-delete" data-id="${row.ID}" title="Delete backup"><i class="fa fa-trash"></i></button>`
                            : '';
                        const restoreBtn = (CanRestoreBackups && row.Status === 'Success')
                            ? `<button class="btn btn-sm btn-warning js-restore" data-id="${row.ID}" data-file="${(row.BackupFileName || '').replace(/"/g, '&quot;')}" title="Restore database from this backup"><i class="fa fa-history"></i> Restore</button>`
                            : '';

                        return_data.push({
                            'BackupFileName': row.BackupFileName,
                            'BackupStartTime': row.BackupStartTime,
                            'FileSize': row.FileSize,
                            'Status': statusBadge(row.Status),
                            'TriggerType': row.TriggerType,
                            'CreatedBy': row.CreatedBy || '',
                            'Actions': `<div class="btn-group-sm action-btns">${detailsBtn} ${downloadBtn} ${deleteBtn} ${restoreBtn}</div>`
                        });
                    }
                    return return_data;
                }
            },
            columns: [
                { data: 'BackupFileName' },
                { data: 'BackupStartTime' },
                { data: 'FileSize' },
                { data: 'Status' },
                { data: 'TriggerType' },
                { data: 'CreatedBy' },
                { data: 'Actions', orderable: false, searchable: false }
            ],
            language: {
                emptyTable: 'No backups recorded yet',
                searchPlaceholder: 'Search by file name'
            },
            drawCallback: function () {
                const pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate');
                pagination.toggle(this.api().page.info().pages > 1);
            }
        });

        $('.dataTables_filter input')
            .attr('title', 'Search by file name')
            .addClass('seachbar-tooltip');
    }

    function reloadHistory() {
        _$historyTable.DataTable().ajax.reload(null, false);
    }

    function bindHistoryActions() {
        _$historyTable.on('click', '.js-details', async function () {
            const id = $(this).data('id');
            try {
                const resp = await $.post(GetBackupDetailsUrl, { ID: id });
                if (!resp.Status || !resp.Data) {
                    toastr.error(resp.Message || 'Failed to load backup details.', '', { timeOut: 1500 });
                    return;
                }
                const d = resp.Data;
                _$detailsModal.find('.js-details-file').text(d.BackupFileName || '');
                _$detailsModal.find('.js-details-location').text(d.BackupLocation || '');
                _$detailsModal.find('.js-details-start').text(d.BackupStartTime || '');
                _$detailsModal.find('.js-details-end').text(d.BackupEndTime || 'Not completed');
                _$detailsModal.find('.js-details-size').text(d.FileSize || '-');
                _$detailsModal.find('.js-details-status').text(d.Status || '');
                _$detailsModal.find('.js-details-trigger').text(d.TriggerType || '');
                _$detailsModal.find('.js-details-user').text(d.CreatedBy || 'System');
                _$detailsModal.find('.js-details-error').text(d.ErrorMessage || 'None');
                _$detailsModal.modal('show');
            } catch (error) {
                toastr.error('Failed to load backup details.', '', { timeOut: 1000 });
            }
        });

        _$historyTable.on('click', '.js-delete', function () {
            const id = $(this).data('id');

            bootbox.confirm({
                title: 'Delete Backup',
                message: 'Do you want to permanently delete this backup file? This cannot be undone.',
                size: 'small',
                buttons: {
                    confirm: { label: 'Delete', className: 'btn-danger' },
                    cancel: { label: 'Cancel', className: 'btn-secondary' }
                },
                callback: async function (result) {
                    if (!result) return;

                    try {
                        const resp = await $.post(DeleteBackupUrl, {
                            ID: id,
                            __RequestVerificationToken: antiForgeryToken()
                        });
                        const { Status: status, Message: message } = resp;
                        if (status) {
                            toastr.success(message, '', { timeOut: 1500 });
                            reloadHistory();
                            refreshStatusSummary();
                        } else {
                            toastr.error(message, '', { timeOut: 2500 });
                        }
                    } catch (error) {
                        toastr.error('Failed to delete the backup due to a technical error.', '', { timeOut: 1000 });
                    }
                }
            });
        });
    }

    function bindRestoreActions() {
        _$historyTable.on('click', '.js-restore', function () {
            const id = $(this).data('id');
            const fileName = $(this).data('file');

            $('#restoreHistoryId').val(id);
            _$restoreModal.find('.js-restore-file').text(fileName || '');
            $('#RestoreConfirmationName').val('');
            _$restoreModal.modal('show');
        });

        $('.js-confirm-restore').on('click', async function () {
            const id = parseInt($('#restoreHistoryId').val(), 10);
            const confirmation = $.trim($('#RestoreConfirmationName').val());
            const dbName = $.trim($('.js-restore-dbname').first().text());

            if (!confirmation) {
                toastr.error('Type the database name "' + dbName + '" to confirm the restore.', '', { timeOut: 2000 });
                return;
            }

            const _$btn = $(this);
            _$btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Restoring...');

            await executeRestore(id, confirmation, _$btn);
        });
    }

    async function executeRestore(id, confirmation, _$btn) {
        try {
            // Verify the file is still a valid backup for this database first.
            const validation = await $.post(ValidateRestoreUrl, { ID: id });
            if (!validation.Status) {
                toastr.error(validation.Message || 'The backup file failed validation.', '', { timeOut: 3500 });
                return;
            }

            const resp = await $.post(RestoreDatabaseUrl, {
                ID: id,
                ConfirmationName: confirmation,
                __RequestVerificationToken: antiForgeryToken()
            });
            const { Status: status, Message: message } = resp;

            if (status) {
                toastr.success(message, '', { timeOut: 5000 });
                _$restoreModal.modal('hide');
                setTimeout(function () {
                    window.location.reload();
                }, 3500);
                return;
            }

            if (message && message.indexOf('FORCE RESTORE') > -1) {
                bootbox.prompt({
                    title: 'Safety backup failed',
                    message: message,
                    inputType: 'text',
                    inputOptions: {},
                    callback: async function (forceText) {
                        if (forceText === null) return;
                        if ($.trim(forceText) !== 'FORCE RESTORE') {
                            toastr.error('Restore cancelled. Nothing was changed.', '', { timeOut: 2000 });
                            return;
                        }
                        await executeRestore(id, 'FORCE RESTORE', _$btn);
                    }
                });
                return;
            }

            toastr.error(message || 'Restore failed.', '', { timeOut: 5000 });
        } catch (error) {
            toastr.error('Failed to restore the database due to a technical error.', '', { timeOut: 1500 });
        } finally {
            _$btn.prop('disabled', false).html('<i class="fas fa-exclamation-circle"></i> Restore Database');
        }
    }
})(jQuery);
