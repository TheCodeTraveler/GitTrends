using CommunityToolkit.Maui.Markup;
using GitTrends.Common;
using GitTrends.Mobile.Common;
using GitTrends.Resources;
using Sharpnado.MaterialFrame;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using static GitTrends.MauiService;

namespace GitTrends;

abstract class BaseRepositoryDataTemplate : DataTemplate
{
	public const int TopPadding = 12;
	public const int BottomPadding = 4;

	const int _statsColumnSize = 40;
	const double _statisticsRowHeight = StatisticsLabel.StatisticsFontSize + 4;
	const double _emojiColumnSize = _statisticsRowHeight;

	static readonly double _circleImageHeight = IsSmallScreen ? 48 : 62;

	private protected BaseRepositoryDataTemplate(Func<object> loadTemplate) : base(loadTemplate)
	{
	}

	private protected enum Row
	{
		Title,
		Description,
		DescriptionPadding,
		Separator,
		SeparatorPadding,
		Statistics
	}

	private protected enum Column
	{
		Avatar,
		AvatarPadding,
		Trending,
		Emoji1,
		Statistic1,
		Emoji2,
		Statistic2,
		Emoji3,
		Statistic3
	}

#if ANDROID
	protected sealed class CardView : ExtendedSwipeView
#else
	protected sealed class CardView : SwipeView
#endif
	{
		public CardView(in IDeviceInfo deviceInfo, in IEnumerable<View> dataTemplateChildren)
		{
#if ANDROID
			this.Bind(TappedCommandParameterProperty, mode: BindingMode.OneTime)
				.Bind(TappedCommandProperty,
					nameof(RepositoryPage.RepositoryDataTemplateTappedCommand),
					BindingMode.OneTime,
					source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestor, typeof(RepositoryPage)));
#endif
			var sidePadding = IsSmallScreen ? 8 : 16;
			this.DynamicResource(BackgroundColorProperty, nameof(BaseTheme.PageBackgroundColor));

			RightItems =
			[
				new SwipeItemView
					{
						Content = new Label()
							.Margins(right: sidePadding)
							.Font(size: 32).Center()
							.Bind(Label.TextProperty,
								getter: static (Repository repository) => repository.IsFavorite,
								mode: BindingMode.OneTime,
								convert: static (bool? isFavorite) => isFavorite is true
									? FontAwesomeConstants.StarFilled
									: FontAwesomeConstants.StarOutline)
							.Bind(Label.FontFamilyProperty,
								getter: static (Repository repository) => repository.IsFavorite,
								mode: BindingMode.OneTime,
								convert: static (bool? isFavorite) => isFavorite is true
									? FontFamilyConstants.FontAwesomeSolid
									: FontFamilyConstants.FontAwesome)
							.DynamicResource(Label.TextColorProperty, nameof(BaseTheme.CardStarsStatsIconColor))
					}.Bind(SwipeItemView.CommandProperty,
						nameof(RepositoryViewModel.ToggleIsFavoriteCommand),
						BindingMode.OneTime,
						source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestorBindingContext, typeof(RepositoryViewModel)))
					.Bind(SwipeItemView.CommandParameterProperty,
						mode: BindingMode.OneTime)
			];

