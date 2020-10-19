﻿
using System;
using System.Collections.Generic;

using ASC.Api.Core.Middleware;
using ASC.Common;
using ASC.Core;
using ASC.Data.Backup.Contracts;
using ASC.Data.Backup.Models;
using ASC.Web.Api.Routing;
using ASC.Web.Studio.Utility;

using Microsoft.AspNetCore.Mvc;

namespace ASC.Data.Backup.Controllers
{
    [DefaultRoute]
    [ApiController]
    public class BackupController
    {
        private BackupAjaxHandler BackupHandler { get; }
        private CoreBaseSettings CoreBaseSettings { get; }
        private TenantExtra TenantExtra { get; }

        public BackupController(
            BackupAjaxHandler backupAjaxHandler,
            CoreBaseSettings coreBaseSettings,
            TenantExtra tenantExtra)
        {
            BackupHandler = backupAjaxHandler;
            CoreBaseSettings = coreBaseSettings;
            TenantExtra = tenantExtra;
        }
        /// <summary>
        /// Returns the backup schedule of the current portal
        /// </summary>
        /// <category>Backup</category>
        /// <returns>Backup Schedule</returns>
        [Read("getbackupschedule")]
        public BackupAjaxHandler.Schedule GetBackupSchedule()
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            return BackupHandler.GetSchedule();
        }

        /// <summary>
        /// Create the backup schedule of the current portal
        /// </summary>
        /// <param name="storageType">Storage type</param>
        /// <param name="storageParams">Storage parameters</param>
        /// <param name="backupsStored">Max of the backup's stored copies</param>
        /// <param name="cronParams">Cron parameters</param>
        /// <param name="backupMail">Include mail in the backup</param>
        /// <category>Backup</category>
        [Create("createbackupschedule")]
        public void CreateBackupSchedule(BackupStorageType storageType, [FromQuery] Dictionary<string, string> storageParams, int backupsStored, [FromBody] BackupAjaxHandler.CronParams cronParams, bool backupMail)
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            BackupHandler.CreateSchedule(storageType, storageParams, backupsStored, cronParams, backupMail);
        }

        /// <summary>
        /// Delete the backup schedule of the current portal
        /// </summary>
        /// <category>Backup</category>
        [Delete("deletebackupschedule")]
        public void DeleteBackupSchedule()
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            BackupHandler.DeleteSchedule();
        }

        /// <summary>
        /// Start a backup of the current portal
        /// </summary>
        /// <param name="storageType">Storage Type</param>
        /// <param name="storageParams">Storage Params</param>
        /// <param name="backupMail">Include mail in the backup</param>
        /// <category>Backup</category>
        /// <returns>Backup Progress</returns>
        [Create("startbackup")]
        public BackupProgress StartBackup(Models.Backup backup)
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            BackupHandler.StartBackup(backup.StorageType, backup.StorageParams ?? new Dictionary<string, string>(), backup.BackupMail);
            return BackupHandler.GetBackupProgress();
        }

        /// <summary>
        /// Returns the progress of the started backup
        /// </summary>
        /// <category>Backup</category>
        /// <returns>Backup Progress</returns>
        [Read("getbackupprogress")]
        public BackupProgress GetBackupProgress()
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            return BackupHandler.GetBackupProgress();
        }

        /// <summary>
        /// Returns the backup history of the started backup
        /// </summary>
        /// <category>Backup</category>
        /// <returns>Backup History</returns>
        [Read("getbackuphistory")]
        public List<BackupHistoryRecord> GetBackupHistory()
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            return BackupHandler.GetBackupHistory();
        }

        /// <summary>
        /// Delete the backup with the specified id
        /// </summary>
        /// <category>Backup</category>
        [Delete("deletebackup/{id}")]
        public void DeleteBackup(Guid id)
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            BackupHandler.DeleteBackup(id);
        }

        /// <summary>
        /// Delete all backups of the current portal
        /// </summary>
        /// <category>Backup</category>
        /// <returns>Backup History</returns>
        [Delete("deletebackuphistory")]
        public void DeleteBackupHistory()
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            BackupHandler.DeleteAllBackups();
        }

        /// <summary>
        /// Start a data restore of the current portal
        /// </summary>
        /// <param name="backupId">Backup Id</param>
        /// <param name="storageType">Storage Type</param>
        /// <param name="storageParams">Storage Params</param>
        /// <param name="notify">Notify about backup to users</param>
        /// <category>Backup</category>
        /// <returns>Restore Progress</returns>
        [Create("startrestore")]
        public BackupProgress StartBackupRestore(BackupRestore backupRestore)
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            BackupHandler.StartRestore(backupRestore.BackupId, backupRestore.StorageType, backupRestore.StorageParams, backupRestore.Notify);
            return BackupHandler.GetBackupProgress();
        }

        /// <summary>
        /// Returns the progress of the started restore
        /// </summary>
        /// <category>Backup</category>
        /// <returns>Restore Progress</returns>
        [Read("getrestoreprogress", true)]  //NOTE: this method doesn't check payment!!!
        public BackupProgress GetRestoreProgress()
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            return BackupHandler.GetRestoreProgress();
        }

        ///<visible>false</visible>
        [Read("backuptmp")]
        public object GetTempPath()
        {
            if (CoreBaseSettings.Standalone)
            {
                TenantExtra.DemandControlPanelPermission();
            }

            return BackupHandler.GetTmpFolder();
        }
    }
    public static class BackupControllerExtension
    {
        public static DIHelper AddBackupController(this DIHelper services)
        {
            return services
                .AddBackupAjaxHandler()
                .AddIpSecurityFilter()
                .AddCoreBaseSettingsService()
                .AddTenantExtraService();
        }
    }
}
