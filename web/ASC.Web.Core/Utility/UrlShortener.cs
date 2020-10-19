﻿using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

using ASC.Common;
using ASC.Core.Common.Configuration;
using ASC.FederatedLogin.LoginProviders;
using ASC.Security.Cryptography;
using ASC.Web.Studio.Utility;

using Microsoft.Extensions.Configuration;

namespace ASC.Web.Core.Utility
{
    public interface IUrlShortener
    {
        string GetShortenLink(string shareLink);
    }

    public class UrlShortener
    {
        public bool Enabled { get { return !(Instance is NullShortener); } }

        private IUrlShortener _instance;
        public IUrlShortener Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (ConsumerFactory.Get<BitlyLoginProvider>().Enabled)
                    {
                        _instance = new BitLyShortener(ConsumerFactory);
                    }
                    else if (!string.IsNullOrEmpty(Configuration["web:url-shortener:value"]))
                    {
                        _instance = new OnlyoShortener(Configuration, CommonLinkUtility, MachinePseudoKeys);
                    }
                    else
                    {
                        _instance = new NullShortener();
                    }
                }

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        private IConfiguration Configuration { get; }
        private ConsumerFactory ConsumerFactory { get; }
        private CommonLinkUtility CommonLinkUtility { get; }
        private MachinePseudoKeys MachinePseudoKeys { get; }

        public UrlShortener(
            IConfiguration configuration,
            ConsumerFactory consumerFactory,
            CommonLinkUtility commonLinkUtility,
            MachinePseudoKeys machinePseudoKeys)
        {
            Configuration = configuration;
            ConsumerFactory = consumerFactory;
            CommonLinkUtility = commonLinkUtility;
            MachinePseudoKeys = machinePseudoKeys;
        }
    }

    public class BitLyShortener : IUrlShortener
    {
        public BitLyShortener(ConsumerFactory consumerFactory)
        {
            ConsumerFactory = consumerFactory;
        }

        private ConsumerFactory ConsumerFactory { get; }

        public string GetShortenLink(string shareLink)
        {
            return ConsumerFactory.Get<BitlyLoginProvider>().GetShortenLink(shareLink);
        }
    }

    public class OnlyoShortener : IUrlShortener
    {
        private readonly string url;
        private readonly string internalUrl;
        private readonly byte[] sKey;

        public OnlyoShortener(
            IConfiguration configuration,
            CommonLinkUtility commonLinkUtility,
            MachinePseudoKeys machinePseudoKeys)
        {
            url = configuration["web:url-shortener:value"];
            internalUrl = configuration["web:url-shortener:internal"];
            sKey = machinePseudoKeys.GetMachineConstant();

            if (!url.EndsWith("/"))
                url += '/';
            CommonLinkUtility = commonLinkUtility;
        }

        private CommonLinkUtility CommonLinkUtility { get; }

        public string GetShortenLink(string shareLink)
        {
            using var client = new WebClient { Encoding = Encoding.UTF8 };
            client.Headers.Add("Authorization", CreateAuthToken());
            return CommonLinkUtility.GetFullAbsolutePath(url + client.DownloadString(new Uri(internalUrl + "?url=" + HttpUtility.UrlEncode(shareLink))));
        }

        private string CreateAuthToken(string pkey = "urlShortener")
        {
            using var hasher = new HMACSHA1(sKey);
            var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var hash = Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", now, pkey))));
            return string.Format("ASC {0}:{1}:{2}", pkey, now, hash);
        }
    }

    public class NullShortener : IUrlShortener
    {
        public string GetShortenLink(string shareLink)
        {
            return null;
        }
    }

    public static class UrlShortenerExtension
    {
        public static DIHelper AddUrlShortener(this DIHelper services)
        {
            if (services.TryAddScoped<UrlShortener>())
            {
                return services
                    .AddConsumerFactoryService()
                    .AddCommonLinkUtilityService();
            }

            return services;
        }
    }
}
