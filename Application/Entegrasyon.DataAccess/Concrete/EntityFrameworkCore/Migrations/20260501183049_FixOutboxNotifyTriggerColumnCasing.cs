using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class FixOutboxNotifyTriggerColumnCasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    CREATE OR REPLACE FUNCTION notify_outbox_insert()
    RETURNS TRIGGER AS $$
    BEGIN
        PERFORM pg_notify('notification_outbox_new', NEW.""Id""::text);
        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    CREATE OR REPLACE FUNCTION notify_outbox_insert()
    RETURNS TRIGGER AS $$
    BEGIN
        PERFORM pg_notify('notification_outbox_new', NEW.id::text);
        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;
");
        }
    }
}
