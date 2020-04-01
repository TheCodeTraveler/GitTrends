﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitTrends.Mobile.Shared;
using GitTrends.Shared;
using NUnit.Framework;
using Xamarin.UITest;

namespace GitTrends.UITests
{
    [TestFixture(Platform.Android, UserType.Demo)]
    [TestFixture(Platform.Android, UserType.LoggedIn)]
    [TestFixture(Platform.iOS, UserType.LoggedIn)]
    [TestFixture(Platform.iOS, UserType.Demo)]
    class RepositoriesTests : BaseTest
    {
        public RepositoriesTests(Platform platform, UserType userType) : base(platform, userType)
        {
        }

        [Test]
        public async Task CancelSortingMenu()
        {
            //Fail all tests if the DefaultSortingOption has changed
            Assert.AreEqual(SortingConstants.DefaultSortingOption, SortingOption.Views);

            //Arrange
            Repository finalTopRepository;
            Repository finalSecondTopRepository;
            Repository finalLastRepository;
            Repository initialTopRepository = RepositoryPage.GetVisibleRepositoryList().First();
            Repository initialSecondTopRepository = RepositoryPage.GetVisibleRepositoryList().Skip(1).First();
            Repository initialLastRepository = RepositoryPage.GetVisibleRepositoryList().Last();

            //Act
            await RepositoryPage.CancelSortingMenu().ConfigureAwait(false);

            //Assert
            finalTopRepository = RepositoryPage.GetVisibleRepositoryList().First();
            finalSecondTopRepository = RepositoryPage.GetVisibleRepositoryList().Skip(1).First();
            finalLastRepository = RepositoryPage.GetVisibleRepositoryList().Last();

            if (initialTopRepository.IsTrending == initialSecondTopRepository.IsTrending)
                Assert.GreaterOrEqual(initialTopRepository.TotalViews, initialSecondTopRepository.TotalViews);
            else
                Assert.GreaterOrEqual(initialSecondTopRepository.TotalViews, initialLastRepository.TotalViews);

            Assert.AreEqual(initialTopRepository.Name, finalTopRepository.Name);
            Assert.AreEqual(initialSecondTopRepository.Name, finalSecondTopRepository.Name);

            if (finalTopRepository.IsTrending == finalSecondTopRepository.IsTrending)
                Assert.GreaterOrEqual(finalTopRepository.TotalViews, finalSecondTopRepository.TotalViews);
            else
                Assert.GreaterOrEqual(finalSecondTopRepository.TotalViews, finalLastRepository.TotalViews);
        }

        [Test]
        public async Task DismissSortingMenu()
        {
            //Fail all tests if the DefaultSortingOption has changed
            Assert.AreEqual(SortingConstants.DefaultSortingOption, SortingOption.Views);

            //Arrange
            Repository finalTopRepository;
            Repository finalSecondTopRepository;
            Repository finalLastRepository;
            Repository initialTopRepository = RepositoryPage.GetVisibleRepositoryList().First();
            Repository initialSecondTopRepository = RepositoryPage.GetVisibleRepositoryList().Skip(1).First();
            Repository initialLastRepository = RepositoryPage.GetVisibleRepositoryList().Last();

            //Act
            await RepositoryPage.DismissSortingMenu().ConfigureAwait(false);

            //Assert
            finalTopRepository = RepositoryPage.GetVisibleRepositoryList().First();
            finalSecondTopRepository = RepositoryPage.GetVisibleRepositoryList().Skip(1).First();
            finalLastRepository = RepositoryPage.GetVisibleRepositoryList().Last();

            if (initialTopRepository.IsTrending == initialSecondTopRepository.IsTrending)
                Assert.GreaterOrEqual(initialTopRepository.TotalViews, initialSecondTopRepository.TotalViews);
            else
                Assert.GreaterOrEqual(initialSecondTopRepository.TotalViews, initialLastRepository.TotalViews);

            Assert.AreEqual(initialTopRepository.Name, finalTopRepository.Name);
            Assert.AreEqual(initialSecondTopRepository.Name, finalSecondTopRepository.Name);

            if (finalTopRepository.IsTrending == finalSecondTopRepository.IsTrending)
                Assert.GreaterOrEqual(finalTopRepository.TotalViews, finalSecondTopRepository.TotalViews);
            else
                Assert.GreaterOrEqual(finalSecondTopRepository.TotalViews, finalLastRepository.TotalViews);
        }

