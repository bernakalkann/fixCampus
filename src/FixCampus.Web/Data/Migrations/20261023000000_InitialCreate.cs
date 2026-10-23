using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FixCampus.Web.Data.Migrations;

[DbContext(typeof(FixCampusDbContext))]
[Migration("20261023000000_InitialCreate")]
public class InitialCreate : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
        => InitialDataModel.Build(modelBuilder);

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Profiller",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdSoyad = table.Column<string>(type: "character varying(100)", nullable: false, maxLength: 100),
                Rol = table.Column<string>(type: "character varying(20)", nullable: false, maxLength: 20),
                OlusturulmaTarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Profiller", x => x.Id);
                table.CheckConstraint("CK_Profiller_Rol", "\"Rol\" IN ('Ogrenci', 'Yonetici')");
            });

        migrationBuilder.CreateTable(
            name: "Kategoriler",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Ad = table.Column<string>(type: "character varying(80)", nullable: false, maxLength: 80),
                Tur = table.Column<string>(type: "character varying(20)", nullable: false, maxLength: 20),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Kategoriler", x => x.Id);
                table.CheckConstraint("CK_Kategoriler_Tur", "\"Tur\" IN ('Sosyal', 'Kayip', 'Buluntu')");
            });

        migrationBuilder.CreateTable(
            name: "Ilanlar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SahipId = table.Column<Guid>(type: "uuid", nullable: false),
                KategoriId = table.Column<Guid>(type: "uuid", nullable: false),
                Baslik = table.Column<string>(type: "character varying(150)", nullable: false, maxLength: 150),
                Aciklama = table.Column<string>(type: "character varying(3000)", nullable: false, maxLength: 3000),
                Tur = table.Column<string>(type: "character varying(20)", nullable: false, maxLength: 20),
                Yer = table.Column<string>(type: "character varying(200)", nullable: false, maxLength: 200),
                BulusmaTarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Durum = table.Column<string>(type: "character varying(20)", nullable: false, maxLength: 20),
                OlusturulmaTarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ilanlar", x => x.Id);
                table.CheckConstraint("CK_Ilanlar_Tur", "\"Tur\" IN ('Sosyal', 'Kayip', 'Buluntu')");
                table.CheckConstraint("CK_Ilanlar_Durum", "\"Durum\" IN ('Aktif', 'Cozuldu', 'Gizli')");
                table.CheckConstraint("CK_Ilanlar_BulusmaTarihi", "(\"Tur\" = 'Sosyal' AND \"BulusmaTarihi\" IS NOT NULL) OR (\"Tur\" <> 'Sosyal' AND \"BulusmaTarihi\" IS NULL)");
                table.ForeignKey("FK_Ilanlar_Profiller_SahipId", x => x.SahipId,
                    "Profiller", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Ilanlar_Kategoriler_KategoriId", x => x.KategoriId,
                    "Kategoriler", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Katilimlar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IlanId = table.Column<Guid>(type: "uuid", nullable: false),
                ProfilId = table.Column<Guid>(type: "uuid", nullable: false),
                KatilimTarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Katilimlar", x => x.Id);
                table.ForeignKey("FK_Katilimlar_Ilanlar_IlanId", x => x.IlanId,
                    "Ilanlar", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Katilimlar_Profiller_ProfilId", x => x.ProfilId,
                    "Profiller", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "IlanFotograflari",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IlanId = table.Column<Guid>(type: "uuid", nullable: false),
                DosyaYolu = table.Column<string>(type: "character varying(500)", nullable: false, maxLength: 500),
                YuklemeTarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IlanFotograflari", x => x.Id);
                table.ForeignKey("FK_IlanFotograflari_Ilanlar_IlanId", x => x.IlanId,
                    "Ilanlar", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Sikayetler",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IlanId = table.Column<Guid>(type: "uuid", nullable: false),
                BildirenId = table.Column<Guid>(type: "uuid", nullable: false),
                Neden = table.Column<string>(type: "character varying(1000)", nullable: false, maxLength: 1000),
                Durum = table.Column<string>(type: "character varying(20)", nullable: false, maxLength: 20),
                OlusturulmaTarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sikayetler", x => x.Id);
                table.CheckConstraint("CK_Sikayetler_Durum", "\"Durum\" IN ('Bekliyor', 'Incelendi')");
                table.ForeignKey("FK_Sikayetler_Ilanlar_IlanId", x => x.IlanId,
                    "Ilanlar", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Sikayetler_Profiller_BildirenId", x => x.BildirenId,
                    "Profiller", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Kategoriler_Ad_Tur", table: "Kategoriler",
            columns: new[] { "Ad", "Tur" }, unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Ilanlar_SahipId", table: "Ilanlar",
            columns: new[] { "SahipId" }, unique: false);
        migrationBuilder.CreateIndex(
            name: "IX_Ilanlar_KategoriId", table: "Ilanlar",
            columns: new[] { "KategoriId" }, unique: false);
        migrationBuilder.CreateIndex(
            name: "IX_Ilanlar_Tur_Durum_BulusmaTarihi", table: "Ilanlar",
            columns: new[] { "Tur", "Durum", "BulusmaTarihi" }, unique: false);
        migrationBuilder.CreateIndex(
            name: "IX_Katilimlar_IlanId_ProfilId", table: "Katilimlar",
            columns: new[] { "IlanId", "ProfilId" }, unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Katilimlar_ProfilId", table: "Katilimlar",
            columns: new[] { "ProfilId" }, unique: false);
        migrationBuilder.CreateIndex(
            name: "IX_IlanFotograflari_IlanId", table: "IlanFotograflari",
            columns: new[] { "IlanId" }, unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Sikayetler_IlanId_BildirenId", table: "Sikayetler",
            columns: new[] { "IlanId", "BildirenId" }, unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Sikayetler_BildirenId", table: "Sikayetler",
            columns: new[] { "BildirenId" }, unique: false);

        // Direct server access only; no client Data API policy is granted by this migration.
        migrationBuilder.Sql("ALTER TABLE \"Profiller\" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Kategoriler\" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Ilanlar\" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Katilimlar\" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"IlanFotograflari\" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Sikayetler\" ENABLE ROW LEVEL SECURITY;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Sikayetler");
        migrationBuilder.DropTable(name: "IlanFotograflari");
        migrationBuilder.DropTable(name: "Katilimlar");
        migrationBuilder.DropTable(name: "Ilanlar");
        migrationBuilder.DropTable(name: "Kategoriler");
        migrationBuilder.DropTable(name: "Profiller");
    }
}
