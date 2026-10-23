using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Data.Migrations;

internal static class InitialDataModel
{
    internal static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);
        modelBuilder.UseIdentityByDefaultColumns();
        modelBuilder.Entity("FixCampus.Web.Models.Entities.Profil", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<string>("AdSoyad").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
            b.Property<string>("Rol").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
            b.Property<DateTimeOffset>("OlusturulmaTarihi").HasColumnType("timestamp with time zone").IsRequired();
            b.HasKey("Id");
            b.ToTable("Profiller", t =>
            {
                t.HasCheckConstraint("CK_Profiller_Rol", "\"Rol\" IN ('Ogrenci', 'Yonetici')");
            });
        });

        modelBuilder.Entity("FixCampus.Web.Models.Entities.Kategori", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<string>("Ad").HasColumnType("character varying(80)").HasMaxLength(80).IsRequired();
            b.Property<string>("Tur").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
            b.HasKey("Id");
            b.HasIndex("Ad", "Tur").IsUnique();
            b.ToTable("Kategoriler", t =>
            {
                t.HasCheckConstraint("CK_Kategoriler_Tur", "\"Tur\" IN ('Sosyal', 'Kayip', 'Buluntu')");
            });
        });

        modelBuilder.Entity("FixCampus.Web.Models.Entities.Ilan", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<Guid>("SahipId").HasColumnType("uuid").IsRequired();
            b.Property<Guid>("KategoriId").HasColumnType("uuid").IsRequired();
            b.Property<string>("Baslik").HasColumnType("character varying(150)").HasMaxLength(150).IsRequired();
            b.Property<string>("Aciklama").HasColumnType("character varying(3000)").HasMaxLength(3000).IsRequired();
            b.Property<string>("Tur").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
            b.Property<string>("Yer").HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            b.Property<DateTimeOffset?>("BulusmaTarihi").HasColumnType("timestamp with time zone");
            b.Property<string>("Durum").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
            b.Property<DateTimeOffset>("OlusturulmaTarihi").HasColumnType("timestamp with time zone").IsRequired();
            b.HasKey("Id");
            b.HasIndex("SahipId");
            b.HasIndex("KategoriId");
            b.HasIndex("Tur", "Durum", "BulusmaTarihi");
            b.ToTable("Ilanlar", t =>
            {
                t.HasCheckConstraint("CK_Ilanlar_Tur", "\"Tur\" IN ('Sosyal', 'Kayip', 'Buluntu')");
                t.HasCheckConstraint("CK_Ilanlar_Durum", "\"Durum\" IN ('Aktif', 'Cozuldu', 'Gizli')");
                t.HasCheckConstraint("CK_Ilanlar_BulusmaTarihi", "(\"Tur\" = 'Sosyal' AND \"BulusmaTarihi\" IS NOT NULL) OR (\"Tur\" <> 'Sosyal' AND \"BulusmaTarihi\" IS NULL)");
            });
        });

        modelBuilder.Entity("FixCampus.Web.Models.Entities.Katilim", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<Guid>("IlanId").HasColumnType("uuid").IsRequired();
            b.Property<Guid>("ProfilId").HasColumnType("uuid").IsRequired();
            b.Property<DateTimeOffset>("KatilimTarihi").HasColumnType("timestamp with time zone").IsRequired();
            b.HasKey("Id");
            b.HasIndex("IlanId", "ProfilId").IsUnique();
            b.HasIndex("ProfilId");
            b.ToTable("Katilimlar");
        });

        modelBuilder.Entity("FixCampus.Web.Models.Entities.IlanFotografi", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<Guid>("IlanId").HasColumnType("uuid").IsRequired();
            b.Property<string>("DosyaYolu").HasColumnType("character varying(500)").HasMaxLength(500).IsRequired();
            b.Property<DateTimeOffset>("YuklemeTarihi").HasColumnType("timestamp with time zone").IsRequired();
            b.HasKey("Id");
            b.HasIndex("IlanId").IsUnique();
            b.ToTable("IlanFotograflari");
        });

        modelBuilder.Entity("FixCampus.Web.Models.Entities.Sikayet", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<Guid>("IlanId").HasColumnType("uuid").IsRequired();
            b.Property<Guid>("BildirenId").HasColumnType("uuid").IsRequired();
            b.Property<string>("Neden").HasColumnType("character varying(1000)").HasMaxLength(1000).IsRequired();
            b.Property<string>("Durum").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
            b.Property<DateTimeOffset>("OlusturulmaTarihi").HasColumnType("timestamp with time zone").IsRequired();
            b.HasKey("Id");
            b.HasIndex("IlanId", "BildirenId").IsUnique();
            b.HasIndex("BildirenId");
            b.ToTable("Sikayetler", t =>
            {
                t.HasCheckConstraint("CK_Sikayetler_Durum", "\"Durum\" IN ('Bekliyor', 'Incelendi')");
            });
        });

        modelBuilder.Entity("FixCampus.Web.Models.Entities.Ilan")
            .HasOne("FixCampus.Web.Models.Entities.Profil", "Sahip")
            .WithMany("Ilanlar").HasForeignKey("SahipId")
            .OnDelete(DeleteBehavior.Restrict).IsRequired();
        modelBuilder.Entity("FixCampus.Web.Models.Entities.Ilan")
            .HasOne("FixCampus.Web.Models.Entities.Kategori", "Kategori")
            .WithMany("Ilanlar").HasForeignKey("KategoriId")
            .OnDelete(DeleteBehavior.Restrict).IsRequired();
        modelBuilder.Entity("FixCampus.Web.Models.Entities.Katilim")
            .HasOne("FixCampus.Web.Models.Entities.Ilan", "Ilan")
            .WithMany("Katilimlar").HasForeignKey("IlanId")
            .OnDelete(DeleteBehavior.Cascade).IsRequired();
        modelBuilder.Entity("FixCampus.Web.Models.Entities.Katilim")
            .HasOne("FixCampus.Web.Models.Entities.Profil", "Profil")
            .WithMany("Katilimlar").HasForeignKey("ProfilId")
            .OnDelete(DeleteBehavior.Restrict).IsRequired();
        modelBuilder.Entity("FixCampus.Web.Models.Entities.IlanFotografi")
            .HasOne("FixCampus.Web.Models.Entities.Ilan", "Ilan")
            .WithMany("Fotograflar").HasForeignKey("IlanId")
            .OnDelete(DeleteBehavior.Cascade).IsRequired();
        modelBuilder.Entity("FixCampus.Web.Models.Entities.Sikayet")
            .HasOne("FixCampus.Web.Models.Entities.Ilan", "Ilan")
            .WithMany("Sikayetler").HasForeignKey("IlanId")
            .OnDelete(DeleteBehavior.Cascade).IsRequired();
        modelBuilder.Entity("FixCampus.Web.Models.Entities.Sikayet")
            .HasOne("FixCampus.Web.Models.Entities.Profil", "Bildiren")
            .WithMany("Sikayetler").HasForeignKey("BildirenId")
            .OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
