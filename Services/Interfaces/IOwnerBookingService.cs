using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;

namespace SportBooking.Services.Interfaces;

public interface IOwnerBookingService
{
    Task<OwnerBookingListViewModel> GetBookingsAsync(string ownerId, OwnerBookingListViewModel filter);
    Task<ServiceResult> ConfirmCounterPaymentAsync(string ownerId, string bookingGroupCode);
    Task<ServiceResult> UpdateBookingStatusAsync(string ownerId, int bookingId, BookingStatus targetStatus);
    Task<ServiceResult> ConfirmCounterRefundAsync(string ownerId, int bookingId);
}
