using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FixCampus.Web.Data.Migrations;

[DbContext(typeof(FixCampusDbContext))]
[Migration("20261029150000_AddProfileEducation")]
public class AddProfileEducation : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
        => ProfileEducationModel.Build(modelBuilder);

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Bolum", table: "Profiller", type: "character varying(200)",
            maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "Sinif", table: "Profiller", type: "character varying(30)",
            maxLength: 30, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Bolum", table: "Profiller");
        migrationBuilder.DropColumn(name: "Sinif", table: "Profiller");
    }
}
