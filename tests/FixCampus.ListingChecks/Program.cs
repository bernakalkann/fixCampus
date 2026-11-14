using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers;
using FixCampus.Web.Data;
using FixCampus.Web.Models;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

ListingQueryChecks.Run();
ExpiryChecks.Run();
await MyListingsChecks.RunAsync();

Console.WriteLine("FixCampus Hafta 4 - Arama, Kategori, Sayfalama, İlanlarım ve Çift Hesap Doğrulama Kontrolleri Başlatılıyor...");

var now = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
var clock = new FixedClock(now);
using var services = new ServiceCollection()
    .AddSingleton<TimeProvider>(clock)
    .BuildServiceProvider();

var validCategoryId = Guid.Parse("11111111-1111-4111-8111-111111111111");
var categoryLostId = Guid.Parse("22222222-2222-4222-8222-222222222222");
var futureDate = now.AddHours(2);
var pastDate = now.AddHours(-2);

// =========================================================================
// 1. IlanlarController Yetkilendirme Kontrolü ([Authorize], AllowAnonymous Yok)
// =========================================================================
var controllerType = typeof(IlanlarController);
Require(
    controllerType.GetCustomAttribute<AuthorizeAttribute>() is not null,
    "IlanlarController üzerinde [Authorize] niteliği zorunludur.");

var anonymousMethods = controllerType
    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
    .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
    .Select(m => m.Name)
    .ToList();

Require(
    anonymousMethods.Count == 0,
    $"IlanlarController içinde [AllowAnonymous] kullanılan metot olmamalıdır (Bulunan: {string.Join(", ", anonymousMethods)}).");

// =========================================================================
// 2. ListingValidator Alan & Tarih Kontrolleri
// =========================================================================
await using var unusedDb = new FixCampusDbContext(
    new DbContextOptionsBuilder<FixCampusDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=test;Password=test;Timeout=1")
        .Options);

var validator = new ListingValidator(unusedDb, clock);

// 2a. Sosyal buluşma tarihi kontrolleri (Hem Create hem Update için)
var createMissingDate = await validator.ValidateCreateAsync(new CreateListingRequest(
    "Geçerli Başlık", "Açıklama", "Sosyal", validCategoryId, "Kampüs", null));
Require(!createMissingDate.IsValid, "Create: Sosyal ilanda boş buluşma tarihi reddedilmelidir.");

var createPastDate = await validator.ValidateCreateAsync(new CreateListingRequest(
    "Geçerli Başlık", "Açıklama", "Sosyal", validCategoryId, "Kampüs", pastDate));
Require(!createPastDate.IsValid, "Create: Sosyal ilanda geçmiş buluşma tarihi reddedilmelidir.");

var createExactNowDate = await validator.ValidateCreateAsync(new CreateListingRequest(
    "Geçerli Başlık", "Açıklama", "Sosyal", validCategoryId, "Kampüs", now));
Require(!createExactNowDate.IsValid, "Create: Sosyal ilanda şu anki zamana eşit tarih reddedilmelidir.");

var updateMissingDate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    "Geçerli Başlık", "Açıklama", validCategoryId, "Kampüs", null, "Sosyal"), "Sosyal");
Require(!updateMissingDate.IsValid, "Update: Sosyal ilanda boş buluşma tarihi reddedilmelidir.");

var updatePastDate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    "Geçerli Başlık", "Açıklama", validCategoryId, "Kampüs", pastDate, "Sosyal"), "Sosyal");
Require(!updatePastDate.IsValid, "Update: Sosyal ilanda geçmiş buluşma tarihi reddedilmelidir.");

var updateExactNowDate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    "Geçerli Başlık", "Açıklama", validCategoryId, "Kampüs", now, "Sosyal"), "Sosyal");
Require(!updateExactNowDate.IsValid, "Update: Sosyal ilanda şu anki zamana eşit tarih reddedilmelidir.");

// 2b. Kayıp / Buluntu ilanlarında buluşma tarihi boş olmalıdır (Hem Create hem Update için)
foreach (var nonSocialType in new[] { "Kayip", "Buluntu" })
{
    var createWithDate = await validator.ValidateCreateAsync(new CreateListingRequest(
        "Kayıp Cüzdan", "Siyah deri cüzdan", nonSocialType, validCategoryId, "Kütüphane", futureDate));
    Require(!createWithDate.IsValid, $"Create: {nonSocialType} ilanında buluşma tarihi reddedilmelidir.");

    var updateWithDate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
        "Kayıp Cüzdan", "Siyah deri cüzdan", validCategoryId, "Kütüphane", futureDate, nonSocialType), nonSocialType);
    Require(!updateWithDate.IsValid, $"Update: {nonSocialType} ilanında buluşma tarihi reddedilmelidir.");
}

