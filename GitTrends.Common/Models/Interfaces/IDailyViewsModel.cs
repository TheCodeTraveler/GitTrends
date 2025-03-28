namespace GitTrends.Common;

public interface IDailyViewsModel
{
	DateTime LocalDay { get; }

	DateTimeOffset Day { get; }

	long TotalViews { get; }

	long TotalUniqueViews { get; }
}