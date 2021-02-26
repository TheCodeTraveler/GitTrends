﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using GitTrends.Shared;

namespace GitTrends
{
    public class AppInitializationService
    {
        readonly static WeakEventManager<InitializationCompleteEventArgs> _initializationCompletedEventManager = new();

        readonly ThemeService _themeService;
        readonly LanguageService _languageService;
        readonly LibrariesService _librariesService;
        readonly IAnalyticsService _analyticsService;
        readonly SyncfusionService _syncfusionService;
        readonly MediaElementService _mediaElementService;
        readonly NotificationService _notificationService;
        readonly GitTrendsStatisticsService _gitTrendsStatisticsService;
        readonly IDeviceNotificationsService _deviceNotificationsService;

        public AppInitializationService(ThemeService themeService,
                                        LanguageService languageService,
                                        LibrariesService librariesService,
                                        IAnalyticsService analyticsService,
                                        SyncfusionService syncFusionService,
                                        MediaElementService mediaElementService,
                                        NotificationService notificationService,
                                        GitTrendsStatisticsService gitTrendsStatisticsService,
                                        IDeviceNotificationsService deviceNotificationService)
        {
            _themeService = themeService;
            _languageService = languageService;
            _librariesService = librariesService;
            _analyticsService = analyticsService;
            _syncfusionService = syncFusionService;
            _mediaElementService = mediaElementService;
            _notificationService = notificationService;
            _deviceNotificationsService = deviceNotificationService;
            _gitTrendsStatisticsService = gitTrendsStatisticsService;
        }

        public static event EventHandler<InitializationCompleteEventArgs> InitializationCompleted
        {
            add => _initializationCompletedEventManager.AddEventHandler(value);
            remove => _initializationCompletedEventManager.RemoveEventHandler(value);
        }

        public bool IsInitializationComplete { get; private set; }

        public async Task<bool> InitializeApp(CancellationToken cancellationToken)
        {
            bool isInitializationSuccessful = false;

            try
            {
                #region First, Initialize Services That Dont Require API Response
                _languageService.Initialize();
                _deviceNotificationsService.Initialize();
                await _themeService.Initialize().ConfigureAwait(false);
                #endregion

                #region Then, Initialize Services Requiring API Response
                var initializeSyncFusionServiceTask = _syncfusionService.Initialize(cancellationToken);
                var intializeOnboardingChartValueTask = _mediaElementService.InitializeOnboardingChart(cancellationToken);
                var initializeLibrariesServiceValueTask = _librariesService.Initialize(cancellationToken);
                var initializeNotificationServiceValueTask = _notificationService.Initialize(cancellationToken);
                var initializeGitTrendsStatisticsValueTask = _gitTrendsStatisticsService.Initialize(cancellationToken);
#if DEBUG
                initializeSyncFusionServiceTask.SafeFireAndForget(ex => _analyticsService.Report(ex));
                initializeLibrariesServiceValueTask.SafeFireAndForget(ex => _analyticsService.Report(ex));
                initializeNotificationServiceValueTask.SafeFireAndForget(ex => _analyticsService.Report(ex));
#else
                await initializeSyncFusionServiceTask.ConfigureAwait(false);
                await initializeLibrariesServiceValueTask.ConfigureAwait(false);
                await initializeNotificationServiceValueTask.ConfigureAwait(false);
#endif

                await intializeOnboardingChartValueTask.ConfigureAwait(false);
                await initializeGitTrendsStatisticsValueTask.ConfigureAwait(false);
                #endregion

                isInitializationSuccessful = true;
            }
            catch (Exception e)
            {
                _analyticsService.Report(e);
            }
            finally
            {
                OnInitializationCompleted(isInitializationSuccessful);
            }

            return isInitializationSuccessful;
        }

        void OnInitializationCompleted(bool isInitializationSuccessful)
        {
            IsInitializationComplete = isInitializationSuccessful;
            _initializationCompletedEventManager.RaiseEvent(this, new InitializationCompleteEventArgs(isInitializationSuccessful), nameof(InitializationCompleted));
        }
    }
}
