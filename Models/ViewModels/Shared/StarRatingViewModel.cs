// File: Models/ViewModels/Shared/StarRatingViewModel.cs
namespace SportBooking.Models.ViewModels.Shared;

/// <summary>
/// Model cho partial _StarRating: hiển thị số sao và (tùy chọn) con điểm, số lượng đánh giá.
/// </summary>
public class StarRatingViewModel
{
    /// <summary>Điểm trung bình hoặc số sao (0 - 5).</summary>
    public double Rating { get; set; }

    /// <summary>Có hiển thị con điểm "4.5" cạnh ngôi sao hay không.</summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>Số lượng đánh giá hiển thị trong ngoặc, null là ẩn.</summary>
    public int? Count { get; set; }

    /// <summary>Class Bootstrap cho cỡ ngôi sao (fs-5, fs-6...).</summary>
    public string StarClass { get; set; } = "fs-6";

    public StarRatingViewModel()
    {
    }

    public StarRatingViewModel(double rating)
    {
        Rating = rating;
    }

    public StarRatingViewModel(int rating)
    {
        Rating = rating;
    }
}
