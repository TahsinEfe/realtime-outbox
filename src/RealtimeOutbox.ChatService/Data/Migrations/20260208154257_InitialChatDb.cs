using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealtimeOutbox.ChatService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialChatDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                columns: table => new
                {
                    OutboxEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.OutboxEventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_messages_TenantId_ChannelId_CreatedAtUtc",
                table: "messages",
                columns: new[] { "TenantId", "ChannelId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_SentAtUtc_OccurredAtUtc",
                table: "outbox_events",
                columns: new[] { "SentAtUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_TenantId_SentAtUtc",
                table: "outbox_events",
                columns: new[] { "TenantId", "SentAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "outbox_events");
        }
    }
}
