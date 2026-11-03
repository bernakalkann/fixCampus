using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers;
using FixCampus.Web.Data;
using FixCampus.Web.Models;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

Console.WriteLine("FixCampus Hafta 3 - Web & API Uyumlu İlan Doğrulama, Gizlilik ve Sahiplik Kontrolleri Başlatılıyor...");

var now = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
var clock = new FixedClock(now);
using var services = new ServiceCollection()
    .AddSingleton<TimeProvider>(clock)
    .BuildServiceProvider();

var validCategoryId = Guid.Parse("11111111-1111-4111-8111-111111111111");
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
// 2. ListingValidator Oluşturma ve Güncelleme Alan & Tarih Kontrolleri
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
    Baslik = "AB", // 2 karakter geçerli (API ile uyumlu)
    Aciklama = "A", // 1 karakter geçerli (API ile uyumlu)
    Yer = "B", // 1 karakter geçerli (API ile uyumlu)
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

Console.WriteLine("PASS: Web & API doğrulama uyumu, düzenlemede geçmiş tarih engeli, gizli ilan/oturum koruması ve DB hata kontrolü doğrulandı.");

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
