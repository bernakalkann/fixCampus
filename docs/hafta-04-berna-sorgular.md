# Hafta 4 — Berna Kalkan: ortak ilan sorguları

## Kapsam

Kategori, tür ve başlık/açıklama/yer aramasını `ListingQueries` içinde birleştirdim.
MVC `ListingService` ve mobilin kullandığı `ListingsController` aynı filtreleri ve
aynı sıralamayı kullanır. Gizli ilanlar sorgunun başında elenir. PostgreSQL'de
ILIKE kullanılır; `%`, `_` ve ters eğik çizgi kullanıcı metni olarak aranır.

Sayfalama varsayılanı 1. sayfa ve 12 kayıttır; boyut üst sınırı 50'dir.
Sayfa numarası 1'in altındaysa 1 olur; sıfır/negatif boyut 12 olur.
Sıralama oluşturulma tarihi azalan, eşitlikte kimlik artandır.
Çok büyük sayfa numaraları çarpım taşması nedeniyle ilk sayfaya dönmez;
boş sonuç verir. Kayıtlar sayfalar arasında değişirse sonuçlar da değişebilir;
bu bir anlık görüntü/cursor sayfalaması değildir.

## API sözleşmesi

Mevcut `GET /api/ilanlar` JSON dizisi döndürmeye devam eder. `sayfa` veya
`sayfaBoyutu` parametrelerinden biri bile gönderilirse diğeri varsayılan alınır.
İkisi de gönderilmezse önceki sürüm gibi tüm görünür sonuçları döndürür.

Yeni `GET /api/ilanlar/sayfali` daima sayfalıdır ve aynı oturum/rol politikasına tabidir:

```http
GET /api/ilanlar/sayfali?tur=Kayip&q=kulaklik&sayfa=1&sayfaBoyutu=12
Authorization: Bearer <erişim-belirteci>
```

İsteğe bağlı `kategoriId` Guid değeridir. Yanıt alanları:

```json
{"items": [], "totalCount": 0, "page": 1, "pageSize": 12, "totalPages": 1}
```

Toplam kayıt sayısı filtrelerden sonra hesaplanır. Aralık dışı sayfada `items`
boştur; istenen sayfa numarası korunur. `items` mevcut `ListingResponse`
alanlarını taşır; eski mobil liste sözleşmesi değiştirilmez.

## Doğrulama

`dotnet run --project tests/FixCampus.ListingChecks` başarılıdır. Eklenen
`ListingQueryChecks`, gerçek ortak sorgu kodunu çalıştırarak birleşik filtreleri,
gizli kayıtların elenmesini, eşit tarihlerde sıralamayı, sayfalama sınırlarını ve
taşmayı kontrol eder. Npgsql `ToQueryString` kontrolü ILIKE/kaçış/LIMIT/OFFSET
SQL çevirisini doğrular; bu kontrol canlı Supabase sorgusu çalıştırmaz.

Bu katkı veritabanı migration'ı veya Supabase yapılandırma değişikliği gerektirmez.
İki hesapla canlı uçtan uca demo bu çalışma kapsamında yeniden yapılmamıştır.
