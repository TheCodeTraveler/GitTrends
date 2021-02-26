﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitTrends.Mobile.Common;
using GitTrends.Mobile.Common.Constants;
using GitTrends.Shared;
using Newtonsoft.Json;
using Refit;
using Xamarin.Essentials.Interfaces;

namespace GitTrends
{
    public class GitHubGraphQLApiService : BaseMobileApiService
    {
        readonly IGitHubGraphQLApi _githubApiClient;
        readonly GitHubUserService _gitHubUserService;

        public GitHubGraphQLApiService(IMainThread mainThread,
                                        IAnalyticsService analyticsService,
                                        IGitHubGraphQLApi gitHubGraphQLApi,
                                        GitHubUserService gitHubUserService) : base(analyticsService, mainThread)
        {
            _githubApiClient = gitHubGraphQLApi;
            _gitHubUserService = gitHubUserService;
        }

        public async Task<(string login, string name, Uri avatarUri)> GetCurrentUserInfo(CancellationToken cancellationToken)
        {
            var token = await _gitHubUserService.GetGitHubToken().ConfigureAwait(false);
            var data = await ExecuteGraphQLRequest(() => _githubApiClient.ViewerLoginQuery(new ViewerLoginQueryContent(), GetGitHubBearerTokenHeader(token)), cancellationToken).ConfigureAwait(false);

            return (data.Viewer.Alias, data.Viewer.Name, data.Viewer.AvatarUri);
        }

        public async Task<Repository> GetRepository(string repositoryOwner, string repositoryName, CancellationToken cancellationToken)
        {
            var token = await _gitHubUserService.GetGitHubToken().ConfigureAwait(false);

            var repositoryQueryTask = ExecuteGraphQLRequest(() => _githubApiClient.RepositoryQuery(new RepositoryQueryContent(repositoryOwner, repositoryName), GetGitHubBearerTokenHeader(token)), cancellationToken);
            var starGazersQueryTask = GetStarGazers(repositoryName, repositoryOwner, cancellationToken);

            await Task.WhenAll(repositoryQueryTask, starGazersQueryTask).ConfigureAwait(false);

            var starGazersResult = await starGazersQueryTask.ConfigureAwait(false);
            var repositoryResult = await repositoryQueryTask.ConfigureAwait(false);

            return new Repository(repositoryResult.Repository.Name,
                                    repositoryResult.Repository.Description,
                                    repositoryResult.Repository.ForkCount,
                                    repositoryResult.Repository.Owner.Login,
                                    repositoryResult.Repository.Owner.AvatarUrl,
                                    repositoryResult.Repository.Issues.IssuesCount,
                                    repositoryResult.Repository.Watchers.TotalCount,
                                    repositoryResult.Repository.Url.ToString(),
                                    repositoryResult.Repository.IsFork,
                                    DateTimeOffset.UtcNow,
                                    starredAt: starGazersResult.StarredAt.Select(x => x.StarredAt));
        }

        public async Task<StarGazers> GetStarGazers(string repositoryName, string repositoryOwner, CancellationToken cancellationToken)
        {
            if (_gitHubUserService.IsDemoUser)
            {
                var starCount = DemoDataConstants.GetRandomNumber();
                var starredAtDates = DemoDataConstants.GenerateStarredAtDates(starCount);

                var starGazerInfoList = starredAtDates.Select(x => new StarGazerInfo(x, string.Empty));

                return new StarGazers(starCount, starGazerInfoList);
            }
            else
            {
                var starGazerInfoList = new List<StarGazerInfo>();

                await foreach (var starGazerResponse in GetStarGazers(repositoryName, repositoryOwner, cancellationToken, 100).ConfigureAwait(false))
                {
                    starGazerInfoList.AddRange(starGazerResponse.StarredAt);
                }

                return new StarGazers(starGazerInfoList.Count, starGazerInfoList);
            }
        }

