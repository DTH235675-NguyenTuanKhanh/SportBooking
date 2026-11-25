using SportBooking.Models.ViewModels.Court;

namespace SportBooking.Models.ViewModels.Shared;

public class SportPickerViewModel
{
    public List<SportTypeOption> Options { get; set; } = new();
    public int? SelectedId { get; set; }
}
