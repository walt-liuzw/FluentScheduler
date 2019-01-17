using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Beisen.Configuration;

namespace JobwsClient.Config
{
    public static class RedisConfig
    {
        public static readonly int UPaasSystemTenantId = 100002;

        public static string RedisKeyspace = "UPaaSRemoteCache";

        public static string TotalUseTimerTenantIds { get; internal set; } = "TotalUseTimerTenantIds";
    }
}