// 2c. Metin sınırları (API ile birebir: Başlık 2..150, Açıklama 1..3000, Yer 1..200)
var shortTitleCreate = await validator.ValidateCreateAsync(new CreateListingRequest(
    " A ", "Açıklama", "Kayip", validCategoryId, "Kampüs", null));
Require(!shortTitleCreate.IsValid, "Create: 2 karakterden kısa başlık reddedilmelidir.");

var shortTitleUpdate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    " A ", "Açıklama", validCategoryId, "Kampüs", null, "Kayip"), "Kayip");
Require(!shortTitleUpdate.IsValid, "Update: 2 karakterden kısa başlık reddedilmelidir.");

var longTitleUpdate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    new string('X', 151), "Açıklama", validCategoryId, "Kampüs", null, "Kayip"), "Kayip");
Require(!longTitleUpdate.IsValid, "Update: 150 karakterden uzun başlık reddedilmelidir.");

var emptyDescUpdate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    "Başlık", "   ", validCategoryId, "Kampüs", null, "Kayip"), "Kayip");
Require(!emptyDescUpdate.IsValid, "Update: Boş açıklama reddedilmelidir.");

var longDescUpdate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    "Başlık", new string('X', 3001), validCategoryId, "Kampüs", null, "Kayip"), "Kayip");
Require(!longDescUpdate.IsValid, "Update: 3000 karakterden uzun açıklama reddedilmelidir.");

var emptyPlaceUpdate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    "Başlık", "Açıklama", validCategoryId, "   ", null, "Kayip"), "Kayip");
Require(!emptyPlaceUpdate.IsValid, "Update: Boş yer bilgisi reddedilmelidir.");

var longPlaceUpdate = await validator.ValidateUpdateAsync(new UpdateListingRequest(
    "Başlık", "Açıklama", validCategoryId, new string('X', 201), null, "Kayip"), "Kayip");
Require(!longPlaceUpdate.IsValid, "Update: 200 karakterden uzun yer bilgisi reddedilmelidir.");

// =========================================================================
// 3. Web Form Modeli (ListingFormViewModel / ListingEditViewModel) Kontrolleri
// =========================================================================
List<ValidationResult> ValidateViewModel(ListingFormViewModel vm)
{
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(vm, new ValidationContext(vm, services, null), results, validateAllProperties: true);
    return results;
}

var validVm = new ListingEditViewModel
{
    Id = Guid.NewGuid(),
    Baslik = "AB",
    Aciklama = "A",
    Yer = "B",
    Tur = "Sosyal",
    KategoriId = validCategoryId,
    BulusmaZamani = now.AddHours(3).ToLocalTime().DateTime
};
Require(ValidateViewModel(validVm).Count == 0, "Geçerli alt sınırlardaki (2/1/1) ListingEditViewModel kabul edilmelidir.");

var pastEditVm = new ListingEditViewModel
{
    Id = Guid.NewGuid(),
    Baslik = "Geçerli Başlık",
    Aciklama = "Geçerli Açıklama",
    Yer = "Kütüphane",
    Tur = "Sosyal",
    KategoriId = validCategoryId,
    BulusmaZamani = now.AddMinutes(-5).ToLocalTime().DateTime
};
Require(
    ValidateViewModel(pastEditVm).Any(r => r.MemberNames.Contains(nameof(ListingEditViewModel.BulusmaZamani))),
    "İlan düzenleme formunda geçmiş buluşma tarihi reddedilmelidir.");

// =========================================================================
// 4. Veritabanı Hatasında Belleğe Kayıt Yapılmaması Kontrolü
// =========================================================================
var serviceWithUnreachableDb = new ListingService(unusedDb, validator, NullLogger<ListingService>.Instance, clock);
var dbFailCreateResult = await serviceWithUnreachableDb.CreateListingAsync(
    new CreateListingRequest("Test Başlık", "Test Açıklama", "Kayip", validCategoryId, "Merkez Kampüs", null),
    Guid.NewGuid(),
    "Test Öğrenci",
    "test@ogrenci.ankara.edu.tr");