        [TestCase(SortingConstants.DefaultSortingOption)]
        [TestCase(SortingOption.Clones)]
        [TestCase(SortingOption.Forks)]
        [TestCase(SortingOption.Issues)]
        [TestCase(SortingOption.Stars)]
        [TestCase(SortingOption.UniqueClones)]
        [TestCase(SortingOption.UniqueViews)]
        [TestCase(SortingOption.Views)]
        public async Task VerifySortingOptions(SortingOption sortingOption)
        {
            //Fail all tests if the DefaultSortingOption has changed
            Assert.AreEqual(SortingConstants.DefaultSortingOption, SortingOption.Views);

            //Arrange
            Repository finalFirstRepository;
            Repository finalSecondTopRepository;
            Repository finalLastRepository;
            Repository initialFirstRepository = RepositoryPage.GetVisibleRepositoryList().First();
            Repository initialSecondTopRepository = RepositoryPage.GetVisibleRepositoryList().Skip(1).First();
            Repository initialLastRepository = RepositoryPage.GetVisibleRepositoryList().Last();

            //Act
            await RepositoryPage.SetSortingOption(sortingOption).ConfigureAwait(false);

            //Assert
            finalFirstRepository = RepositoryPage.GetVisibleRepositoryList().First();
            finalSecondTopRepository = RepositoryPage.GetVisibleRepositoryList().Skip(1).First();
            finalLastRepository = RepositoryPage.GetVisibleRepositoryList().Last();

            if (initialFirstRepository.IsTrending == initialSecondTopRepository.IsTrending)
                Assert.GreaterOrEqual(initialFirstRepository.TotalViews, initialSecondTopRepository.TotalViews);

            Assert.GreaterOrEqual(initialFirstRepository.TotalViews, initialLastRepository.TotalViews);

            switch (sortingOption)
            {
                case SortingOption.Views when finalFirstRepository.IsTrending == finalSecondTopRepository.IsTrending:
                    Assert.LessOrEqual(finalFirstRepository.TotalViews, finalSecondTopRepository.TotalViews);
                    break;
                case SortingOption.Views:
                    Assert.LessOrEqual(finalSecondTopRepository.TotalViews, finalLastRepository.TotalViews);
                    break;
                case SortingOption.Stars when finalFirstRepository.IsTrending == finalSecondTopRepository.IsTrending:
                    Assert.GreaterOrEqual(finalFirstRepository.StarCount, finalSecondTopRepository.StarCount);
                    break;
                case SortingOption.Stars:
                    Assert.GreaterOrEqual(finalSecondTopRepository.StarCount, finalLastRepository.StarCount);
                    break;
                case SortingOption.Forks when finalFirstRepository.IsTrending == finalSecondTopRepository.IsTrending:
                    Assert.GreaterOrEqual(finalFirstRepository.ForkCount, finalSecondTopRepository.ForkCount);
                    break;
                case SortingOption.Forks:
                    Assert.GreaterOrEqual(finalSecondTopRepository.ForkCount, finalLastRepository.ForkCount);
                    break;
                case SortingOption.Issues when finalFirstRepository.IsTrending == finalSecondTopRepository.IsTrending:
                    Assert.GreaterOrEqual(finalFirstRepository.IssuesCount, finalSecondTopRepository.IssuesCount);
                    break;
                case SortingOption.Issues:
                    Assert.GreaterOrEqual(finalSecondTopRepository.IssuesCount, finalLastRepository.IssuesCount);
                    break;
                case SortingOption.Clones when finalFirstRepository.IsTrending == finalSecondTopRepository.IsTrending:
                    Assert.GreaterOrEqual(finalFirstRepository.TotalClones, finalSecondTopRepository.TotalClones);
                    break;
                case SortingOption.Clones:
                    Assert.GreaterOrEqual(finalSecondTopRepository.TotalClones, finalLastRepository.TotalClones);
                    break;
                case SortingOption.UniqueClones when finalFirstRepository.IsTrending == finalSecondTopRepository.IsTrending:
                    Assert.GreaterOrEqual(finalFirstRepository.TotalUniqueClones, finalSecondTopRepository.TotalUniqueClones);
                    break;
                case SortingOption.UniqueClones:
                    Assert.GreaterOrEqual(finalSecondTopRepository.TotalUniqueClones, finalLastRepository.TotalUniqueClones);
                    break;
                case SortingOption.UniqueViews when finalFirstRepository.IsTrending == finalSecondTopRepository.IsTrending:
                    Assert.GreaterOrEqual(finalFirstRepository.TotalUniqueViews, finalSecondTopRepository.TotalUniqueViews);
                    break;
                case SortingOption.UniqueViews:
                    Assert.GreaterOrEqual(finalSecondTopRepository.TotalUniqueViews, finalLastRepository.TotalUniqueViews);
                    break;
                default:
                    throw new NotSupportedException();
            };
        }

        [Test]
        public async Task VerifyRepositoriesAfterLogin()
        {
            //Arrange
            IReadOnlyList<Repository> visibleRepositoryList;

            //Act
            RepositoryPage.TriggerPullToRefresh();
            await RepositoryPage.WaitForNoPullToRefreshIndicator().ConfigureAwait(false);

            //Assert
            visibleRepositoryList = RepositoryPage.GetVisibleRepositoryList();
            Assert.IsTrue(visibleRepositoryList.Any());

        }

        [Test]
        public async Task VerifyNoRepositoriesAfterLogOut()
        {
            //Arrange
            IReadOnlyList<Repository> visibleRepositoryList;

            //Act
            RepositoryPage.TapSettingsButton();

            //Assert
            Assert.AreEqual(GitHubLoginButtonConstants.Disconnect, SettingsPage.GitHubButtonText);

            //Act
            SettingsPage.TapGitHubButton();
            SettingsPage.WaitForGitHubLogoutToComplete();

            //Assert
            Assert.AreEqual(GitHubLoginButtonConstants.ConnectWithGitHub, SettingsPage.GitHubButtonText);

            //Act
            SettingsPage.TapBackButton();

            RepositoryPage.DeclineGitHubUserNotFoundPopup();
            RepositoryPage.TriggerPullToRefresh();

            await RepositoryPage.WaitForPullToRefreshIndicator().ConfigureAwait(false);
            await RepositoryPage.WaitForNoPullToRefreshIndicator().ConfigureAwait(false);

            //Assert
            visibleRepositoryList = RepositoryPage.GetVisibleRepositoryList();
            Assert.IsFalse(visibleRepositoryList.Any());
        }
    }
}
