using ASC.Common;
using ASC.Core;
using ASC.Core.Common.Settings;
using ASC.Mail.Models;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class SettingEngine
    {
        private SettingsManager SettingsManager { get; }

        public SettingEngine(SettingsManager settingsManager)
        {
            SettingsManager = settingsManager;
        }

        public MailCommonSettings GetCommonSettings()
        {
            var commonSettings = SettingsManager.LoadForCurrentUser<MailCommonSettings>();

            return commonSettings;
        }

        public bool GetEnableConversationFlag()
        {
            var settings = GetCommonSettings();

            var value = settings.EnableConversationsSetting;

            return value;
        }

        public void SetEnableConversationFlag(bool enabled)
        {
            var settings = GetCommonSettings();

            settings.EnableConversationsSetting = enabled;

            SettingsManager.SaveForCurrentUser(settings);
        }

        public bool GetAlwaysDisplayImagesFlag()
        {
            var settings = GetCommonSettings();

            var value = settings.AlwaysDisplayImagesSetting;

            return value;
        }

        public void SetAlwaysDisplayImagesFlag(bool enabled)
        {
            var settings = GetCommonSettings();

            settings.AlwaysDisplayImagesSetting = enabled;

            SettingsManager.SaveForCurrentUser(settings);
        }

        public bool GetCacheUnreadMessagesFlag()
        {
            //TODO: Change cache algoritnm and restore it back
            /*var settings = GetCommonSettings();

            var value = settings.CacheUnreadMessagesSetting;

            return value;*/

            return false;
        }

        public void SetCacheUnreadMessagesFlag(bool enabled)
        {
            var settings = GetCommonSettings();

            settings.CacheUnreadMessagesSetting = enabled;

            SettingsManager.SaveForCurrentUser(settings);
        }

        public bool GetEnableGoNextAfterMoveFlag()
        {
            var settings = GetCommonSettings();

            var value = settings.EnableGoNextAfterMoveSetting;

            return value;
        }

        public void SetEnableGoNextAfterMoveFlag(bool enabled)
        {
            var settings = GetCommonSettings();

            settings.EnableGoNextAfterMoveSetting = enabled;

            SettingsManager.SaveForCurrentUser(settings);
        }

        public bool GetEnableReplaceMessageBodyFlag()
        {
            var settings = GetCommonSettings();

            var value = settings.ReplaceMessageBodySetting;

            return value;
        }

        public void SetEnableReplaceMessageBodyFlag(bool enabled)
        {
            var settings = GetCommonSettings();

            settings.ReplaceMessageBodySetting = enabled;

            SettingsManager.SaveForCurrentUser(settings);
        }
    }
}
