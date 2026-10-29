using FixCampus.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Data;

public class FixCampusDbContext(DbContextOptions<FixCampusDbContext> options) : DbContext(options)
{
    public DbSet<Profil> Profiller => Set<Profil>();
    public DbSet<Kategori> Kategoriler => Set<Kategori>();
    public DbSet<Ilan> Ilanlar => Set<Ilan>();
    public DbSet<Katilim> Katilimlar => Set<Katilim>();
    public DbSet<IlanFotografi> IlanFotograflari => Set<IlanFotografi>();
    public DbSet<Sikayet> Sikayetler => Set<Sikayet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profil>(b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<string>("AdSoyad").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
            b.Property(x => x.Bolum).HasColumnType("character varying(200)").HasMaxLength(200);
            b.Property(x => x.Sinif).HasColumnType("character varying(30)").HasMaxLength(30);
            b.Property<string>("Rol").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
            b.Property<DateTimeOffset>("OlusturulmaTarihi").HasColumnType("timestamp with time zone").IsRequired();
            b.HasKey("Id");
            b.ToTable("Profiller", t =>
            {
                t.HasCheckConstraint("CK_Profiller_Rol", "\"Rol\" IN ('Ogrenci', 'Yonetici')");
            });
        });

        modelBuilder.Entity<Kategori>(b =>
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

        modelBuilder.Entity<Ilan>(b =>
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

        modelBuilder.Entity<Katilim>(b =>
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

        modelBuilder.Entity<IlanFotografi>(b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid").ValueGeneratedNever().IsRequired();
            b.Property<Guid>("IlanId").HasColumnType("uuid").IsRequired();
            b.Property<string>("DosyaYolu").HasColumnType("character varying(500)").HasMaxLength(500).IsRequired();
            b.Property<DateTimeOffset>("YuklemeTarihi").HasColumnType("timestamp with time zone").IsRequired();
            b.HasKey("Id");
            b.HasIndex("IlanId").IsUnique();
            b.ToTable("IlanFotograflari");
        });

        modelBuilder.Entity<Sikayet>(b =>
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

        modelBuilder.Entity<Ilan>().HasOne(x => x.Sahip)
            .WithMany(x => x.Ilanlar).HasForeignKey(x => x.SahipId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Ilan>().HasOne(x => x.Kategori)
            .WithMany(x => x.Ilanlar).HasForeignKey(x => x.KategoriId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Katilim>().HasOne(x => x.Ilan)
            .WithMany(x => x.Katilimlar).HasForeignKey(x => x.IlanId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Katilim>().HasOne(x => x.Profil)
            .WithMany(x => x.Katilimlar).HasForeignKey(x => x.ProfilId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<IlanFotografi>().HasOne(x => x.Ilan)
            .WithMany(x => x.Fotograflar).HasForeignKey(x => x.IlanId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Sikayet>().HasOne(x => x.Ilan)
            .WithMany(x => x.Sikayetler).HasForeignKey(x => x.IlanId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Sikayet>().HasOne(x => x.Bildiren)
            .WithMany(x => x.Sikayetler).HasForeignKey(x => x.BildirenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
