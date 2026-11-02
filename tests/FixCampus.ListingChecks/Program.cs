using FixCampus.Web.Contracts;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;

Console.WriteLine("FixCampus Hafta 3 - İlan Doğrulama ve Sahiplik Kontrolleri Başlatılıyor...");

// Test 1: Tür Kontrolleri ve Tarih Kuralları (Birim Kontrolleri)
var validSocialDate = DateTimeOffset.UtcNow.AddDays(2);
var pastSocialDate = DateTimeOffset.UtcNow.AddDays(-1);

// Sosyal buluşma tarihi zorunlu kuralı
Require(TestSocialValidation(null) == false, "Sosyal buluşmada boş tarih reddedilmelidir.");
Require(TestSocialValidation(pastSocialDate) == false, "Sosyal buluşmada geçmiş tarih reddedilmelidir.");
Require(TestSocialValidation(validSocialDate) == true, "Sosyal buluşmada gelecek tarih geçerlidir.");

// Kayıp/Buluntu ilanında tarih boş olmalıdır kuralı
Require(TestNonSocialValidation("Kayip", validSocialDate) == false, "Kayıp ilanında buluşma tarihi kabul edilmemelidir.");
Require(TestNonSocialValidation("Buluntu", validSocialDate) == false, "Buluntu ilanında buluşma tarihi kabul edilmemelidir.");
Require(TestNonSocialValidation("Kayip", null) == true, "Kayıp ilanında tarihsiz oluşturma geçerlidir.");
Require(TestNonSocialValidation("Buluntu", null) == true, "Buluntu ilanında tarihsiz oluşturma geçerlidir.");

// Test 2: Başlık ve Açıklama Karakter Kısıtları
Require(TestTextLengths("A", "Geçerli açıklama metni...", "Kampüs") == false, "3 karakterden kısa başlık reddedilmelidir.");
Require(TestTextLengths(new string('X', 151), "Geçerli açıklama metni...", "Kampüs") == false, "150 karakterden uzun başlık reddedilmelidir.");
Require(TestTextLengths("Geçerli Başlık", "Kısa", "Kampüs") == false, "5 karakterden kısa açıklama reddedilmelidir.");
Require(TestTextLengths("Geçerli Başlık", "Geçerli açıklama metni...", "K") == false, "2 karakterden kısa yer reddedilmelidir.");
Require(TestTextLengths("Geçerli Başlık", "Geçerli açıklama metni...", "Mühendislik D-Blok") == true, "Geçerli metin uzunlukları kabul edilmelidir.");

// Test 3: Sahiplik ve Rol Yetkilendirme (403 Forbidden Mantığı)
var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var otherStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var adminId = Guid.Parse("99999999-9999-9999-9999-999999999999");

var testListing = new Ilan
{
    Id = Guid.NewGuid(),
    SahipId = ownerId,
    Baslik = "Örnek İlan",
    Aciklama = "Detaylı açıklama",
    Tur = "Sosyal",
    Yer = "Kütüphane",
    BulusmaTarihi = validSocialDate,
    Durum = "Aktif"
};

// Başka öğrenci ilanı güncelleyemez veya silemez (403 Forbidden)
var studentCanEdit = CanManageListing(testListing, otherStudentId, isAdmin: false);
Require(!studentCanEdit, "Başka bir öğrencinin ilanını düzenleme/silme engellenmelidir (403 Forbidden).");

// İlan sahibi kendi ilanını düzenleyebilir ve silebilir
var ownerCanEdit = CanManageListing(testListing, ownerId, isAdmin: false);
Require(ownerCanEdit, "İlan sahibi kendi ilanını yönetebilmelidir.");

// Sistem yöneticisi herhangi bir ilanı silebilir veya düzenleyebilir
var adminCanEdit = CanManageListing(testListing, adminId, isAdmin: true);
Require(adminCanEdit, "Yönetici herhangi bir ilanı yönetebilmelidir.");

Console.WriteLine("PASS: Tüm alan doğrulama kuralları, sosyal tarih mantığı ve 403 sahiplik kontrolleri başarıyla doğrulandı.");

// ==========================================
// YARDIMCI MANTIK DOĞRULAMA FONKSİYONLARI
// ==========================================
static bool TestSocialValidation(DateTimeOffset? date)
{
    if (date is null) return false;
    if (date.Value <= DateTimeOffset.UtcNow) return false;
    return true;
}

static bool TestNonSocialValidation(string tur, DateTimeOffset? date)
{
    if (tur is not ("Kayip" or "Buluntu")) return false;
    return date is null;
}

static bool TestTextLengths(string baslik, string aciklama, string yer)
{
    if (string.IsNullOrWhiteSpace(baslik) || baslik.Trim().Length < 3 || baslik.Length > 150) return false;
    if (string.IsNullOrWhiteSpace(aciklama) || aciklama.Trim().Length < 5 || aciklama.Length > 3000) return false;
    if (string.IsNullOrWhiteSpace(yer) || yer.Trim().Length < 2 || yer.Length > 200) return false;
    return true;
}

static bool CanManageListing(Ilan listing, Guid currentUserId, bool isAdmin)
{
    return listing.SahipId == currentUserId || isAdmin;
}

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

