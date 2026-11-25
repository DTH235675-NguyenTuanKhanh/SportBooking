using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.Entities;

public sealed class TransferImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] Png { get; set; } = [];
}

public sealed class OwnerTransferProfile
{
    [Key] public string OwnerId { get; set; } = "";
    public bool Enabled { get; set; }
    [MaxLength(80)] public string BankName { get; set; } = "";
    [MaxLength(30)] public string AccountNumber { get; set; } = "";
    [MaxLength(100)] public string AccountName { get; set; } = "";
    public Guid QrImageId { get; set; }
    public TransferImage QrImage { get; set; } = null!;
}

public enum TransferState
{
    AwaitingReport, Reported, NeedsInfo, Received, ReceivedLate
}

public sealed class BankTransfer
{
    [Key, MaxLength(30)] public string GroupCode { get; set; } = "";
    [MaxLength(30)] public string Reference { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public int ComplexId { get; set; }
    [MaxLength(80)] public string BankName { get; set; } = "";
    [MaxLength(30)] public string AccountNumber { get; set; } = "";
    [MaxLength(100)] public string AccountName { get; set; } = "";
    public Guid QrImageId { get; set; }
    public TransferImage QrImage { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransferState State { get; set; }
    public DateTime ReportDeadline { get; set; }
    public DateTime? LastReportedAt { get; set; }
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<TransferReport> Reports { get; set; } = [];
}

public sealed class TransferReport
{
    public int Id { get; set; }
    public string GroupCode { get; set; } = "";
    public BankTransfer Transfer { get; set; } = null!;
    public DateTime ReportedAt { get; set; }
    public Guid? ReceiptId { get; set; }
    public TransferImage? Receipt { get; set; }
    [MaxLength(450)] public string? HandledById { get; set; }
    public DateTime? HandledAt { get; set; }
    [MaxLength(300)] public string? HandlingNote { get; set; }
    public TransferState? Outcome { get; set; }
}
