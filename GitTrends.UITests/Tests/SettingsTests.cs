using System;
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
	class SettingsTests : BaseUITest
	{
		public SettingsTests(Platform platform, UserType userType) : base(platform, userType)
		{
		}

		public override async Task BeforeEachTest()
		{
			await base.BeforeEachTest().ConfigureAwait(false);

			RepositoryPage.TapSettingsButton();
			await SettingsPage.WaitForPageToLoad().ConfigureAwait(false);
		}

		[Test]
		public void VerifyLogOutButton()
		{
			//Arrange
			bool isLoggedIn_Final, isLoggedIn_Initial;

			//Act
			isLoggedIn_Initial = SettingsPage.IsLoggedIn;

			SettingsPage.TapLoginButton();

			isLoggedIn_Final = SettingsPage.IsLoggedIn;

			//Assert
			Assert.IsTrue(isLoggedIn_Initial);
			Assert.IsFalse(isLoggedIn_Final);
		}

		[Test]
		public void EnsureCreatedByLabelOpensBrowser()
		{
			//Arrange

			//Act
			SettingsPage.TapCopyrightLabel();

			//Assert
			if (App is iOSApp)
			{
				SettingsPage.WaitForBrowserToOpen();
				Assert.IsTrue(SettingsPage.IsBrowserOpen);
			}
		}

		[Test]
		public void EnsureGitHubUserViewOpensBrowser()
		{
			if (UserType is UserType.LoggedIn)
				EnsureGitHubUserViewOpensBrowser_LoggedIn();
			else if (UserType is UserType.Demo)
				EnsureGitHubUserViewOpensBrowser_Demo();
			else
				throw new NotImplementedException();
		}

		[Test]
		public async Task VerifyNotificationsSwitch()
		{
			//Arrange
			bool areNotificationsEnabled_Initial = SettingsPage.AreNotificationsEnabled;
			bool shouldSendNotifications_Initial = SettingsPage.ShouldSendNotifications;
			bool shouldSendNotifications_Final, areNotificationsEnabled_Final;

			//Act
			SettingsPage.ToggleRegisterForNotificationsSwitch();

			await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
			SettingsPage.WaitForNoOperatingSystemNotificationDiaglog();

			shouldSendNotifications_Final = SettingsPage.ShouldSendNotifications;
			areNotificationsEnabled_Final = SettingsPage.AreNotificationsEnabled;

			//Assert
			Assert.IsFalse(shouldSendNotifications_Initial);
			Assert.IsTrue(areNotificationsEnabled_Initial);

			Assert.IsTrue(shouldSendNotifications_Final);
			Assert.IsTrue(areNotificationsEnabled_Final);
		}

		[Test]
		public async Task VerifyThemePicker()
		{
			//Arrange
			PreferredTheme preferredTheme_Initial = SettingsPage.PreferredTheme;
			PreferredTheme preferredTheme_Light, preferredTheme_Dark;

			//Act
			await SettingsPage.SelectTheme(PreferredTheme.Light).ConfigureAwait(false);
			preferredTheme_Light = SettingsPage.PreferredTheme;

			await SettingsPage.SelectTheme(PreferredTheme.Dark).ConfigureAwait(false);
			preferredTheme_Dark = SettingsPage.PreferredTheme;

			//Assert
			Assert.AreEqual(PreferredTheme.Default, preferredTheme_Initial);
			Assert.AreEqual(PreferredTheme.Light, preferredTheme_Light);
			Assert.AreEqual(PreferredTheme.Dark, preferredTheme_Dark);
		}

		[Test]
		public async Task VerifyLanguagePicker()
		{
			//Arrange
			string? settingsButtonText = null, sortButtonText = null;
			string? preferredLanguage_Initial, preferredLanguage_Final;
			string gitHubNameText, gitHubAliasText, tryDemoButtonText, loginTitleLabelText_Disconnect, loginTitleLabelText_Connect,
				themeTitleLabelText, languageTitleLabelText, settingsPageTitle, copyrightLabelTitleLabelText, registerForNotificationsTitleLabelText,
				preferredChartsTitleTitleLabelText, preferredChartsAllTitleLabelText, preferredChartsNoUniquesTitleLabelText, preferredChartsOnlyUniquesTitleLabelText;


			foreach (var preferredLanguageKeyValuePair in CultureConstants.CulturePickerOptions.Reverse())
			{
				//Act
				preferredLanguage_Initial = SettingsPage.PreferredLanguage;

				await SettingsPage.SelectLanguage(preferredLanguageKeyValuePair.Value).ConfigureAwait(false);

				SettingsPage.TapLoginButton();
				tryDemoButtonText = SettingsPage.TryDemoButtonText;
				loginTitleLabelText_Connect = SettingsPage.LoginTitleText;

				await login().ConfigureAwait(false);

				preferredLanguage_Final = SettingsPage.PreferredLanguage;

				settingsPageTitle = SettingsPage.PageTitle;
				gitHubNameText = SettingsPage.GitHubNameLabelText;
				gitHubAliasText = SettingsPage.GitHubAliasLabelText;
				themeTitleLabelText = SettingsPage.ThemeTitleLabelText;
				languageTitleLabelText = SettingsPage.LangageTitleLabelText;
				loginTitleLabelText_Disconnect = SettingsPage.LoginTitleText;
				copyrightLabelTitleLabelText = SettingsPage.CopyrightLabelText;
				preferredChartsTitleTitleLabelText = SettingsPage.PreferredChartLabelText;
				registerForNotificationsTitleLabelText = SettingsPage.RegisterForNotificationsTitleLabelText;

				preferredChartsAllTitleLabelText = TrendsChartConstants.TrendsChartTitles[TrendsChartOption.All];
				preferredChartsNoUniquesTitleLabelText = TrendsChartConstants.TrendsChartTitles[TrendsChartOption.NoUniques];
				preferredChartsOnlyUniquesTitleLabelText = TrendsChartConstants.TrendsChartTitles[TrendsChartOption.JustUniques];

				SettingsPage.TapBackButton();

				if (App is AndroidApp)
				{
					sortButtonText = RepositoryPage.GetSortButtonText();
					settingsButtonText = RepositoryPage.GetSettingsButtonText();
				}

				//Assert
				Assert.AreEqual(PageTitles.RepositoryPage, RepositoryPage.PageTitle);

				Assert.AreNotEqual(preferredLanguage_Final, preferredLanguage_Initial);
				Assert.AreEqual(preferredLanguage_Final, string.IsNullOrWhiteSpace(preferredLanguageKeyValuePair.Key) ? null : preferredLanguageKeyValuePair.Key);

				Assert.AreEqual(PageTitles.SettingsPage, settingsPageTitle);
				Assert.AreEqual(SettingsPageConstants.Theme, themeTitleLabelText);
				Assert.AreEqual(GitHubLoginButtonConstants.TryDemo, tryDemoButtonText);
				Assert.AreEqual(SettingsPageConstants.Language, languageTitleLabelText);
				Assert.AreEqual(GitHubLoginButtonConstants.Disconnect, loginTitleLabelText_Disconnect);
				Assert.AreEqual(GitHubLoginButtonConstants.ConnectToGitHub, loginTitleLabelText_Connect);
				Assert.AreEqual(SettingsPageConstants.RegisterForNotifications, registerForNotificationsTitleLabelText);
				Assert.AreEqual(SettingsPageConstants.PreferredChartSettingsLabelText, preferredChartsTitleTitleLabelText);

				Assert.IsTrue(copyrightLabelTitleLabelText.Contains(SettingsPageConstants.CreatedBy));

				Assert.AreEqual(TrendsChartTitleConstants.All, preferredChartsAllTitleLabelText);
				Assert.AreEqual(TrendsChartTitleConstants.NoUniques, preferredChartsNoUniquesTitleLabelText);
				Assert.AreEqual(TrendsChartTitleConstants.JustUniques, preferredChartsOnlyUniquesTitleLabelText);

				if (UserType is UserType.Demo)
				{
					Assert.AreEqual(DemoUserConstants.Name, gitHubNameText);
					Assert.AreEqual("@" + DemoUserConstants.Alias, gitHubAliasText);
				}

				if (App is AndroidApp)
				{
					Assert.AreEqual(PageTitles.SettingsPage, settingsButtonText);
					Assert.AreEqual(RepositoryPageConstants.SortToolbarItemText, sortButtonText);
				}

				Assert.AreEqual(LoggedInUserName, gitHubNameText);
				Assert.AreEqual("@" + LoggedInUserAlias, gitHubAliasText);

				//Act
				RepositoryPage.TapSettingsButton();
				await SettingsPage.WaitForPageToLoad().ConfigureAwait(false);
			}
			;

			async Task login()
			{
				if (UserType is UserType.Demo)
					SettingsPage.TapTryDemoButton();
				else if (UserType is UserType.LoggedIn)
					await LoginToGitHub().ConfigureAwait(false);
			}
		}

		[Test]
		public async Task VerifyChartPicker()
		{
			//Arrange

			//Assert
			Assert.AreEqual(TrendsChartOption.All, SettingsPage.CurrentTrendsChartOption);

			//Act
			await SettingsPage.SetTrendsChartOption(TrendsChartOption.JustUniques).ConfigureAwait(false);

			//Assert
			Assert.AreEqual(TrendsChartOption.JustUniques, SettingsPage.CurrentTrendsChartOption);

			//Act
			await SettingsPage.SetTrendsChartOption(TrendsChartOption.All).ConfigureAwait(false);

			//Assert
			Assert.AreEqual(TrendsChartOption.All, SettingsPage.CurrentTrendsChartOption);

			//Act
			await SettingsPage.SetTrendsChartOption(TrendsChartOption.NoUniques);

			//Assert
			Assert.AreEqual(TrendsChartOption.NoUniques, SettingsPage.CurrentTrendsChartOption);

			//Act
			SettingsPage.TapBackButton();

			await RepositoryPage.WaitForPageToLoad().ConfigureAwait(false);
			await RepositoryPage.WaitForNoPullToRefreshIndicator().ConfigureAwait(false);

			var visibleRepositories = RepositoryPage.VisibleCollection;
			RepositoryPage.TapRepository(visibleRepositories.First().Name);

			await TrendsPage.WaitForPageToLoad().ConfigureAwait(false);

			//Assert
			Assert.IsTrue(TrendsPage.IsViewsClonesChartSeriesVisible(TrendsChartTitleConstants.TotalViewsTitle));
			Assert.IsTrue(TrendsPage.IsViewsClonesChartSeriesVisible(TrendsChartTitleConstants.TotalClonesTitle));
			Assert.IsFalse(TrendsPage.IsViewsClonesChartSeriesVisible(TrendsChartTitleConstants.UniqueViewsTitle));
			Assert.IsFalse(TrendsPage.IsViewsClonesChartSeriesVisible(TrendsChartTitleConstants.UniqueClonesTitle));
		}

		void EnsureGitHubUserViewOpensBrowser_LoggedIn()
		{
			//Arrange
			var aliasLabelText = SettingsPage.GitHubAliasLabelText;
			var nameLabelText = SettingsPage.GitHubNameLabelText;

			//Act
			SettingsPage.TapGitHubUserView();

			//Assert
			Assert.AreEqual("@" + LoggedInUserAlias, aliasLabelText);
			Assert.AreEqual(LoggedInUserName, nameLabelText);

			if (App is iOSApp)
			{
				SettingsPage.WaitForBrowserToOpen();
				Assert.IsTrue(SettingsPage.IsBrowserOpen);
			}
		}

		void EnsureGitHubUserViewOpensBrowser_Demo()
		{
			//Arrange
			string nameLabelText, aliasLabelText;

			//Act
			SettingsPage.WaitForGitHubLoginToComplete();

			aliasLabelText = SettingsPage.GitHubAliasLabelText;
			nameLabelText = SettingsPage.GitHubNameLabelText;

			SettingsPage.TapGitHubUserView();

			//Assert
			Assert.AreEqual("@" + DemoUserConstants.Alias, aliasLabelText);
			Assert.AreEqual(DemoUserConstants.Name, nameLabelText);

			if (App is iOSApp)
			{
				SettingsPage.WaitForBrowserToOpen();
				Assert.IsTrue(SettingsPage.IsBrowserOpen);
			}
		}
	}
}