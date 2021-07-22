/*
 *
 * (c) Copyright Ascensio System Limited 2010-2020
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;
using System.Collections.Generic;
using System.Drawing;
using ASC.Web.Core;
using ASC.Web.Core.Utility.Skins;
using ASC.Web.Files.Classes;

namespace ASC.Mail.Storage
{
    public static class ContactPhotoManager
    {
        #region Members

        private const string PHOTOS_BASE_DIR_NAME = "photos";
        private static readonly Dictionary<int, IDictionary<Size, string>> PhotoCache = new Dictionary<int, IDictionary<Size, string>>();

        private static readonly Size BigSize = new Size(200, 200);
        private static readonly Size MediumSize = new Size(82, 82);
        private static readonly Size SmallSize = new Size(40, 40);

        private static readonly object Locker = new object();

        #endregion

        #region Get Photo Methods

        public static string GetSmallSizePhoto(GlobalStore globalStore, WebImageSupplier webImageSupplier, int contacId)
        {
            return contacId <= 0 
                ? GetDefaultPhoto(webImageSupplier, SmallSize) 
                : GetPhotoUri(globalStore, webImageSupplier, contacId, SmallSize);
        }

        public static string GetMediumSizePhoto(GlobalStore globalStore, WebImageSupplier webImageSupplier, int contacId)
        {
            return contacId <= 0 
                ? GetDefaultPhoto(webImageSupplier, MediumSize) 
                : GetPhotoUri(globalStore, webImageSupplier, contacId, MediumSize);
        }

        public static string GetBigSizePhoto(GlobalStore globalStore, WebImageSupplier webImageSupplier, int contacId)
        {
            return contacId <= 0 
                ? GetDefaultPhoto(webImageSupplier, BigSize) 
                : GetPhotoUri(globalStore, webImageSupplier, contacId, BigSize);
        }

        #endregion

        #region Private Methods

        private static string GetDefaultPhoto(WebImageSupplier webImageSupplier, Size photoSize)
        {
            const int contacе_id = -1;

            var defaultPhotoUri = FromCache(contacе_id, photoSize);

            if (!string.IsNullOrEmpty(defaultPhotoUri)) return defaultPhotoUri;

            defaultPhotoUri = webImageSupplier.GetAbsoluteWebPath(
                string.Format("empty_people_logo_{0}_{1}.png", 
                photoSize.Height, photoSize.Width), 
                WebItemManager.MailProductID);

            ToCache(contacе_id, defaultPhotoUri, photoSize);

            return defaultPhotoUri;
        }

        private static string GetPhotoUri(GlobalStore globalStore, WebImageSupplier webImageSupplier, int contactId, Size photoSize)
        {
            var photoUri = FromCache(contactId, photoSize);

            if (!string.IsNullOrEmpty(photoUri)) return photoUri;

            photoUri = FromDataStore(globalStore, contactId, photoSize);

            if (string.IsNullOrEmpty(photoUri))
                photoUri = GetDefaultPhoto(webImageSupplier, photoSize);

            ToCache(contactId, photoUri, photoSize);

            return photoUri;
        }

        private static string BuildFileDirectory(int contactId)
        {
            var s = contactId.ToString("000000");

            return string.Concat(PHOTOS_BASE_DIR_NAME, "/", s.Substring(0, 2), "/",
                                 s.Substring(2, 2), "/",
                                 s.Substring(4), "/");
        }

        private static string BuildFileName(int contactId, Size photoSize)
        {
            return string.Format("contact_{0}_{1}_{2}", contactId, photoSize.Width, photoSize.Height);
        }

        #endregion

        #region Cache and DataStore Methods

        private static string FromCache(int contactId, Size photoSize)
        {
            lock (Locker)
            {
                if (PhotoCache.ContainsKey(contactId))
                {
                    if (PhotoCache[contactId].ContainsKey(photoSize))
                    {
                        return PhotoCache[contactId][photoSize];
                    }
                }
            }

            return string.Empty;
        }

        private static void ToCache(int contactId, string photoUri, Size photoSize)
        {
            lock (Locker)
            {
                if (PhotoCache.ContainsKey(contactId))
                    if (PhotoCache[contactId].ContainsKey(photoSize))
                        PhotoCache[contactId][photoSize] = photoUri;
                    else
                        PhotoCache[contactId].Add(photoSize, photoUri);
                else
                    PhotoCache.Add(contactId, new Dictionary<Size, string> { { photoSize, photoUri } });
            }
        }

        private static string FromDataStore(GlobalStore globalStore, int contactId, Size photoSize)
        {
            var directoryPath = BuildFileDirectory(contactId);

            var filesUri = globalStore.GetStore().ListFiles(directoryPath, BuildFileName(contactId, photoSize) + "*", false);

            return filesUri.Length == 0 ? String.Empty : filesUri[0].ToString();
        }

        #endregion
    }
}
