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

## Mobil ekran entegrasyonu

Üç mobil ilan ekranı sayfalı API'yi kullanır. Sayfalı uçtaki isteğe bağlı
`kayipBuluntu` ve `aktifSosyal` bayrakları mevcut akışları korur. Arama
kelimeleri başlık/açıklama/yer/kategori adında birlikte aranır; Türkçe İ/i ve
I/ı normalizasyonu uygulanır.

`ListingSearchDatabaseChecks` gerçek PostgreSQL üzerinde yeni ucu, joker
kaçışlarını, Türkçe/çok kelimeli aramayı, sayfalama toplamlarını ve MVC/mobil
API'nin ortak kaydı okumasını denetler. GitHub Actions, yalnız geçici
`fixcampus_test` veritabanında migration ve CRUD testlerini çalıştırır.

## İnceleme düzeltmeleri

`PagedList` metodunda `CancellationToken` son parametredir. Sorgu parametrelerinin
adları ve HTTP sözleşmesi değişmez.

PostgreSQL aramasında dört alan boşlukla ayrılan tek arama metnine birleştirilir.
Türkçe İ/I dönüşümü iki `REPLACE` ile uygulanır; büyük/küçük harf karşılaştırmasını
`ILIKE ALL (@patterns)` yapar. Tekrarlanan arama kelimeleri kaldırılır. Böylece
her kelime için dört ayrı normalizasyon ve `LOWER` ifadesi üretilmez. Literal
`%`, `_`, ters eğik çizgi, Türkçe ve farklı alanlara yayılan kelimeler için
mevcut PostgreSQL regresyon testleri korunur. Şema değişikliği gerekmez.

Bu düzenleme indeksli tam metin araması değildir; başında `%` olan alt metin
araması büyük veri kümelerinde tarama gerektirebilir. Sonraki ölçek adımı gerçek
veride `EXPLAIN (ANALYZE, BUFFERS)` ölçümüyle seçilmelidir: mevcut alt metin
davranışı için normalleştirilmiş arama metni ve `pg_trgm` GIN indeksi; kelime
kökü/sıralama isteyen ürün davranışı için `to_tsvector` ve GIN. `unaccent` ise
ş/s gibi ayrımları da kaldırır ve mevcut eşleşme davranışını değiştirir. Bu
geçişler ayrı migration ve eşleşme testleriyle yapılmalıdır.

Npgsql çevirisi: https://www.npgsql.org/efcore/mapping/array.html
