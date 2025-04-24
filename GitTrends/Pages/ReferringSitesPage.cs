using CommunityToolkit.Maui.Markup;
using GitTrends.Common;
using GitTrends.Mobile.Common;
using GitTrends.Mobile.Common.Constants;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace GitTrends;

sealed class ReferringSitesPage : BaseContentPage<ReferringSitesViewModel>, IDisposable
{
	const int _titleTopMargin = 10;

	readonly CancellationTokenSource _refreshViewCancellationTokenSource = new();

	readonly RefreshView _refreshView;
	readonly ThemeService _themeService;
	readonly ReviewService _reviewService;
	readonly GitHubUserService _gitHubUserService;
	readonly DeepLinkingService _deepLinkingService;

	public ReferringSitesPage(
		IDeviceInfo deviceInfo,
		ThemeService themeService,
		ReviewService reviewService,
		IAnalyticsService analyticsService,
		GitHubUserService gitHubUserService,
		DeepLinkingService deepLinkingService,
		ReferringSitesViewModel referringSitesViewModel) : base(referringSitesViewModel, analyticsService)
	{
#if IOS || MACCATALYST
		Shell.SetPresentationMode(this, PresentationMode.ModalAnimated);
#endif

		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);

		Title = PageTitles.ReferringSitesPage;

		_themeService = themeService;
		_reviewService = reviewService;
		_gitHubUserService = gitHubUserService;
		_deepLinkingService = deepLinkingService;

		ReferringSitesViewModel.PullToRefreshFailed += HandlePullToRefreshFailed;

		var isiOS = deviceInfo.Platform == DevicePlatform.iOS;
		var titleRowHeight = isiOS ? 50 : 0;
		var shadowHeight = isiOS ? 1 : 0;

		var collectionView = new ReferringSitesCollectionView(deviceInfo)
			.Bind(IsVisibleProperty,
				getter: static (ReferringSitesViewModel vm) => vm.IsEmptyDataViewEnabled)
			.Bind(CollectionView.ItemsSourceProperty,
				getter: static (ReferringSitesViewModel vm) => vm.MobileReferringSitesList)
			.Invoke(collectionView => collectionView.SelectionChanged += HandleCollectionViewSelectionChanged);

		Content = new Grid
		{
			IgnoreSafeArea = true,

			RowSpacing = 0,

			RowDefinitions = Rows.Define(
				(Row.Title, Auto),
				(Row.TitleShadow, shadowHeight),
				(Row.List, Star)),

			ColumnDefinitions = Columns.Define(
				(Column.Title, Stars(3)),
				(Column.Button, Stars(1))),

			Children =
			{
				new ReferringSitesRefreshView(collectionView).Assign(out _refreshView)
					.Row(Row.TitleShadow).RowSpan(3).ColumnSpan(All<Column>())
					.DynamicResource(RefreshView.RefreshColorProperty, nameof(BaseTheme.PullToRefreshColor))
					.Bind(RefreshView.CommandProperty,
						getter: static (ReferringSitesViewModel vm) => vm.ExecuteRefreshCommand,
						mode: BindingMode.OneTime)
					.Bind(RefreshView.IsRefreshingProperty,
						getter: static (ReferringSitesViewModel vm) => vm.IsRefreshing,
						setter: static (vm, isRefreshing) => vm.IsRefreshing = isRefreshing)
			}
		};

