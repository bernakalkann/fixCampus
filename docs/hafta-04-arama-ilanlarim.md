# Hafta 4 — Kategori, Metin Arama, Sayfalama ve İlanlarım

**Çalışma Dönemi:** 4. Hafta

---

## 📌 Hafta Özeti ve Kapsam

Bu hafta; fixCampus platformunun ilan keşif ve kişisel ilan yönetimi süreçleri geliştirilmiştir. Kullanıcıların kampüs içindeki sosyal buluşmaları ve kayıp/buluntu duyurularını kategori, ilan türü ve serbest metin arama yoluyla saniyeler içinde bulabilmesi sağlanmış; sayfalama yapısı optimize edilmiştir. Ayrıca öğrencilerin kendi oluşturdukları ilanları tek ekrandan istatistikleri ve durumlarıyla takip edebileceği, filtreleyip yönetebileceği **"İlanlarım"** paneli tamamlanmıştır. Sistem iki farklı öğrenci hesabı üzerinden uçtan uca test edilmiş ve ilk gösterim (demo) sürümü hazırlanmıştır.

### Görev Dağılımı

| Sorumlu Üye | Hafta Görevi | Kapsam |
|---|---|---|
| **Berke Mahmut Acar** | Arama Ekranı | `Views/Ilanlar/Index.cshtml` modern arama arayüzü, hızlı tür butonları (pills), kategori filtreleme, aktif filtre rozetleri, sayfalama ve responsive kart tasarımı |
| **Berna Kalkan** | Sorgular & API | EF Core arama (`q`), kategori (`kategoriId`), tür (`tur`) ve sayfalama (`sayfa`, `sayfaBoyutu`) filtreleme sorguları; `GET /api/ilanlar/benim` ucu |
| **Zülfikar Durak** | İlanlarım Ekranı | `Views/Ilanlar/Benim.cshtml` ve `IlanlarController.Benim` akışı, toplam ve türe göre istatistik sayaçları, tür filtre sekmeleri, onay modalı ile silme kontrolü |

---

## 🌐 API Endpointleri & Parametreler

Tüm endpointler Swagger (`/swagger`) arayüzünde belgelenmiştir ve Supabase Bearer token veya aktif web çerez oturumu gerektirir.

| Yöntem | Endpoint | Yetki | Parametreler & Açıklama |
|---|---|---|---|
| `GET` | `/api/ilanlar` | Oturum Gerekli | `tur` (Sosyal, Kayip, Buluntu), `kategoriId` (Guid), `q` (metin arama), `sayfa` (int), `sayfaBoyutu` (int). Gizli olmayan ilanları filtreli/sayfalı listeler. |
| `GET` | `/api/ilanlar/benim` | Oturum Gerekli | `tur` (opsiyonel filtre). Oturum açan kullanıcının kendi aktif ilanlarını listeler. |
| `GET` | `/api/kategoriler` | Oturum Gerekli | `tur` (opsiyonel filtre). İlan türüne göre kategorileri listeler. |
| `GET` | `/api/ilanlar/{id}` | Oturum Gerekli | İlan detayını döner (Gizli ilanlar 404 döner). |
| `POST` | `/api/ilanlar` | Oturum Gerekli | Yeni ilan oluşturur (`SahipId` oturumdan atanır). |
| `PUT` | `/api/ilanlar/{id}` | Yalnızca Sahibi | İlanı günceller (yetkisiz istekte 403 Forbidden). |
| `DELETE` | `/api/ilanlar/{id}` | Yalnızca Sahibi | İlanı siler (yetkisiz istekte 403 Forbidden). |

---

## 🖥️ Web Arayüz Akışları (Berke Mahmut Acar & Zülfikar Durak)

1. **Gelişmiş Arama & Filtreleme (`/Ilanlar`):**
   - **Hızlı Tür Seçimi:** "Tüm İlanlar", "Sosyal Radar", "Kayıp Eşyalar" ve "Buluntu Eşyalar" hap butonları ile anında filtreleme.
   - **Metin Arama (`q`):** Başlık, açıklama ve kampüs konumu alanlarında eşzamanlı arama.
   - **Kategori Uyumu:** Seçilen ilan türüne ait kategorilerin filtrelenmesi.
   - **Aktif Filtre Rozetleri:** Uygulanan filtrelerin rozet olarak listelenmesi ve tek tıkla filtreleri sıfırlama olanağı.
   - **Sayfalama (Pagination):** Arama parametrelerini koruyarak sayfalar arasında gezinme (`« Önceki`, `1, 2, 3...`, `Sonraki »`).
2. **Kişisel Yönetim Paneli — "İlanlarım" (`/Ilanlar/Benim`):**
   - **Özet İstatistik Kartları:** Toplam İlanım, Sosyal Radar Buluşmaları ve Kayıp & Buluntu Eşya sayaçları.
   - **Tür Sekmeleri:** İlanları türlerine göre anında filtreleyip inceleme.
   - **Hızlı Aksiyonlar:** İlan detayını görüntüleme, düzenleme formuna erişim ve modal onaylı güvenli silme.

---

## 👥 İki Hesapla Test ve İlk Gösterim Senaryosu (Demo)

Sistem iki farklı öğrenci rolündeki kullanıcı hesabı ile uçtan uca sınanmıştır:
- **Öğrenci 1 (Berke Mahmut Acar):** `22290455@ogrenci.ankara.edu.tr`
- **Öğrenci 2 (Zülfikar / Diğer Öğrenci):** `22290590@ogrenci.ankara.edu.tr`

### Adım Adım Doğrulama Akışı:
1. **İlan Oluşturma:** Öğrenci 1 giriş yaparak bir Sosyal Radar buluşması ("Kütüphane Algoritma Çalışması") oluşturur.
2. **Arama ve Keşif:** Öğrenci 2 sisteme giriş yapar; arama kutusuna `"Algoritma"` yazar veya `"Sosyal"` filtresini seçer. Öğrenci 1'in oluşturduğu ilan sayfalanmış sonuçlarda listelenir.
3. **Detay ve Yetki Sınırı (403 Forbidden):** Öğrenci 2 ilanın detay sayfasını (`/Ilanlar/Detay/{id}`) görüntüler ancak ilanı düzenleyemez veya silemez; doğrudan API veya URL üzerinden müdahale etmeye kalktığında sistem `403 Forbidden` döner.
4. **İlanlarım İzolasyonu:** Öğrenci 1 kendi "İlanlarım" (`/Ilanlar/Benim`) ekranını açtığında sadece kendi ilanlarını ve istatistiklerini görür; Öğrenci 2'nin ilanları listelenmez.
5. **Silme & Güncelleme:** Öğrenci 1 kendi ilanını düzenler ve başarıyla günceller veya silme modalı ile kaldırır.
6. **Gizlilik Koruması:** `Durum == "Gizli"` olan ilanlar arama sonuçlarında çıkmaz ve doğrudan linkle dahi açılamaz (`404 NotFound`).

---

## 🧪 Hafta 4 Testlerini Çalıştırma

```powershell
dotnet run --project tests/FixCampus.ListingChecks
```

