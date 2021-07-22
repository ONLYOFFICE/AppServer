using ASC.Mail.Models;
using ASC.Web.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace ASC.Mail.Controllers
{
    public partial class MailController : ControllerBase
    {
        /// <summary>
        ///    Returns Common Settings
        /// </summary>
        /// <returns>MailCommonSettings object</returns>
        /// <short>Get common settings</short> 
        /// <category>Settings</category>
        [Read(@"settings")]
        public MailCommonSettings GetCommonSettings()
        {
            return SettingEngine.GetCommonSettings();
        }

        /// <summary>
        ///    Returns EnableConversations flag
        /// </summary>
        /// <returns>boolean</returns>
        /// <short>Get EnableConversations flag</short> 
        /// <category>Settings</category>
        [Read(@"settings/conversationsEnabled")]
        public bool GetEnableConversationFlag()
        {
            return SettingEngine.GetEnableConversationFlag();
        }

        /// <summary>
        ///    Set EnableConversations flag
        /// </summary>
        /// <param name="enabled">True or False value</param>
        /// <short>Set EnableConversations flag</short> 
        /// <category>Settings</category>
        [Update(@"settings/conversationsEnabled")]
        public void SetEnableConversationFlag(bool enabled)
        {
            SettingEngine.SetEnableConversationFlag(enabled);
        }

        /// <summary>
        ///    Returns AlwaysDisplayImages flag
        /// </summary>
        /// <returns>boolean</returns>
        /// <short>Get AlwaysDisplayImages flag</short> 
        /// <category>Settings</category>
        [Read(@"settings/alwaysDisplayImages")]
        public bool GetAlwaysDisplayImagesFlag()
        {
            return SettingEngine.GetAlwaysDisplayImagesFlag();
        }

        /// <summary>
        ///    Set AlwaysDisplayImages flag
        /// </summary>
        /// <param name="enabled">True or False value</param>
        /// <short>Set AlwaysDisplayImages flag</short> 
        /// <category>Settings</category>
        [Update(@"settings/alwaysDisplayImages")]
        public void SetAlwaysDisplayImagesFlag(bool enabled)
        {
            SettingEngine.SetAlwaysDisplayImagesFlag(enabled);
        }

        /// <summary>
        ///    Returns CacheUnreadMessages flag
        /// </summary>
        /// <returns>boolean</returns>
        /// <short>Get CacheUnreadMessages flag</short> 
        /// <category>Settings</category>
        [Read(@"settings/cacheMessagesEnabled")]
        public bool GetCacheUnreadMessagesFlag()
        {
            //TODO: Change cache algoritnm and restore it back
            // return SettingEngine.GetCacheUnreadMessagesFlag()

            return false;
        }

        /// <summary>
        ///    Set CacheUnreadMessages flag
        /// </summary>
        /// <param name="enabled">True or False value</param>
        /// <short>Set CacheUnreadMessages flag</short> 
        /// <category>Settings</category>
        [Update(@"settings/cacheMessagesEnabled")]
        public void SetCacheUnreadMessagesFlag(bool enabled)
        {
            SettingEngine.SetCacheUnreadMessagesFlag(enabled);
        }

        /// <summary>
        ///    Returns GoNextAfterMove flag
        /// </summary>
        /// <returns>boolean</returns>
        /// <short>Get GoNextAfterMove flag</short> 
        /// <category>Settings</category>
        [Read(@"settings/goNextAfterMoveEnabled")]
        public bool GetEnableGoNextAfterMoveFlag()
        {
            return SettingEngine.GetEnableGoNextAfterMoveFlag();
        }

        /// <summary>
        ///    Set GoNextAfterMove flag
        /// </summary>
        /// <param name="enabled">True or False value</param>
        /// <short>Set GoNextAfterMove flag</short> 
        /// <category>Settings</category>
        [Update(@"settings/goNextAfterMoveEnabled")]
        public void SetEnableGoNextAfterMoveFlag(bool enabled)
        {
            SettingEngine.SetEnableGoNextAfterMoveFlag(enabled);
        }

        /// <summary>
        ///    Returns ReplaceMessageBody flag
        /// </summary>
        /// <returns>boolean</returns>
        /// <short>Get ReplaceMessageBody flag</short> 
        /// <category>Settings</category>
        [Read(@"settings/replaceMessageBody")]
        public bool GetEnableReplaceMessageBodyFlag()
        {
            return SettingEngine.GetEnableReplaceMessageBodyFlag();
        }

        /// <summary>
        ///    Set ReplaceMessageBody flag
        /// </summary>
        /// <param name="enabled">True or False value</param>
        /// <short>Set ReplaceMessageBody flag</short> 
        /// <category>Settings</category>
        [Update(@"settings/replaceMessageBody")]
        public void SetEnableReplaceMessageBodyFlag(bool enabled)
        {
            SettingEngine.SetEnableReplaceMessageBodyFlag(enabled);
        }
    }
}