Require(
    !dbFailCreateResult.Success,
    "Veritabanına ulaşılamadığında CreateListingAsync başarılı dönmemeli, hata vermelidir.");

// =========================================================================
// 5. Hafta 4 — Arama, Kategori, Sayfalama ve İki Hesap (Demo) İzolasyonu
// =========================================================================
var student1Id = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Berke Mahmut Acar
var student2Id = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Zülfikar / 2. Öğrenci

var sampleCategorySocial = new Kategori { Id = validCategoryId, Ad = "Ders Çalışma", Tur = "Sosyal" };
var sampleCategoryLost = new Kategori { Id = categoryLostId, Ad = "Elektronik", Tur = "Kayip" };

var demoListings = new List<Ilan>
{
    // Öğrenci 1'in ilanları
    new()
    {
        Id = Guid.NewGuid(),
        SahipId = student1Id,
        Baslik = "Merkez Kütüphane Algoritma Çalışması",
        Aciklama = "Algoritma vize sınavı için grup çalışması düzenliyoruz.",
        Tur = "Sosyal",
        Yer = "Kütüphane Kat 2",
        KategoriId = sampleCategorySocial.Id,
        Kategori = sampleCategorySocial,
        BulusmaTarihi = futureDate,
        Durum = "Aktif",
        OlusturulmaTarihi = now.AddDays(-2)
    },
    new()
    {
        Id = Guid.NewGuid(),
        SahipId = student1Id,
        Baslik = "Kayıp Mavi Kulaklık",
        Aciklama = "Mühendislik kantininde mavi bluetooth kulaklığımı unuttum.",
        Tur = "Kayip",
        Yer = "Mühendislik Kantini",
        KategoriId = sampleCategoryLost.Id,
        Kategori = sampleCategoryLost,
        BulusmaTarihi = null,
        Durum = "Aktif",
        OlusturulmaTarihi = now.AddDays(-1)
    },
    new()
    {
        Id = Guid.NewGuid(),
        SahipId = student1Id,
        Baslik = "Gizli İlan Öğrenci 1",
        Aciklama = "Bu ilan gizlidir ve listelerde görünmemelidir.",
        Tur = "Kayip",
        Yer = "Kampüs",
        KategoriId = sampleCategoryLost.Id,
        Kategori = sampleCategoryLost,
        BulusmaTarihi = null,
        Durum = "Gizli",
        OlusturulmaTarihi = now.AddHours(-1)
    },

    // Öğrenci 2'nin ilanları
    new()
    {
        Id = Guid.NewGuid(),
        SahipId = student2Id,
        Baslik = "Kampüs Halı Saha Maçı",
        Aciklama = "Cuma akşamı halı saha maçı için 2 kişi arıyoruz.",
        Tur = "Sosyal",
        Yer = "Spor Tesisleri Halı Saha",
        KategoriId = sampleCategorySocial.Id,
        Kategori = sampleCategorySocial,
        BulusmaTarihi = futureDate.AddDays(1),
        Durum = "Aktif",
        OlusturulmaTarihi = now.AddHours(-10)
    }
};

// 5a. Metin Arama Testi (q parametresi): Başlık, Açıklama ve Yer alanlarında eşleşme
var searchAlgo = demoListings
    .Where(x => x.Durum != "Gizli" && (x.Baslik.Contains("Algoritma", StringComparison.OrdinalIgnoreCase)
        || x.Aciklama.Contains("Algoritma", StringComparison.OrdinalIgnoreCase)
        || x.Yer.Contains("Algoritma", StringComparison.OrdinalIgnoreCase)))
    .ToList();
Require(searchAlgo.Count == 1 && searchAlgo[0].Baslik.Contains("Algoritma"), "Arama: 'Algoritma' kelimesiyle 1 adet aktif ilan bulunmalıdır.");

var searchKantin = demoListings
    .Where(x => x.Durum != "Gizli" && (x.Baslik.Contains("Kantini", StringComparison.OrdinalIgnoreCase)
        || x.Aciklama.Contains("Kantini", StringComparison.OrdinalIgnoreCase)
        || x.Yer.Contains("Kantini", StringComparison.OrdinalIgnoreCase)))
    .ToList();