			LeftItems =
			[
				new SwipeItemView
					{
						Content = new Label
							{
								Text = FontAwesomeConstants.ExternalLink.ToString()
							}.DynamicResource(Label.TextColorProperty, nameof(BaseTheme.PrimaryTextColor))
							.Font(FontFamilyConstants.FontAwesomeSolid, 28).Center()
							.Margins(left: sidePadding),
					}.Bind(SwipeItemView.CommandProperty,
						nameof(RepositoryViewModel.NavigateToRepositoryWebsiteCommand),
						BindingMode.OneTime,
						source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestorBindingContext, typeof(RepositoryViewModel)))
					.Bind(SwipeItemView.CommandParameterProperty,
						mode: BindingMode.OneTime)
			];

			Content = new Grid
			{
				BackgroundColor = Colors.Transparent,

				InputTransparent = true,

				RowSpacing = 0,

				RowDefinitions = Rows.Define(
					(CardViewRow.TopPadding, TopPadding),
					(CardViewRow.Card, Star),
					(CardViewRow.BottomPadding, BottomPadding)),

				ColumnDefinitions = Columns.Define(
					(CardViewColumn.LeftPadding, sidePadding),
					(CardViewColumn.Card, Star),
					(CardViewColumn.RightPadding, sidePadding)),

				Children =
				{
					new CardViewFrame(deviceInfo, dataTemplateChildren).Row(CardViewRow.Card).Column(CardViewColumn.Card)
				}
			};
		}

		enum CardViewRow
		{
			TopPadding,
			Card,
			BottomPadding
		}

		enum CardViewColumn
		{
			LeftPadding,
			Card,
			RightPadding
		}

		sealed class CardViewFrame : MaterialFrame
		{
			public CardViewFrame(in IDeviceInfo deviceInfo, in IEnumerable<View> dataTemplateChildren)
			{
				Padding = IsSmallScreen ? new Thickness(8, 14, 6, 8) : new Thickness(16, 14, 12, 8);
				CornerRadius = 4;
				HasShadow = true;
				Elevation = 4;

				Content = new ContentGrid(deviceInfo, dataTemplateChildren);

				this.DynamicResource(MaterialThemeProperty, nameof(BaseTheme.MaterialFrameTheme));
			}

			sealed class ContentGrid : Grid
			{
				public ContentGrid(in IDeviceInfo deviceInfo, in IEnumerable<View> dataTemplateChildren)
				{
					Margin = Padding = 0;

					RowSpacing = 6;

					this.Fill();

					RowDefinitions = Rows.Define(
						(Row.Title, 25),
						(Row.Description, 40),
						(Row.DescriptionPadding, 4),
						(Row.Separator, 1),
						(Row.SeparatorPadding, 8),
						(Row.Statistics, _statisticsRowHeight));

					ColumnDefinitions = Columns.Define(
						(Column.Avatar, _circleImageHeight),
						(Column.AvatarPadding, IsSmallScreen ? 4 : 8),
						(Column.Trending, Star),
						(Column.Emoji1, _emojiColumnSize),
						(Column.Statistic1, _statsColumnSize),
						(Column.Emoji2, _emojiColumnSize),
						(Column.Statistic2, _statsColumnSize),
						(Column.Emoji3, _emojiColumnSize),
						(Column.Statistic3, _statsColumnSize));

					Children.Add(new AvatarImage(_circleImageHeight)
						.Row(Row.Title).Column(Column.Avatar).RowSpan(2)
						.Bind(AvatarImage.ImageSourceProperty,
							getter: static (Repository repository) => repository.OwnerAvatarUrl,
							mode: BindingMode.OneTime)
						.DynamicResources(
							(CircleImage.ErrorPlaceholderProperty, nameof(BaseTheme.DefaultProfileImageSource)),
							(CircleImage.LoadingPlaceholderProperty, nameof(BaseTheme.DefaultProfileImageSource))));

					Children.Add(new NameLabel()
						.Row(Row.Title).Column(Column.Trending).ColumnSpan(7)
						.Bind(Label.TextProperty,
							getter: static (Repository repository) => repository.Name,
							mode: BindingMode.OneTime));

					Children.Add(new DescriptionLabel()
						.Row(Row.Description).Column(Column.Trending).ColumnSpan(7)
						.Bind(Label.TextProperty,
							getter: static (Repository repository) => repository.Description));

					Children.Add(new Separator()
						.Row(Row.Separator).Column(Column.Trending).ColumnSpan(7));

					//On large screens, display TrendingImage in the same column as the repository name
					Children.Add(new TrendingImage(deviceInfo, RepositoryPageAutomationIds.LargeScreenTrendingImage)
						.Row(Row.SeparatorPadding).Column(Column.Trending).RowSpan(2)
						.Assign(out TrendingImage largeScreenTrendingImage)
						.Bind(IsVisibleProperty,
							binding1: new Binding(nameof(Repository.IsTrending), BindingMode.OneWay),
							binding2: new Binding(nameof(Width), BindingMode.OneWay, source: this),
							binding3: new Binding(nameof(Repository.IsFavorite), BindingMode.OneWay),
							convert: static ((bool IsTrending, double Width, bool? IsFavorite) inputs) => IsTrendingImageVisible(inputs.IsTrending, inputs.Width, inputs.IsFavorite, largeScreenTrendingImageWidth => largeScreenTrendingImageWidth < (TrendingImage.SvgWidthRequest + 8))));

					//On smaller screens, display TrendingImage under the Avatar
					Children.Add(new TrendingImage(deviceInfo, RepositoryPageAutomationIds.SmallScreenTrendingImage)
						.Row(Row.SeparatorPadding).Column(Column.Avatar).RowSpan(2).ColumnSpan(3)
						.Bind(IsVisibleProperty,
							binding1: new Binding(nameof(Repository.IsTrending), BindingMode.OneWay),
							binding2: new Binding(nameof(Width), BindingMode.OneWay, source: largeScreenTrendingImage),
							binding3: new Binding(nameof(Repository.IsFavorite), BindingMode.OneWay),
							convert: static ((bool IsTrending, double Width, bool? IsFavorite) inputs) => IsTrendingImageVisible(inputs.IsTrending, inputs.Width, inputs.IsFavorite, largeScreenTrendingImageWidth => largeScreenTrendingImageWidth >= (TrendingImage.SvgWidthRequest + 8))));

					foreach (var child in dataTemplateChildren)
					{
						Children.Add(child);
					}
				}

				sealed class NameLabel : PrimaryColorLabel
				{
					public NameLabel() : base(20)
					{
						LineBreakMode = LineBreakMode.TailTruncation;
						HorizontalOptions = LayoutOptions.Fill;
						FontFamily = FontFamilyConstants.RobotoBold;
					}
				}

				sealed class DescriptionLabel : PrimaryColorLabel
				{
					public DescriptionLabel() : base(14)
					{
						MaxLines = 2;
						LineHeight = 1.16;
						FontFamily = FontFamilyConstants.RobotoRegular;
					}
				}

				abstract class PrimaryColorLabel : Label
				{
					protected PrimaryColorLabel(in double fontSize)
					{
						FontSize = fontSize;
						LineBreakMode = LineBreakMode.TailTruncation;
						HorizontalTextAlignment = TextAlignment.Start;
						VerticalTextAlignment = TextAlignment.Start;

						this.DynamicResource(TextColorProperty, nameof(BaseTheme.PrimaryTextColor));
					}
				}

				sealed class Separator : BoxView
				{
					public Separator() => this.DynamicResource(ColorProperty, nameof(BaseTheme.SeparatorColor));
				}

				sealed class TrendingImage : SvgImage
				{
					public const double SvgWidthRequest = 62;
					public const double SvgHeightRequest = 16;

					public TrendingImage(IDeviceInfo deviceInfo, string automationId) : base(deviceInfo, SvgWidthRequest, SvgHeightRequest)
					{
						AutomationId = automationId;
						HorizontalOptions = LayoutOptions.Start;
						VerticalOptions = LayoutOptions.End;

						this.Bind(SourceProperty,
							getter: static (Repository repository) => repository.IsTrending,
							mode: BindingMode.OneTime,
							convert: static isTrending => isTrending ? "trending_tag.svg" : "favorite_tag.svg");

						this.Bind(GetSvgColorProperty,
							getter: static (Repository repository) => repository.IsFavorite,
							mode: BindingMode.OneTime,
							convert: static isFavorite => isFavorite is true
								? (Func<Color>)(() => AppResources.GetResource<Color>(nameof(BaseTheme.CardStarsStatsIconColor)))
								: (Func<Color>)(() => AppResources.GetResource<Color>(nameof(BaseTheme.CardTrendingStatsColor))));
					}
				}

				static bool IsTrendingImageVisible(bool isTrending, double width, bool? isFavorite, Func<double, bool> isWidthValid)
				{
					// When `Width is -1`, .NET MAUI hasn't inflated the View
					// Allow .NET MAUI to inflate the view, then validate its Width
					return (isTrending || isFavorite is true)
						   && (width is -1 || isWidthValid(width));
				}
			}
		}
	}

	protected class StatisticsLabel : Label
	{
		public const int StatisticsFontSize = 12;

		public StatisticsLabel(in string textColorThemeName)
		{
			FontSize = StatisticsFontSize;

			HorizontalOptions = LayoutOptions.Fill;

			HorizontalTextAlignment = TextAlignment.Start;
			VerticalTextAlignment = TextAlignment.End;

			HorizontalOptions = LayoutOptions.Start;
			VerticalOptions = LayoutOptions.Center;

			LineBreakMode = LineBreakMode.TailTruncation;

			Padding = new Thickness(2, 0, 0, 0);

			this.DynamicResource(TextColorProperty, textColorThemeName);
		}
	}
}