/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
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
using System.IO;
using System.Text.Json;

namespace ASC.Core.ChunkedUploader
{
    public class CommonChunkedUploadSession : ICloneable
    {
        public string Id { get; set; }

        public DateTime Created { get; set; }

        public DateTime Expired { get; set; }

        public string Location { get; set; }

        public long BytesUploaded { get; set; }

        public long BytesTotal { get; set; }

        public int TenantId { get; set; }

        public Guid UserId { get; set; }

        public bool UseChunks { get; set; }

        public string CultureName { get; set; }

        public Dictionary<string, object> Items = new Dictionary<string, object>();

        private const string TempPathKey = "TempPath";
        public string TempPath
        {
            get { return GetItemOrDefault<string>(TempPathKey); }
            set { Items[TempPathKey] = value; }
        }

        private const string UploadIdKey = "UploadId";
        public string UploadId
        {
            get { return GetItemOrDefault<string>(UploadIdKey); }
            set { Items[UploadIdKey] = value; }
        }

        private const string ChunksBufferKey = "ChunksBuffer";
        public string ChunksBuffer
        {
            get { return GetItemOrDefault<string>(ChunksBufferKey); }
            set { Items[ChunksBufferKey] = value; }
        }

        public CommonChunkedUploadSession(long bytesTotal)
        {
            Id = Guid.NewGuid().ToString("N");
            Created = DateTime.UtcNow;
            BytesUploaded = 0;
            BytesTotal = bytesTotal;
            UseChunks = true;
        }

        public T GetItemOrDefault<T>(string key)
        {
            return Items.ContainsKey(key) && Items[key] is T t ? t : default;
        }

        public virtual object Clone()
        {
            return (CommonChunkedUploadSession)MemberwiseClone();
        }

        public static CommonChunkedUploadSession Deserialize(Stream stream)
        {
            var sr = new StreamReader(stream);
            string str = sr.ReadToEnd();
            var property = JsonSerializer.Deserialize<Property>(str);
            return property.Deserialize();
        }

        [Serializable]
        public class Property
        {
            public string Id { get; set; }

            public DateTime Created { get; set; }

            public DateTime Expired { get; set; }

            public string Location { get; set; }

            public long BytesUploaded { get; set; }

            public long BytesTotal { get; set; }

            public int TenantId { get; set; }

            public Guid UserId { get; set; }

            public bool UseChunks { get; set; }

            public string CultureName { get; set; }

            public Dictionary<string, object> Items { get; set; }

            public CommonChunkedUploadSession Deserialize()
            {
                var chunk = new CommonChunkedUploadSession(BytesTotal);
                Initialization(chunk);
                return chunk;
            }

            private Dictionary<string, object> GetItems()
            {
                var items = new Dictionary<string, object>();
                foreach (var item in Items)
                {
                    if (item.Value.GetType() == typeof(JsonElement))
                    {
                        var value = (JsonElement)item.Value;
                        if (value.ValueKind == JsonValueKind.String)
                        {
                            items.Add(item.Key, item.Value.ToString());
                        }
                    }
                    else
                    {
                        items.Add(item.Key, item.Value);
                    }
                }
                return items;
            }

            private void Initialization(CommonChunkedUploadSession chunk)
            {
                chunk.Id = Id;
                chunk.Created = Created;
                chunk.Expired = Expired;
                chunk.Location = Location;
                chunk.BytesUploaded = BytesUploaded;
                chunk.TenantId = TenantId;
                chunk.UserId = UserId;
                chunk.UseChunks = UseChunks;
                chunk.CultureName = CultureName;
                chunk.Items = GetItems();
            }
        }
    }
}
