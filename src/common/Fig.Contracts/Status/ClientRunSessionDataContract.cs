using System;
using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class ClientRunSessionDataContract
    {
        public ClientRunSessionDataContract(Guid runSessionId, DateTime? lastSeen, bool liveReload,
            double pollIntervalMs, DateTime startTimeUtc, string? ipAddress, string? hostname, string figVersion,
            string applicationVersion, bool offlineSettingsEnabled, bool supportsRestart, bool restartRequested,
            bool restartRequiredToApplySettings,
            string runningUser, long memoryUsageBytes, bool hasConfigurationError, 
            MemoryUsageAnalysisDataContract? memoryAnalysis, List<MemoryUsageDataContract> historicalMemoryUsage,
            DateTime lastSettingLoadUtc)
        {
            RunSessionId = runSessionId;
            LastSeen = lastSeen;
            LiveReload = liveReload;
            PollIntervalMs = pollIntervalMs;
            StartTimeUtc = startTimeUtc;
            IpAddress = ipAddress;
            Hostname = hostname;
            FigVersion = figVersion;
            ApplicationVersion = applicationVersion;
            OfflineSettingsEnabled = offlineSettingsEnabled;
            SupportsRestart = supportsRestart;
            RestartRequested = restartRequested;
            RestartRequiredToApplySettings = restartRequiredToApplySettings;
            RunningUser = runningUser;
            MemoryUsageBytes = memoryUsageBytes;
            HasConfigurationError = hasConfigurationError;
            MemoryAnalysis = memoryAnalysis;
            HistoricalMemoryUsage = historicalMemoryUsage;
            LastSettingLoadUtc = lastSettingLoadUtc;
        }

        public Guid RunSessionId { get; }

        public DateTime? LastSeen { get; }

        public bool LiveReload { get; }
        
        public DateTime LastSettingLoadUtc { get; }

        public double PollIntervalMs { get; }

        public DateTime StartTimeUtc { get; }

        public string? IpAddress { get; }

        public string? Hostname { get; }

        public string FigVersion { get; }

        public string ApplicationVersion { get; }

        public bool OfflineSettingsEnabled { get; }

        public bool SupportsRestart { get; }

        public bool RestartRequested { get; }
        
        public bool RestartRequiredToApplySettings { get; }

        public string RunningUser { get; }

        public long MemoryUsageBytes { get; }
        
        public bool HasConfigurationError { get; set; }
        
        public MemoryUsageAnalysisDataContract? MemoryAnalysis { get; }
        
        public List<MemoryUsageDataContract> HistoricalMemoryUsage { get; }
    }
}