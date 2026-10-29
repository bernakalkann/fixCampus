using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Data.Migrations;

internal static class ProfileEducationModel
{
    internal static void Build(ModelBuilder modelBuilder)
    {
        InitialDataModel.Build(modelBuilder);
        modelBuilder.Entity("FixCampus.Web.Models.Entities.Profil", b =>
        {
            b.Property<string>("Bolum").HasColumnType("character varying(200)").HasMaxLength(200);
            b.Property<string>("Sinif").HasColumnType("character varying(30)").HasMaxLength(30);
        });
    }
}
