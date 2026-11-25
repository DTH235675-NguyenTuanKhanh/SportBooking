using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportBooking.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerBankTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManualRefundNote",
                table: "Payments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManualRefundedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualRefundedById",
                table: "Payments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankTransferGroupCode",
                table: "Bookings",
                type: "nvarchar(30)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransferImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Png = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankTransfers",
                columns: table => new
                {
                    GroupCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ComplexId = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QrImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    ReportDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReportedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransfers", x => x.GroupCode);
                    table.ForeignKey(
                        name: "FK_BankTransfers_TransferImages_QrImageId",
                        column: x => x.QrImageId,
                        principalTable: "TransferImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerTransferProfiles",
                columns: table => new
                {
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QrImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerTransferProfiles", x => x.OwnerId);
                    table.ForeignKey(
                        name: "FK_OwnerTransferProfiles_TransferImages_QrImageId",
                        column: x => x.QrImageId,
                        principalTable: "TransferImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerTransferProfiles_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupCode = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandledById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HandledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandlingNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferReports_BankTransfers_GroupCode",
                        column: x => x.GroupCode,
                        principalTable: "BankTransfers",
                        principalColumn: "GroupCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferReports_TransferImages_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "TransferImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BankTransferGroupCode",
                table: "Bookings",
                column: "BankTransferGroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransfers_QrImageId",
                table: "BankTransfers",
                column: "QrImageId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransfers_Reference",
                table: "BankTransfers",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerTransferProfiles_QrImageId",
                table: "OwnerTransferProfiles",
                column: "QrImageId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferReports_GroupCode",
                table: "TransferReports",
                column: "GroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_TransferReports_ReceiptId",
                table: "TransferReports",
                column: "ReceiptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BankTransfers_BankTransferGroupCode",
                table: "Bookings",
                column: "BankTransferGroupCode",
                principalTable: "BankTransfers",
                principalColumn: "GroupCode",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BankTransfers_BankTransferGroupCode",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "OwnerTransferProfiles");

            migrationBuilder.DropTable(
                name: "TransferReports");

            migrationBuilder.DropTable(
                name: "BankTransfers");

            migrationBuilder.DropTable(
                name: "TransferImages");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BankTransferGroupCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ManualRefundNote",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ManualRefundedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ManualRefundedById",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BankTransferGroupCode",
                table: "Bookings");
        }
    }
}