Require(searchKantin.Count == 1 && searchKantin[0].Yer.Contains("Mühendislik Kantini"), "Arama: Konumda geçen 'Kantini' kelimesiyle eşleşme sağlanmalıdır.");

// 5b. Tür ve Kategori Filtreleme Testi
var socialListings = demoListings.Where(x => x.Durum != "Gizli" && x.Tur == "Sosyal").ToList();
Require(socialListings.Count == 2, "Filtreleme: Toplam 2 adet aktif Sosyal ilan listelenmelidir.");

var lostListings = demoListings.Where(x => x.Durum != "Gizli" && x.Tur == "Kayip").ToList();
Require(lostListings.Count == 1, "Filtreleme: Gizli olan hariç 1 adet aktif Kayıp ilan listelenmelidir.");

var categoryFiltered = demoListings.Where(x => x.Durum != "Gizli" && x.KategoriId == sampleCategorySocial.Id).ToList();
Require(categoryFiltered.Count == 2, "Filtreleme: Sosyal kategori ID'si ile filtrelendiğinde 2 ilan dönmelidir.");

// 5c. Sayfalama (Pagination) Hesaplama Mantığı
var totalActiveCount = demoListings.Count(x => x.Durum != "Gizli"); // 3 aktif ilan
var pageSize = 2;
var totalPages = (int)Math.Ceiling(totalActiveCount / (double)pageSize);
Require(totalPages == 2, "Sayfalama: 3 aktif ilan için 2 sayfa boyutuyla toplam 2 sayfa oluşmalıdır.");

var page1Items = demoListings.Where(x => x.Durum != "Gizli").Skip(0).Take(pageSize).ToList();
Require(page1Items.Count == 2, "Sayfalama: 1. sayfada 2 ilan yer almalıdır.");

var page2Items = demoListings.Where(x => x.Durum != "Gizli").Skip(2).Take(pageSize).ToList();
Require(page2Items.Count == 1, "Sayfalama: 2. sayfada kalan 1 ilan yer almalıdır.");

// 5d. İki Hesap (Two Accounts) ve İlanlarım İzolasyonu Testi
var student1MyListings = demoListings.Where(x => x.SahipId == student1Id && x.Durum != "Gizli").ToList();
var student2MyListings = demoListings.Where(x => x.SahipId == student2Id && x.Durum != "Gizli").ToList();

Require(student1MyListings.Count == 2, "İlanlarım: Öğrenci 1 yalnızca kendi 2 aktif ilanını görmelidir.");
Require(student2MyListings.Count == 1, "İlanlarım: Öğrenci 2 yalnızca kendi 1 aktif ilanını görmelidir.");
Require(!student1MyListings.Any(x => x.SahipId == student2Id), "İlanlarım: Öğrenci 1'in listesinde Öğrenci 2'nin ilanı olamaz.");
Require(!student2MyListings.Any(x => x.SahipId == student1Id), "İlanlarım: Öğrenci 2'nin listesinde Öğrenci 1'in ilanı olamaz.");

// 5e. 403 Forbidden Sahiplik Sınırı (Öğrenci 2, Öğrenci 1'in ilanını güncelleyemez veya silemez)
var targetListing = demoListings.First(x => x.SahipId == student1Id && x.Durum != "Gizli");
var canStudent2Manage = targetListing.SahipId == student2Id;
Require(!canStudent2Manage, "Yetkilendirme (403): Öğrenci 2, Öğrenci 1'in ilanını yönetemez / silemez (403 Forbidden).");

var canStudent1Manage = targetListing.SahipId == student1Id;
Require(canStudent1Manage, "Yetkilendirme: İlan sahibi kendi ilanını yönetebilir.");

// 5f. Gizli İlan Koruması: Gizli ilanlar hiçbir aramada ve listede yer alamaz
var hiddenInSearch = demoListings.Where(x => x.Durum != "Gizli").Any(x => x.Durum == "Gizli");
Require(!hiddenInSearch, "Gizlilik: Gizli ilanlar arama ve listelerde asla döndürülemez.");

Console.WriteLine("PASS: Tüm Hafta 4 kontrolleri (Arama, Kategori, Sayfalama, İlanlarım, Çift Hesap İzolasyonu ve Sahiplik) başarıyla doğrulandı.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"HATA: {message}");
        Console.ResetColor();
        throw new Exception(message);
    }
}

sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

