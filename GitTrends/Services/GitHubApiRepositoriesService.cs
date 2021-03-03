﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitTrends.Shared;
using Refit;

namespace GitTrends
{
    public class GitHubApiRepositoriesService
    {
        readonly IAnalyticsService _analyticsService;
        readonly GitHubUserService _gitHubUserService;
        readonly GitHubApiV3Service _gitHubApiV3Service;
        readonly GitHubGraphQLApiService _gitHubGraphQLApiService;

        public GitHubApiRepositoriesService(IAnalyticsService analyticsService,
                                            GitHubUserService gitHubUserService,
                                            GitHubApiV3Service gitHubApiV3Service,
                                            GitHubGraphQLApiService gitHubGraphQLApiService)
        {
            _analyticsService = analyticsService;
            _gitHubUserService = gitHubUserService;
            _gitHubApiV3Service = gitHubApiV3Service;
            _gitHubGraphQLApiService = gitHubGraphQLApiService;
        }

        public async IAsyncEnumerable<Repository> UpdateRepositoriesWithViewsClonesAndStarsData(IReadOnlyList<Repository> repositories, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var getRepositoryStatisticsTaskList = new List<Task<(RepositoryViewsResponseModel?, RepositoryClonesResponseModel?, StarGazers?)>>(repositories.Select(x => GetRepositoryStatistics(x, cancellationToken)));

            while (getRepositoryStatisticsTaskList.Any())
            {
                var completedStatisticsTask = await Task.WhenAny(getRepositoryStatisticsTaskList).ConfigureAwait(false);
                getRepositoryStatisticsTaskList.Remove(completedStatisticsTask);

                var (viewsResponse, clonesResponse, starGazers) = await completedStatisticsTask.ConfigureAwait(false);

                if (viewsResponse != null && clonesResponse != null && starGazers != null)
                {
                    var updatedRepository = repositories.Single(x => x.Name == viewsResponse.RepositoryName) with
                    {
                        DailyViewsList = viewsResponse.DailyViewsList,
                        DailyClonesList = clonesResponse.DailyClonesList,
                        StarredAt = starGazers.StarredAt.Select(x => x.StarredAt).ToList()
                    };

                    yield return updatedRepository;
                }
            }
        }

        async Task<(RepositoryViewsResponseModel? ViewsResponse, RepositoryClonesResponseModel? ClonesResponse, StarGazers? StarGazerResponse)> GetRepositoryStatistics(Repository repository, CancellationToken cancellationToken)
        {
            var getViewStatisticsTask = _gitHubApiV3Service.GetRepositoryViewStatistics(repository.OwnerLogin, repository.Name, cancellationToken);
            var getCloneStatisticsTask = _gitHubApiV3Service.GetRepositoryCloneStatistics(repository.OwnerLogin, repository.Name, cancellationToken);
            var getStarGazrsTask = _gitHubGraphQLApiService.GetStarGazers(repository.Name, repository.OwnerLogin, cancellationToken);

            try
            {
                await Task.WhenAll(getViewStatisticsTask, getCloneStatisticsTask, getStarGazrsTask).ConfigureAwait(false);

                return (await getViewStatisticsTask.ConfigureAwait(false),
                        await getCloneStatisticsTask.ConfigureAwait(false),
                        await getStarGazrsTask.ConfigureAwait(false));
            }
            catch (ApiException e) when (e.StatusCode is System.Net.HttpStatusCode.Forbidden)
            {
                reportException(e);

                return (null, null, null);
            }
            catch (GraphQLException<StarGazers> e) when (e.ContainsSamlOrganizationAthenticationError())
            {
                reportException(e);

                return (null, null, null);
            }

            void reportException(in Exception e)
            {
                _analyticsService.Report(e, new Dictionary<string, string>
                {
                    { nameof(Repository) + nameof(Repository.Name), repository.Name },
                    { nameof(Repository) + nameof(Repository.OwnerLogin), repository.OwnerLogin },
                    { nameof(GitHubUserService) + nameof(GitHubUserService.Alias), _gitHubUserService.Alias },
                    { nameof(GitHubUserService) + nameof(GitHubUserService.Name), _gitHubUserService.Name },
                });
            }
        }
    }
}