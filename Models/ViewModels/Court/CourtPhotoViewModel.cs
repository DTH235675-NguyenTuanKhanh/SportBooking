namespace SportBooking.Models.ViewModels.Court;

public record CourtPhotoViewModel(string Name, string SportTypeName, string? Description, string? ImageUrl, bool IsDetail = false);
