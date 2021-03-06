﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitTrends.Shared;
using Xamarin.Essentials.Interfaces;

namespace GitTrends
{
    public class AzureFunctionsApiService : BaseMobileApiService
    {
        readonly IAzureFunctionsApi _azureFunctionsApiClient;

        public AzureFunctionsApiService(IAnalyticsService analyticsService, IMainThread mainThread, IAzureFunctionsApi azureFunctionsApi) : base(analyticsService, mainThread)
        {
            _azureFunctionsApiClient = azureFunctionsApi;
        }

        public Task<GetGitHubClientIdDTO> GetGitHubClientId(CancellationToken cancellationToken) => AttemptAndRetry_Mobile(() => _azureFunctionsApiClient.GetGitTrendsClientId(), cancellationToken);
        public Task<GitHubToken> GenerateGitTrendsOAuthToken(GenerateTokenDTO generateTokenDTO, CancellationToken cancellationToken) => AttemptAndRetry_Mobile(() => _azureFunctionsApiClient.GenerateGitTrendsOAuthToken(generateTokenDTO), cancellationToken);
        public Task<SyncFusionDTO> GetSyncfusionInformation(CancellationToken cancellationToken) => AttemptAndRetry_Mobile(() => _azureFunctionsApiClient.GetSyncfusionInformation(SyncfusionService.AssemblyVersionNumber), cancellationToken);
        public Task<StreamingManifest> GetChartStreamingUrl(CancellationToken cancellationToken) => AttemptAndRetry_Mobile(() => _azureFunctionsApiClient.GetChartStreamingUrl(), cancellationToken);
        public Task<NotificationHubInformation> GetNotificationHubInformation(CancellationToken cancellationToken) => AttemptAndRetry_Mobile(() => _azureFunctionsApiClient.GetNotificationHubInformation(), cancellationToken);
        public Task<IReadOnlyList<NuGetPackageModel>> GetLibraries(CancellationToken cancellationToken) => AttemptAndRetry_Mobile(() => _azureFunctionsApiClient.GetLibraries(), cancellationToken);
    }
}
