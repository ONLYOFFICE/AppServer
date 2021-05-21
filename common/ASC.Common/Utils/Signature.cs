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
using System.Security.Cryptography;
using System.Text;

using ASC.Security.Cryptography;

using Microsoft.AspNetCore.WebUtilities;

using Newtonsoft.Json;

namespace ASC.Common.Utils
{
    [Singletone]
    public class Signature
    {
        public Signature(MachinePseudoKeys machinePseudoKeys)
        {
            MachinePseudoKeys = machinePseudoKeys;
        }

        private MachinePseudoKeys MachinePseudoKeys { get; }

        public string Create<T>(T obj)
        {
            return Create(obj, Encoding.UTF8.GetString(MachinePseudoKeys.GetMachineConstant()));
        }

        public static string Create<T>(T obj, string secret)
        {
            var str = JsonConvert.SerializeObject(obj);
            var payload = GetHashBase64(str + secret) + "?" + str;
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        }

        public T Read<T>(string signature)
        {
            return Read<T>(signature, Encoding.UTF8.GetString(MachinePseudoKeys.GetMachineConstant()));
        }

        public static T Read<T>(string signature, string secret)
        {
            try
            {
                var rightSignature = signature.Replace("\"", "");
                var payloadParts = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(rightSignature)).Split('?');
                if (GetHashBase64(payloadParts[1] + secret) == payloadParts[0])
                {
                    //Sig correct
                    return JsonConvert.DeserializeObject<T>(payloadParts[1]);
                }
            }
            catch (Exception)
            {
            }
            return default;
        }

        private static string GetHashBase64(string str)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }

        private static string GetHashBase64MD5(string str)
        {
            using var md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }
    }
}