		if (isiOS)
		{
			var grid = (Grid)Content;

			grid.Children.Add(new TitleShadowView(themeService, titleRowHeight, shadowHeight).Row(Row.Title)
				.ColumnSpan(All<Column>()));
			grid.Children.Add(new TitleLabel().Row(Row.Title).Column(Column.Title));
			grid.Children.Add(new CloseButton(titleRowHeight)
				.Invoke(closeButton => closeButton.Clicked += HandleCloseButtonClicked).Row(Row.Title)
				.Column(Column.Button));
		}
	}

	enum Row
	{
		Title,
		TitleShadow,
		List
	}

	enum Column
	{
		Title,
		Button
	}

	public void Dispose()
	{
		_refreshViewCancellationTokenSource.Dispose();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_refreshView.Content is CollectionView collectionView
			&& collectionView.ItemsSource.IsNullOrEmpty())
		{
			_refreshView.IsRefreshing = true;
			await _reviewService.TryRequestReviewPrompt();
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		_refreshViewCancellationTokenSource.Cancel();
	}

	async void HandleCollectionViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var collectionView = (CollectionView)sender;
		collectionView.SelectedItem = null;

		if (e.CurrentSelection.FirstOrDefault() is
			ReferringSiteModel { IsReferrerUriValid: true, ReferrerUri: not null } referringSite)
		{
			AnalyticsService.Track("Referring Site Tapped", new Dictionary<string, string>
			{
				{
					nameof(ReferringSiteModel) + nameof(ReferringSiteModel.Referrer), referringSite.Referrer
				},
				{
					nameof(ReferringSiteModel) + nameof(ReferringSiteModel.ReferrerUri),
					referringSite.ReferrerUri.ToString()
				}
			});

			await _deepLinkingService.OpenBrowser(referringSite.ReferrerUri, CancellationToken.None);
		}
	}

	void HandlePullToRefreshFailed(object? sender, PullToRefreshFailedEventArgs eventArgs) => Dispatcher.DispatchAsync(
		async () =>
		{
			if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault() is { Page: Microsoft.Maui.Controls.Page currentPage }
				&& currentPage.Navigation.ModalStack.LastOrDefault() is not ReferringSitesPage
				&& currentPage.Navigation.NavigationStack.Last() is not ReferringSitesPage)
			{
				return;
			}

			switch (eventArgs)
			{
				case MaximumApiRequestsReachedEventArgs:
					var isAccepted = await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Accept,
						eventArgs.Cancel);
					if (isAccepted)
						await _deepLinkingService.OpenBrowser(GitHubConstants.GitHubRateLimitingDocs,
							CancellationToken.None);
					break;

				case AbuseLimitPullToRefreshEventArgs when _gitHubUserService.GitHubApiAbuseLimitCount <= 1:
					var isAlertAccepted = await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Accept,
						eventArgs.Cancel);
					if (isAlertAccepted)
						await _deepLinkingService.OpenBrowser(GitHubConstants.GitHubApiAbuseDocs,
							CancellationToken.None);
					break;

				case AbuseLimitPullToRefreshEventArgs:
					// Don't display error message when GitHubUserService.GitHubApiAbuseLimitCount > 1
					break;

				case LoginExpiredPullToRefreshEventArgs:
					await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Cancel);
					await Navigation.PopToRootAsync();
					break;

				case ErrorPullToRefreshEventArgs:
					await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Cancel);
					break;

				default:
					throw new NotSupportedException();
			}
		});

	async void HandleCloseButtonClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();

	sealed class ReferringSitesRefreshView : RefreshView
	{
		public ReferringSitesRefreshView(in CollectionView collectionView)
		{
			Content = collectionView;
			AutomationId = ReferringSitesPageAutomationIds.RefreshView;
		}
	}

	sealed class ReferringSitesCollectionView : CollectionView
	{
		public ReferringSitesCollectionView(IDeviceInfo deviceInfo)
		{
			AutomationId = ReferringSitesPageAutomationIds.CollectionView;
			BackgroundColor = Colors.Transparent;
			SelectionMode = SelectionMode.Single;
			ItemTemplate = new ReferringSitesDataTemplate();
			ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);

			//iOS Header + Footer break CollectionView after Refresh bug: https://github.com/xamarin/Xamarin.Forms/issues/9879
			Header = deviceInfo.Platform == DevicePlatform.iOS
				? null
				: new BoxView
				{
					HeightRequest = ReferringSitesDataTemplate.BottomPadding
				};

			Footer = deviceInfo.Platform == DevicePlatform.iOS
				? null
				: new BoxView
				{
					HeightRequest = ReferringSitesDataTemplate.TopPadding
				};

			EmptyView = new EmptyDataView("EmptyReferringSitesList", ReferringSitesPageAutomationIds.EmptyDataView)
				.Bind(EmptyDataView.TitleProperty,
					getter: static (ReferringSitesViewModel vm) => vm.EmptyDataViewTitle)
				.Bind(EmptyDataView.DescriptionProperty,
					getter: static (ReferringSitesViewModel vm) => vm.EmptyDataViewDescription);
		}
	}

	sealed class TitleShadowView : BoxView
	{
		public TitleShadowView(in ThemeService themeService, in double heightRequest, in double shadowHeight)
		{
			HeightRequest = heightRequest;

			this.DynamicResource(BackgroundColorProperty, nameof(BaseTheme.CardSurfaceColor));
			if (themeService.IsLightTheme())
			{
				Shadow = new Shadow
				{
					Brush = Colors.Gray,
					Offset = new(new Size(0, shadowHeight)),
					Opacity = 0.5f,
					Radius = 4
				};
			}
		}
	}

	sealed class TitleLabel : Label
	{
		public TitleLabel()
		{
			Text = PageTitles.ReferringSitesPage;
			Padding = new Thickness(10, 5, 0, 5);

			this.Font(family: FontFamilyConstants.RobotoMedium, size: 30);
			this.Start().TextCenterVertical().TextStart();
			this.DynamicResource(TextColorProperty, nameof(BaseTheme.TextColor));
		}
	}

	sealed class CloseButton : Button
	{
		public CloseButton(in int titleRowHeight)
		{
			Text = ReferringSitesPageConstants.CloseButtonText;
			AutomationId = ReferringSitesPageAutomationIds.CloseButton;

			CornerRadius = 4;

			this.Font(family: FontFamilyConstants.RobotoRegular);
			this.End().CenterVertical().Margins(right: 10).Height(titleRowHeight * 3.0f / 5.0f).Padding(5, 0);

			this.DynamicResources((TextColorProperty, nameof(BaseTheme.CloseButtonTextColor)),
				(BackgroundColorProperty, nameof(BaseTheme.CloseButtonBackgroundColor)));
		}
	}
}