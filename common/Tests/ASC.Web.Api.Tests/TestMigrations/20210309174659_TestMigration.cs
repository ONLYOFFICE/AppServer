using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASC.Core.Common.Migrations.MySql.TenantDbContextMySql
{
    public partial class TestMigration : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.UpdateData(
                table: "core_user",
                keyColumn: "id",
                keyValue: "005bb3ff-7de3-47d2-9b3d-61b9ec8a76a5",
                columns: new[] { "last_modified", "workfromdate" },
                values: new object[] { new DateTime(2021, 3, 9, 17, 46, 59, 90, DateTimeKind.Utc).AddTicks(9616), new DateTime(2021, 3, 9, 17, 46, 59, 90, DateTimeKind.Utc).AddTicks(9613) });

            migrationBuilder.InsertData(
                table: "core_user",
                columns: new[] { "id", "activation_status", "bithdate", "contacts", "culture", "email", "firstname", "last_modified", "lastname", "location", "notes", "phone", "phone_activation", "removed", "sex", "sid", "sso_name_id", "sso_session_id", "status", "tenant", "terminateddate", "title", "username", "workfromdate" },
                values: new object[] { "99223c7b-e3c9-11eb-9063-982cbc0ea1e5", 0, null, null, null, "", "Test", new DateTime(2020, 12, 22, 15, 29, 45, 332, DateTimeKind.Utc).AddTicks(3070), "User", null, null, null, 0, false, null, null, null, null, 1, 1, null, null, "TestUser", new DateTime(2020, 12, 22, 15, 29, 45, 332, DateTimeKind.Utc).AddTicks(3053) });


            migrationBuilder.UpdateData(
                table: "core_user",
                keyColumn: "id",
                keyValue: "66faa6e4-f133-11ea-b126-00ffeec8b4ef",
                columns: new[] { "last_modified", "workfromdate" },
                values: new object[] { new DateTime(2021, 3, 9, 17, 46, 59, 90, DateTimeKind.Utc).AddTicks(9324), new DateTime(2021, 3, 9, 17, 46, 59, 90, DateTimeKind.Utc).AddTicks(8614) });
           
            migrationBuilder.InsertData(
                table: "core_usersecurity",
                columns: new[] { "userid", "pwdhash", "pwdhashsha512", "tenant" },
                values: new object[] { "99223c7b-e3c9-11eb-9063-982cbc0ea1e5", "", "USubvPlB+ogq0Q1trcSupg==", 1 });
           
            migrationBuilder.UpdateData(
                table: "tenants_tenants",
                keyColumn: "id",
                keyValue: 1,
                column: "creationdatetime",
                value: new DateTime(2021, 3, 9, 17, 46, 59, 97, DateTimeKind.Utc).AddTicks(4317));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "core_user",
                keyColumn: "id",
                keyValue: "005bb3ff-7de3-47d2-9b3d-61b9ec8a76a5",
                columns: new[] { "last_modified", "workfromdate" },
                values: new object[] { new DateTime(2021, 3, 9, 17, 44, 25, 638, DateTimeKind.Utc).AddTicks(9818), new DateTime(2021, 3, 9, 17, 44, 25, 638, DateTimeKind.Utc).AddTicks(9814) });

            migrationBuilder.UpdateData(
                table: "core_user",
                keyColumn: "id",
                keyValue: "66faa6e4-f133-11ea-b126-00ffeec8b4ef",
                columns: new[] { "last_modified", "workfromdate" },
                values: new object[] { new DateTime(2021, 3, 9, 17, 44, 25, 638, DateTimeKind.Utc).AddTicks(9220), new DateTime(2021, 3, 9, 17, 44, 25, 638, DateTimeKind.Utc).AddTicks(7799) });

            migrationBuilder.UpdateData(
                table: "tenants_tenants",
                keyColumn: "id",
                keyValue: 1,
                column: "creationdatetime",
                value: new DateTime(2021, 3, 9, 17, 44, 25, 651, DateTimeKind.Utc).AddTicks(4834));
        }
    }
}
