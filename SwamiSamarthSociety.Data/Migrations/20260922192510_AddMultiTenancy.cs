using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwamiSamarthSociety.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        // Drops whichever unique, non-primary-key index or unique constraint currently exists on
        // the given table -- used for the three legacy single-column uniques (MonthlyCycle
        // Year+Month, Member.MemberCode, Loan.LoanNumber) that predate this migration history and
        // so don't carry EF's "IX_<Table>_<Column>" naming convention. A unique constraint (as
        // opposed to a plain unique index) can't be dropped with DROP INDEX -- SQL Server requires
        // ALTER TABLE ... DROP CONSTRAINT for those, so this checks sys.key_constraints first.
        private static string DropUniqueIndexByTableSql(string table) => $@"
            DECLARE @ucName nvarchar(128);
            SELECT @ucName = kc.name FROM sys.key_constraints kc
                WHERE kc.parent_object_id = OBJECT_ID(N'[dbo].[{table}]') AND kc.type = 'UQ';
            IF @ucName IS NOT NULL
                EXEC('ALTER TABLE [{table}] DROP CONSTRAINT [' + @ucName + ']');
            ELSE
            BEGIN
                DECLARE @idxName nvarchar(128);
                SELECT @idxName = i.name FROM sys.indexes i
                    WHERE i.object_id = OBJECT_ID(N'[dbo].[{table}]') AND i.is_unique = 1 AND i.is_primary_key = 0;
                IF @idxName IS NOT NULL
                    EXEC('DROP INDEX [' + @idxName + '] ON [{table}]');
            END
        ";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renamed in place (not dropped/recreated) so the existing operational rows --
            // e.g. LastPaymentReminderSentDate -- survive the migration instead of being lost.
            migrationBuilder.RenameTable(
                name: "SocietySetting",
                newName: "AppSetting");

            migrationBuilder.RenameColumn(
                name: "SocietySettingId",
                table: "AppSetting",
                newName: "AppSettingId");

            // The table's original PK constraint may have a SQL-Server-generated name (e.g.
            // PK__SocietyS__...) rather than EF's "PK_SocietySetting" convention if it predates
            // this migration history, so look it up by table instead of assuming the name.
            migrationBuilder.Sql(@"
                DECLARE @pkName nvarchar(128);
                SELECT @pkName = kc.name FROM sys.key_constraints kc
                    WHERE kc.parent_object_id = OBJECT_ID(N'[dbo].[AppSetting]') AND kc.type = 'PK';
                IF @pkName IS NOT NULL AND @pkName <> N'PK_AppSetting'
                    EXEC sp_rename @pkName, N'PK_AppSetting', N'OBJECT';
            ");

            // These three tables' original unique constraints were created outside EF's
            // migration history (e.g. via a hand-written setup script) and don't carry EF's
            // "IX_<Table>_<Column>" naming convention -- look each one up by table instead of
            // assuming a name, same as the AppSetting primary key above.
            migrationBuilder.Sql(DropUniqueIndexByTableSql("MonthlyCycle"));
            migrationBuilder.Sql(DropUniqueIndexByTableSql("Member"));
            migrationBuilder.Sql(DropUniqueIndexByTableSql("Loan"));

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "SmsLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "MonthlyCycle",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "MemberSharePayment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "MemberExitSettlement",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "Member",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "LoanPayment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "LoanInstallmentRule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "LoanInstallment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "LoanGuarantor",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "Loan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "ImportLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "FinancialTransaction",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "BankTransaction",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "BankAccount",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "AuditLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SocietyId",
                table: "AppSetting",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Was nvarchar(max) on the old SocietySetting table -- SQL Server can't index an
            // (max) column, and Key now needs to be part of the SocietyId+Key unique index.
            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "AppSetting",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Society",
                columns: table => new
                {
                    SocietyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameMarathi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContactPersonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeactivatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Society", x => x.SocietyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyCycle_SocietyId_Year_Month",
                table: "MonthlyCycle",
                columns: new[] { "SocietyId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Member_SocietyId_MemberCode",
                table: "Member",
                columns: new[] { "SocietyId", "MemberCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loan_SocietyId_LoanNumber",
                table: "Loan",
                columns: new[] { "SocietyId", "LoanNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SocietyId",
                table: "AspNetUsers",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSetting_SocietyId_Key",
                table: "AppSetting",
                columns: new[] { "SocietyId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Society_Code",
                table: "Society",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Society_SocietyId",
                table: "AspNetUsers",
                column: "SocietyId",
                principalTable: "Society",
                principalColumn: "SocietyId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Society_SocietyId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Society");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyCycle_SocietyId_Year_Month",
                table: "MonthlyCycle");

            migrationBuilder.DropIndex(
                name: "IX_Member_SocietyId_MemberCode",
                table: "Member");

            migrationBuilder.DropIndex(
                name: "IX_Loan_SocietyId_LoanNumber",
                table: "Loan");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SocietyId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppSetting_SocietyId_Key",
                table: "AppSetting");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "SmsLog");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "MonthlyCycle");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "MemberSharePayment");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "MemberExitSettlement");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "Member");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "LoanPayment");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "LoanInstallmentRule");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "LoanInstallment");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "LoanGuarantor");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "Loan");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "ImportLog");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "FinancialTransaction");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "BankTransaction");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "BankAccount");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "AppSetting");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "AppSetting",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.Sql(@"
                DECLARE @pkName nvarchar(128);
                SELECT @pkName = kc.name FROM sys.key_constraints kc
                    WHERE kc.parent_object_id = OBJECT_ID(N'[dbo].[AppSetting]') AND kc.type = 'PK';
                IF @pkName IS NOT NULL AND @pkName <> N'PK_SocietySetting'
                    EXEC sp_rename @pkName, N'PK_SocietySetting', N'OBJECT';
            ");

            migrationBuilder.RenameColumn(
                name: "AppSettingId",
                table: "AppSetting",
                newName: "SocietySettingId");

            migrationBuilder.RenameTable(
                name: "AppSetting",
                newName: "SocietySetting");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyCycle_Year_Month",
                table: "MonthlyCycle",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Member_MemberCode",
                table: "Member",
                column: "MemberCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loan_LoanNumber",
                table: "Loan",
                column: "LoanNumber",
                unique: true);
        }
    }
}