        public async IAsyncEnumerable<Repository> GetRepositories(string repositoryOwner, [EnumeratorCancellation] CancellationToken cancellationToken, int numberOfRepositoriesPerRequest = 100)
        {
            if (_gitHubUserService.IsDemoUser)
            {
                //Yield off of main thread to generate the demoDataList
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                for (int i = 0; i < DemoDataConstants.RepoCount; i++)
                {
                    var demoRepo = new Repository($"Repository " + DemoDataConstants.GetRandomText(), DemoDataConstants.GetRandomText(), DemoDataConstants.GetRandomNumber(),
                                                DemoUserConstants.Alias, _gitHubUserService.AvatarUrl,
                                                DemoDataConstants.GetRandomNumber(), DemoDataConstants.GetRandomNumber(), _gitHubUserService.AvatarUrl, false, DateTimeOffset.UtcNow);
                    yield return demoRepo;
                }

                //Allow UI to update
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            else
            {
                RepositoryConnection? repositoryConnection = null;

                do
                {
                    repositoryConnection = await GetRepositoryConnection(repositoryOwner, repositoryConnection?.PageInfo?.EndCursor, cancellationToken, numberOfRepositoriesPerRequest).ConfigureAwait(false);

                    foreach (var repository in repositoryConnection.RepositoryList)
                    {
                        if (repository is not null)
                            yield return new Repository(repository.Name, repository.Description, repository.ForkCount, repository.Owner.Login, repository.Owner.AvatarUrl,
                                                        repository.Issues.IssuesCount, repository.Watchers.TotalCount, repository.Url.ToString(), repository.IsFork, repository.DataDownloadedAt);
                    }
                }
                while (repositoryConnection?.PageInfo?.HasNextPage is true);
            }
        }

        static string GetEndCursorString(string? endCursor) => string.IsNullOrWhiteSpace(endCursor) ? string.Empty : "after: \"" + endCursor + "\"";

        async IAsyncEnumerable<StarGazers> GetStarGazers(string repositoryName, string repositoryOwner, [EnumeratorCancellation] CancellationToken cancellationToken, int numberOfStarGazersPerRequest = 100)
        {
            StarGazerResponse? starGazerResponse = null;

            var token = await _gitHubUserService.GetGitHubToken().ConfigureAwait(false);

            do
            {
                var endCursor = GetEndCursorString(starGazerResponse?.Repository.StarGazers.StarredAt.LastOrDefault()?.Cursor);
                starGazerResponse = await ExecuteGraphQLRequest(() => _githubApiClient.StarGazerQuery(new StarGazerQueryContent(repositoryName, repositoryOwner, endCursor, numberOfStarGazersPerRequest), GetGitHubBearerTokenHeader(token)), cancellationToken).ConfigureAwait(false);

                if (starGazerResponse?.Repository.StarGazers != null)
                    yield return starGazerResponse.Repository.StarGazers;

            } while (starGazerResponse?.Repository.StarGazers.StarredAt.Count == numberOfStarGazersPerRequest);
        }

        async Task<RepositoryConnection> GetRepositoryConnection(string repositoryOwner, string? endCursor, CancellationToken cancellationToken, int numberOfRepositoriesPerRequest = 100)
        {
            RepositoryConnectionResponse repositoryConnectionResponse;

            var token = await _gitHubUserService.GetGitHubToken().ConfigureAwait(false);

            try
            {
                repositoryConnectionResponse = await ExecuteGraphQLRequest(() => _githubApiClient.RepositoryConnectionQuery(new RepositoryConnectionQueryContent(repositoryOwner, GetEndCursorString(endCursor), numberOfRepositoriesPerRequest), GetGitHubBearerTokenHeader(token)), cancellationToken).ConfigureAwait(false);
            }
            catch (GraphQLException<RepositoryConnectionResponse> e) when (e.GraphQLData != null && e.ContainsSamlOrganizationAthenticationError())
            {
                repositoryConnectionResponse = e.GraphQLData;
            }

            return repositoryConnectionResponse.GitHubUser.RepositoryConnection;
        }

        async Task<T> ExecuteGraphQLRequest<T>(Func<Task<ApiResponse<GraphQLResponse<T>>>> action, CancellationToken cancellationToken, int numRetries = 2, [CallerMemberName] string callerName = "")
        {
            var response = await AttemptAndRetry_Mobile(action, cancellationToken, numRetries, callerName: callerName).ConfigureAwait(false);

            await response.EnsureSuccessStatusCodeAsync().ConfigureAwait(false);

            if (response?.Content?.Errors != null)
                throw new GraphQLException<T>(response.Content.Data, response.Content.Errors, response.StatusCode, response.Headers);

            if (response?.Content is null)
                throw new JsonSerializationException();

            return response.Content.Data;
        }
    }

    public static class GraphQLExceptionExtensions
    {
        public static bool ContainsSamlOrganizationAthenticationError<T>(this GraphQLException<T> graphQLException) => graphQLException.Errors.Any(x => x.Message.Contains("SAML", StringComparison.OrdinalIgnoreCase) && x.Message.Contains("organization", StringComparison.OrdinalIgnoreCase));
    }
}
