using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitTrends.Mobile.Common;
using GitTrends.Mobile.Common.Constants;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.iOS;

namespace GitTrends.UITests
{
	[TestFixture(Platform.Android, UserType.Demo)]
	[TestFixture(Platform.Android, UserType.LoggedIn)]
	[TestFixture(Platform.iOS, UserType.Demo)]
	[TestFixture(Platform.iOS, UserType.LoggedIn)]
	class RepositoriesTests : BaseUITest
	{
		public RepositoriesTests(Platform platform, UserType userType) : base(platform, userType)
		{
		}

		[Test]
		public async Task CancelSortingMenu()
		{
			//Fail all tests if the DefaultSortingOption has changed
			Assert.AreEqual(MobileSortingService.DefaultSortingOption, SortingOption.Views);

			//Arrange
			Repository finalTopRepository;
			Repository finalSecondTopRepository;
			Repository finalLastRepository;
			Repository initialTopRepository = RepositoryPage.VisibleCollection.First();
			Repository initialSecondTopRepository = RepositoryPage.VisibleCollection.Skip(1).First();
			Repository initialLastRepository = RepositoryPage.VisibleCollection.Last();

			//Act
			await RepositoryPage.CancelSortingMenu().ConfigureAwait(false);

			//Assert
			finalTopRepository = RepositoryPage.VisibleCollection.First();
			finalSecondTopRepository = RepositoryPage.VisibleCollection.Skip(1).First();
			finalLastRepository = RepositoryPage.VisibleCollection.Last();

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
			Assert.AreEqual(MobileSortingService.DefaultSortingOption, SortingOption.Views);

			//Arrange
			Repository finalTopRepository;
			Repository finalSecondTopRepository;
			Repository finalLastRepository;
			Repository initialTopRepository = RepositoryPage.VisibleCollection.First();
			Repository initialSecondTopRepository = RepositoryPage.VisibleCollection.Skip(1).First();
			Repository initialLastRepository = RepositoryPage.VisibleCollection.Last();

			//Act
			await RepositoryPage.DismissSortingMenu().ConfigureAwait(false);

			//Assert
			finalTopRepository = RepositoryPage.VisibleCollection.First();
			finalSecondTopRepository = RepositoryPage.VisibleCollection.Skip(1).First();
			finalLastRepository = RepositoryPage.VisibleCollection.Last();

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
			Assert.AreEqual(MobileSortingService.DefaultSortingOption, SortingOption.Views);

			//Arrange
			string floatingActionTextButtonStatistic1Text, floatingActionTextButtonStatistic2Text, floatingActionTextButtonStatistic3Text;

			Repository finalFirstRepository, finalSecondTopRepository, finalLastRepository;
			Repository initialFirstRepository = RepositoryPage.VisibleCollection.First();
			Repository initialSecondTopRepository = RepositoryPage.VisibleCollection.Skip(1).First();
			Repository initialLastRepository = RepositoryPage.VisibleCollection.Last();


			//Act
			await RepositoryPage.SetSortingOption(sortingOption).ConfigureAwait(false);

			floatingActionTextButtonStatistic1Text = RepositoryPage.InformationButtonStatistic1Text;
			floatingActionTextButtonStatistic2Text = RepositoryPage.InformationButtonStatistic2Text;
			floatingActionTextButtonStatistic3Text = RepositoryPage.InformationButtonStatistic3Text;

			RepositoryPage.TapInformationButton();

			//Assert
			finalFirstRepository = RepositoryPage.VisibleCollection.First();
			finalSecondTopRepository = RepositoryPage.VisibleCollection.Skip(1).First();
			finalLastRepository = RepositoryPage.VisibleCollection.Last();

			if (initialFirstRepository.IsTrending == initialSecondTopRepository.IsTrending)
				Assert.GreaterOrEqual(initialFirstRepository.TotalViews, initialSecondTopRepository.TotalViews);

			Assert.GreaterOrEqual(initialFirstRepository.TotalViews, initialLastRepository.TotalViews);


			var floatingActionTextButtonStatistic1Text_Expected = StatisticsService.GetFloatingActionTextButtonText(MobileSortingService.GetSortingCategory(sortingOption), RepositoryPage.VisibleCollection, FloatingActionButtonType.Statistic1);
			var floatingActionTextButtonStatistic2Text_Expected = StatisticsService.GetFloatingActionTextButtonText(MobileSortingService.GetSortingCategory(sortingOption), RepositoryPage.VisibleCollection, FloatingActionButtonType.Statistic2);
			var floatingActionTextButtonStatistic3Text_Expected = StatisticsService.GetFloatingActionTextButtonText(MobileSortingService.GetSortingCategory(sortingOption), RepositoryPage.VisibleCollection, FloatingActionButtonType.Statistic3);

			Assert.AreEqual(floatingActionTextButtonStatistic1Text_Expected, floatingActionTextButtonStatistic1Text);
			Assert.AreEqual(floatingActionTextButtonStatistic2Text_Expected, floatingActionTextButtonStatistic2Text);
			Assert.AreEqual(floatingActionTextButtonStatistic3Text_Expected, floatingActionTextButtonStatistic3Text);

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
			}
			;
		}

		[Test]
		public async Task VerifyRepositoriesAfterLogin()
		{
			//Arrange
			IReadOnlyList<Repository> visibleRepositoryList;
			int smallScreenTrendingImageCount;
			int largeScreenTrendingImageCount;

			//Act
			RepositoryPage.TriggerPullToRefresh();
			await RepositoryPage.WaitForPullToRefreshIndicator().ConfigureAwait(false);
			await RepositoryPage.WaitForNoPullToRefreshIndicator().ConfigureAwait(false);

			smallScreenTrendingImageCount = RepositoryPage.SmallScreenTrendingImageCount;
			largeScreenTrendingImageCount = RepositoryPage.LargeScreenTrendingImageCount;

			//Assert
			visibleRepositoryList = RepositoryPage.VisibleCollection;
			Assert.IsTrue(visibleRepositoryList.Any());

			if (visibleRepositoryList.Count(static x => x.IsTrending) > 0)
			{
				Assert.AreNotEqual(smallScreenTrendingImageCount, largeScreenTrendingImageCount);
				if (smallScreenTrendingImageCount > 0)
					Assert.AreEqual(0, largeScreenTrendingImageCount);

				if (largeScreenTrendingImageCount > 0)
					Assert.AreEqual(0, smallScreenTrendingImageCount);
			}

			Assert.IsFalse(visibleRepositoryList.Any(static x => x.DailyClonesList?.Count <= 0 || x.DailyViewsList?.Count <= 0));
		}

		[Test]
		public async Task VerifyNoRepositoriesAfterLogOut()
		{
			//Arrange

			//Act
			RepositoryPage.TapSettingsButton();
			await SettingsPage.WaitForPageToLoad().ConfigureAwait(false);

			//Assert
			Assert.AreEqual(GitHubLoginButtonConstants.Disconnect, SettingsPage.LoginTitleText);

			//Act
			SettingsPage.TapLoginButton();
			SettingsPage.WaitForGitHubLogoutToComplete();

			//Assert
			Assert.AreEqual(GitHubLoginButtonConstants.ConnectToGitHub, SettingsPage.LoginTitleText);

			//Act
			SettingsPage.TapBackButton();

			//Assert
			await WelcomePage.WaitForPageToLoad().ConfigureAwait(false);
		}
	}
}