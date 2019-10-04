﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using GitTrends.Shared;
using Refit;
using Xamarin.Forms;

namespace GitTrends
{
    public class RepositoryViewModel : BaseViewModel
    {
        readonly WeakEventManager<PullToRefreshFailedEventArgs> _pullToRefreshFailedEventManager = new WeakEventManager<PullToRefreshFailedEventArgs>();
        readonly RepositoryDatabase _repositoryDatabase;
        readonly GitHubAuthenticationService _gitHubAuthenticationService;
        readonly GitHubGraphQLApiService _gitHubGraphQLApiService;

        bool _isRefreshing;
        string _searchBarText = "";
        IReadOnlyList<Repository> _repositoryList = Enumerable.Empty<Repository>().ToList();

        public RepositoryViewModel(RepositoryDatabase repositoryDatabase,
                                    GitHubAuthenticationService gitHubAuthenticationService,
                                    GitHubGraphQLApiService gitHubGraphQLApiService)
        {
            //Make ObservableCollection Thread-Safe
            BindingBase.EnableCollectionSynchronization(VisibleRepositoryCollection, null, ObservableCollectionCallback);

            _repositoryDatabase = repositoryDatabase;
            _gitHubAuthenticationService = gitHubAuthenticationService;
            _gitHubGraphQLApiService = gitHubGraphQLApiService;

            PullToRefreshCommand = new AsyncCommand(() => ExecutePullToRefreshCommand(_gitHubAuthenticationService.Alias));
            FilterRepositoriesCommand = new Command<string>(text => SetSearchBarText(text));
        }

        public event EventHandler<PullToRefreshFailedEventArgs> PullToRefreshFailed
        {
            add => _pullToRefreshFailedEventManager.AddEventHandler(value);
            remove => _pullToRefreshFailedEventManager.RemoveEventHandler(value);
        }

        public ICommand PullToRefreshCommand { get; }
        public ICommand FilterRepositoriesCommand { get; }

        public ObservableRangeCollection<Repository> VisibleRepositoryCollection { get; } = new ObservableRangeCollection<Repository>();

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        async Task ExecutePullToRefreshCommand(string repositoryOwner)
        {
            await Task.Yield();

            try
            {
                var repositoryList = await _gitHubGraphQLApiService.GetRepositories(repositoryOwner).ConfigureAwait(false);

                SetRepositoriesCollection(repositoryList, repositoryOwner, _searchBarText);

                _repositoryDatabase.SaveRepositories(repositoryList).SafeFireAndForget();
            }
            catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized)
            {
                OnPullToRefreshFailed("Login Expired", "Login again");

                await _gitHubAuthenticationService.LogOut().ConfigureAwait(false);
                await _repositoryDatabase.DeleteAllData().ConfigureAwait(false);

                VisibleRepositoryCollection.Clear();
            }
            catch (ApiException)
            {
                var repositoryList = await _repositoryDatabase.GetRepositories().ConfigureAwait(false);

                SetRepositoriesCollection(repositoryList, repositoryOwner, _searchBarText);
            }
            catch (Exception e)
            {
                OnPullToRefreshFailed("Error", e.Message);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        void SetRepositoriesCollection(in IEnumerable<Repository> repositories, string repositoryOwner, string searchBarText)
        {
            _repositoryList = repositories.Where(x => x.OwnerLogin.Equals(repositoryOwner, StringComparison.InvariantCultureIgnoreCase)).OrderByDescending(x => x.StarCount).ToList();

            var filteredRepositoryList = GetRepositoriesFilteredBySearchBar(_repositoryList, searchBarText);

            VisibleRepositoryCollection.Clear();
            VisibleRepositoryCollection.AddRange(filteredRepositoryList);
        }

        void AddRepositoriesToCollection(in IEnumerable<Repository> repositories, string repositoryOwner, string searchBarText)
        {
            _repositoryList = _repositoryList.Concat(GetOwnersRepositories(repositories, repositoryOwner)).ToList();

            var filteredRepositoryList = GetRepositoriesFilteredBySearchBar(_repositoryList, searchBarText);

            VisibleRepositoryCollection.AddRange(filteredRepositoryList);
        }

        IEnumerable<Repository> GetOwnersRepositories(in IEnumerable<Repository> repositories, string repositoryOwner) =>
            repositories.Where(x => x.OwnerLogin.Equals(repositoryOwner, StringComparison.InvariantCultureIgnoreCase)).OrderByDescending(x => x.StarCount);

        IEnumerable<Repository> GetRepositoriesFilteredBySearchBar(in IEnumerable<Repository> repositories, string searchBarText)
        {
            if (string.IsNullOrWhiteSpace(searchBarText))
                return repositories;

            return repositories.Where(x => x.Name.Contains(searchBarText));
        }

        void SetSearchBarText(in string text)
        {
            if (EqualityComparer<string>.Default.Equals(_searchBarText, text))
                return;

            _searchBarText = text;

            if (_repositoryList.Any())
                SetRepositoriesCollection(_repositoryList, _gitHubAuthenticationService.Alias, text);
        }

        void ObservableCollectionCallback(IEnumerable collection, object context, Action accessMethod, bool writeAccess)
        {
            lock (collection)
            {
                accessMethod?.Invoke();
            }
        }

        void OnPullToRefreshFailed(in string title, in string message) =>
            _pullToRefreshFailedEventManager.HandleEvent(this, new PullToRefreshFailedEventArgs(message, title), nameof(PullToRefreshFailed));
    }
}
