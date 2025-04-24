using CommunityToolkit.Maui.Markup;
using GitTrends.Common;
using GitTrends.Mobile.Common;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using static GitTrends.MauiService;

namespace GitTrends;

public abstract class BaseOnboardingDataTemplate(
	string nextButtonText,
	IDeviceInfo deviceInfo,
	Color backgroundColor,
	int carouselPositionIndex,
	Func<View> createImageView,
	Func<BaseOnboardingDataTemplate.TitleLabel> createTitleLabel,
	Func<View> createDescriptionBodyView,
	IAnalyticsService analyticsService)
	: DataTemplate(() => CreateGrid(nextButtonText, deviceInfo, backgroundColor, carouselPositionIndex, createImageView,
		createTitleLabel, createDescriptionBodyView))
{
	enum Row
	{
		Image,
		Description,
		Indicator
	}

	enum Column
	{
		Indicator,
		Button
	}

	protected IDeviceInfo DeviceInfo { get; } = deviceInfo;
	protected IAnalyticsService AnalyticsService { get; } = analyticsService;

	static Grid CreateGrid(string nextButtonText,
		IDeviceInfo deviceInfo,
		Color backgroundColor,
		int carouselPositionIndex,
		Func<View> createImageView,
		Func<TitleLabel> createTitleLabel,
		Func<View> createDescriptionBodyView) => new Grid()
		{
			BackgroundColor = backgroundColor,

			RowDefinitions = Rows.Define(
			(Row.Image, Stars(GetImageRowStarHeight(deviceInfo))),
			(Row.Description, Stars(GetDescriptionRowStarHeight(deviceInfo))),
			(Row.Indicator, 44)),

			ColumnDefinitions = Columns.Define(
			(Column.Indicator, Star),
			(Column.Button, Star)),

			Children =
		{
			new OpacityOverlay()
				.Row(Row.Image).ColumnSpan(All<Column>()),

			createImageView()
				.Row(Row.Image).ColumnSpan(All<Column>())
				.Margin(32, 16),

			new VerticalStackLayout
				{
					Spacing = 16,
					Children =
					{
						createTitleLabel(),
						createDescriptionBodyView()
					}
				}.Margin(32, 8)
				.Row(Row.Description)
				.RowSpan(2).ColumnSpan(All<Column>()),

			new OnboardingIndicatorView(carouselPositionIndex)
				.Row(Row.Indicator).Column(Column.Indicator),

			new NextLabel(nextButtonText)
				.Row(Row.Indicator).Column(Column.Button),
		}
		}.Bind(Grid.WidthRequestProperty,
		nameof(Page.Width),
		mode: BindingMode.OneWay,
		source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestor, typeof(Page)));

	static int GetImageRowStarHeight(IDeviceInfo deviceInfo)
	{
		if (ScreenHeight < 700)
			return 8;

		return deviceInfo.Platform == DevicePlatform.iOS ? 3 : 11;
	}

	static int GetDescriptionRowStarHeight(IDeviceInfo deviceInfo)
	{
		if (ScreenHeight < 700)
			return 9;

		return deviceInfo.Platform == DevicePlatform.iOS ? 2 : 9;
	}

	protected sealed class BodySvg(in IDeviceInfo deviceInfo, in string svgFileName)
		: SvgImage(deviceInfo, svgFileName, () => Colors.White, 24, 24);

	protected sealed class TitleLabel : Label
	{
		public TitleLabel(in string text)
		{
			Text = text;
			FontSize = 34;
			TextColor = Colors.White;
			LineHeight = 1.12;
			FontFamily = FontFamilyConstants.RobotoBold;
			AutomationId = OnboardingAutomationIds.TitleLabel;
		}
	}

	protected sealed class BodyLabel : Label
	{
		public BodyLabel(in string text)
		{
			Text = text;
			FontSize = 15;
			TextColor = Colors.White;
			LineHeight = 1.021;
			LineBreakMode = LineBreakMode.WordWrap;
			FontFamily = FontFamilyConstants.RobotoRegular;
			VerticalTextAlignment = TextAlignment.Start;
		}
	}

	sealed class NextLabel : Label
	{
		public NextLabel(in string text)
		{
			Text = text;
			FontSize = 15;
			TextColor = Colors.White;
			BackgroundColor = Colors.Transparent;
			FontFamily = FontFamilyConstants.RobotoRegular;

			Opacity = 0.8;

			Margin = new Thickness(0, 0, 30, 0);

			HorizontalOptions = LayoutOptions.End;
			VerticalOptions = LayoutOptions.Center;

			AutomationId = OnboardingAutomationIds.NextButon;

			GestureRecognizers.Add(new TapGestureRecognizer
			{
				CommandParameter = text
			}
				.Bind(TapGestureRecognizer.CommandProperty,
					nameof(OnboardingViewModel.HandleDemoButtonTappedCommand),
					source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestorBindingContext,
						typeof(OnboardingViewModel))));

			this.SetBinding(IsVisibleProperty, nameof(OnboardingViewModel.IsDemoButtonVisible));
		}
	}

	sealed class OpacityOverlay : BoxView
	{
		public OpacityOverlay() => BackgroundColor = Colors.White.MultiplyAlpha(0.25f);
	}

	sealed class OnboardingIndicatorView : IndicatorView
	{
		public OnboardingIndicatorView(in int position)
		{
			Position = position;

			IsEnabled = false;
			InputTransparent = true;

			SelectedIndicatorColor = Colors.White;
			IndicatorColor = Colors.White.MultiplyAlpha(0.25f);

			Margin = new Thickness(30, 0, 0, 0);

			HorizontalOptions = LayoutOptions.Start;
			AutomationId = OnboardingAutomationIds.PageIndicator;

			SetBinding(CountProperty, new Binding(nameof(OnboardingPage.PageCount),
				source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestor, typeof(OnboardingPage))));
		}
	}
}