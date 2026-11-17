# Hafta 5 — Buluşmalara Yer, Tarih ve Saat Ekleme, Katılım İşlemleri ve Süre Kuralı

**Çalışma Dönemi:** 5. Hafta  
**Hafta Lideri:** Berke Mahmut Acar

---

## 📌 Hafta Özeti ve Kapsam

Bu hafta; **fixCampus** platformundaki sosyal buluşma ("Sosyal Radar") ilanlarının konum, tarih ve saat planlama akışları geliştirilmiş; öğrencilerin buluşmalara **"Geliyorum"** diyerek katılabilmesi, dilediğinde **"Katılımı Geri Al"** ile ayrılabilmesi, aynı kullanıcının aynı buluşmaya mükerrer (tekrar) katılmasının önlenmesi ve buluşma zamanı geçmiş ilanların genel akıştan otomatik gizlenmesi sağlanmıştır. Ek geliştirmeler kapsamında ilan sahiplerinin katılımcı listesini (`AdSoyad`, `Bölüm`, `Sınıf`) görebilmesi, ortak Google Maps konum bağlantısı (`HaritaUrl`) ve yerel Flutter web CORS ayarı tamamlanmıştır.

### Görev Dağılımı

| Sorumlu Üye | Hafta Görevi | Kapsam |
|---|---|---|
| **Berke Mahmut Acar** *(Lider)* | Buluşma Formu & Web Akışları | `Views/Ilanlar/Yeni.cshtml` ve `Duzenle.cshtml` üzerinde hızlı kampüs konumu seçimi + ayrı **Buluşma Tarihi** (`BulusmaTarihiGun`) ve **Buluşma Saati** (`BulusmaSaati`) alanları; `Detay.cshtml` üzerinde **Geliyorum / Katılımı Geri Al** butonları, katılımcı listesi (yalnızca ilan sahibine) ve `Haritada gör` bağlantısı; Ana Sayfa ve Sosyal Radar kısayollarının gerçek `/Ilanlar/Yeni` formuna bağlanması |
| **Berna Kalkan** | Katılım API & Ek Özellikler | `PUT /api/ilanlar/{id}/katilim` ve `DELETE /api/ilanlar/{id}/katilim` uçları, `GET /api/ilanlar/{id}/katilimcilar`, `LocalFlutterCors`, `ListingMapLink` (`HaritaUrl`) ve Pixel 7 Android emülatör canlı testi (`docs/hafta-05-android-canli-test.md`) |
| **Zülfikar Durak** | Süre Kuralı | `ListingQueries.Filter` (`visibleAtUtc`), `ListingService` ve `ListingsController` üzerinde süresi biten (`BulusmaTarihi <= UtcNow`) sosyal buluşmaların akıştan otomatik gizlenmesi; PostgreSQL süre sınır testleri (`ExpiryChecks`, `ExpiryDatabaseChecks`) |

---

## 🌐 API Endpointleri (Hafta 5)

| Yöntem | Endpoint | Yetki | Açıklama |
|---|---|---|---|
| `PUT` | `/api/ilanlar/{id}/katilim` | Öğrenci / Yönetici | Aktif ve süresi geçmemiş sosyal buluşmaya idempotent olarak katılır (`ListingResponse` döner). Süre dolmuşsa/uygun değilse `409 Conflict` döner. |
| `DELETE` | `/api/ilanlar/{id}/katilim` | Öğrenci / Yönetici | Kullanıcının buluşmadaki katılımını geri alır (`ListingResponse` döner). |
| `GET` | `/api/ilanlar/{id}/katilimcilar` | Yalnızca İlan Sahibi | Sosyal buluşmanın katılımcılarını (`AdSoyad`, `Bolum`, `Sinif`) döner; sahip dışında `403 Forbidden` döner. |
| `GET` | `/api/ilanlar` & `/api/ilanlar/sayfali` | Öğrenci / Yönetici | Gizli olmayan ve buluşma süresi dolmamış (`BulusmaTarihi > UtcNow`) ilanları filtreli/sayfalı döndürür. |

---

## 🖥️ Web Arayüz ve Doğrulama Kuralları

1. **Buluşma Formu (`/Ilanlar/Yeni?tur=Sosyal` ve `/Ilanlar/Duzenle/{id}`):**
   - Kampüs içi konum (`Yer`) alanı ve tek tıkla doldurulan hızlı kampüs konumu butonları (*Kütüphane 2. Kat*, *Mühendislik Kantini*, *Spor Tesisleri*, *Öğrenci Kafeteryası*, *Merkezi Derslik*).
   - Ayrı **Buluşma Tarihi** (`type="date"`) ve **Buluşma Saati** (`type="time"`) girişleri; sunucu tarafında `GetEffectiveBulusmaZamani()` ile birleştirilerek UTC olarak kaydedilir ve gelecekte olma şartı doğrulanır.
2. **Geliyorum & Katılımı Geri Al (`/Ilanlar/Detay/{id}`):**
   - Kullanıcı buluşmaya henüz katılmadıysa **Geliyorum** (`POST /Ilanlar/Katil/{id}`) butonu gösterilir.
   - Kullanıcı katıldığında yeşil **Katılıyorsunuz** rozeti ve **Katılımı Geri Al** (`POST /Ilanlar/Ayril/{id}`) butonu gösterilir.
   - Aynı kullanıcı tekrar katılmaya çalışırsa hem servis katmanı hem de veritabanı benzersiz indeksi (`IX_Katilimlar_IlanId_ProfilId`) mükerrer kaydı engeller.
   - İlan sahibi olan kullanıcı kendi buluşmasının detayında katılımcıların ad-soyad, bölüm ve sınıf bilgilerini görebilir.
3. **Süresi Biten İlanları Gizleme & Harita Bağlantısı:**
   - `BulusmaTarihi <= UtcNow` olan sosyal buluşmalar `/Ilanlar` ve API listelerinden otomatik gizlenir; bu ilanlara yeni katılım yapılamaz.
   - İlan sahibi kendi `/Ilanlar/Benim` sayfasında geçmiş buluşmasını **Süresi Doldu (Akışta Gizli)** rozetiyle görebilir.
   - İlan kartlarında ve detayında `Yer` bilgisinin yanında Google Maps **"Haritada gör"** bağlantısı yer alır.

---

## 🧪 Doğrulama Testleri

```powershell
dotnet run --project tests/FixCampus.ListingChecks
dotnet run --project tests/FixCampus.AuthChecks
dotnet run --project tests/FixCampus.AuthorizationChecks
dotnet run --project tests/FixCampus.DatabaseChecks -- --model-only
```
