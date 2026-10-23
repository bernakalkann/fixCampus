# fixCampus &bull; Kampüs İlan ve Sosyal Radar Platformu

**BLM4581 Ağ Tabanlı Teknolojiler ve Uygulamaları &bull; Dönem Projesi (2026–2027 Güz Yarıyılı)**

---

## 📌 Proje Hakkında
fixCampus; üniversite öğrencilerinin kampüs içinde sosyal buluşmalar organize edebileceği ("Sosyal Radar") ve kayıp-buluntu eşyalarını tek noktadan fotoğraflı ve çözüldü takipli olarak paylaşabileceği ("Kayıp & Buluntu") modern bir kampüs portalıdır.

- **GitHub Deposu:** [https://github.com/bernakalkann/fixCampus.git](https://github.com/bernakalkann/fixCampus.git)
- **Drive Klasörü:** [fixCampus Google Drive Klasörü](https://drive.google.com/drive/folders/1tOCk6avpzkecFvXFqZBoqK-UEW7HGLNB?usp=sharing)

---

## 👥 Proje Ekibi & Liderlik Dağılımı

| Ad Soyad | Öğrenci No | GitHub | Liderlik Haftaları | Temel Görev Kapsamı |
| :--- | :--- | :--- | :--- | :--- |
| **Berna Kalkan** | 22290948 | [@bernakalkann](https://github.com/bernakalkann) | 7, 8 ve 9. Haftalar | Veri modeli, Supabase Auth/Profil, CRUD API, Katılım/Yönetim API |
| **Berke Mahmut Acar** | 22290455 | [@berke1227](https://github.com/berke1227) | 4, 5 ve 6. Haftalar | **Web İskeleti**, Giriş Formları, İlan Sayfaları, Arama & Buluşma Ekranı, Mobil Uyum |
| **Zülfikar Durak** | 22290590 | [@zulficon](https://github.com/zulficon) | 1, 2 ve 3. Haftalar | **API İskeleti**, Rol Kontrolü, Doğrulama Kuralları, İlanlarım, Fotoğraf API |

---

## 🛠️ Kullanılan Teknolojiler
- **Çatı:** ASP.NET Core (MVC + Web API)
- **Arayüz & Tasarım:** Bootstrap 5, Plus Jakarta Sans, Bootstrap Icons, Sade & Kurumsal CSS Tasarım Sistemi
- **Veritabanı (Ortak):** PostgreSQL (Supabase) + Entity Framework Core (Npgsql)
- **API Dokümantasyonu:** Swagger / OpenAPI
- **Harici Entegrasyonlar:** Supabase Auth (Kimlik doğrulama) & Supabase Storage (Fotoğraf saklama)

---

## 🚀 Hafta 1 Kapsamında Tamamlanan Çalışmalar

### 1. API İskeleti & Servisler (Zülfikar Durak)
- [x] ASP.NET Core API iskeleti ve `src/FixCampus.Web` proje/solution (`fixCampus.slnx`) mimarisi kuruldu.
- [x] OpenAPI ve etkileşimli Swagger arayüzü yapılandırıldı.
- [x] Sistem sağlık kontrolü (`GET /api/durum`) ve örnek ilan akışı (`GET /api/ornek/ilanlar`) endpointleri hazırlandı.

### 2. Web İskeleti & Ekran Tasarımları (Berke Mahmut Acar)
- [x] **Sade Tasarım Sistemi & CSS (`wwwroot/css/site.css`):**
  - Sade, kurumsal ve nötr renk paleti, modern tipografi ve esnek bileşenler.
- [x] **Genel Layout (`Views/Shared/_Layout.cshtml`):**
  - Sade marka başlığı, menü navigasyonu (`Ana Sayfa`, `Sosyal Radar`, `Kayıp & Buluntu`, `Tüm İlanlar`).
  - Menü barının sağına, aksiyon butonlarının soluna entegre edilen ilan filtreleme ve arama çubuğu.
  - Sadeleştirilmiş alt bilgi (Footer) alanı.
- [x] **Ana Sayfa Yapısı (`Views/Home/Index.cshtml`):**
  - Proje tanıtımı, hedefleri ve planlanan modüllerin görev tanımları.
- [x] **Ekran Akışları İskeletleri:**
  - `Views/Home/Radar.cshtml` (Sosyal Radar ekranı ve yeni buluşma formu alanı).
  - `Views/Home/LostFound.cshtml` (Kayıp & Buluntu eşya ekranı ve ilan formu alanı).
  - `Views/Home/Ads.cshtml` (Menü filtresiyle senkronize çalışan tam genişlikli ilan akışı ve sayfalama).

---

### 3. Veri Modeli (Berna Kalkan)
- Altı model sınıfı: Profil, Kategori, Ilan, Katilim, IlanFotografi ve Sikayet.
- EF Core DbContext ile tablo ilişkileri, alan uzunlukları ve veri kısıtları.
- Tekrar katılımı ve ilan başına birden fazla fotoğrafı önleyen benzersiz indeksler.

Ayrıntılar: [Hafta 1 veri modeli](docs/hafta-01-veri-modeli.md).

---

## 💻 Projeyi Çalıştırma

Depo kökünde PowerShell ile:

```powershell
dotnet restore fixCampus.slnx
dotnet build fixCampus.slnx --no-restore
dotnet run --project src/FixCampus.Web --launch-profile http
```

Uygulama yerel ortamda açıldığında:
- **Web Portalı & Sayfalar:** `http://localhost:5168/`
- **Swagger UI:** `http://localhost:5168/swagger`
- **OpenAPI Belgesi:** `http://localhost:5168/openapi/v1.json`

### API Endpointleri

| Adres | Açıklama |
|---|---|
| [GET /api/durum](http://localhost:5168/api/durum) | Uygulamanın çalıştığını bildirir. |
| [GET /api/ornek/ilanlar](http://localhost:5168/api/ornek/ilanlar) | Sosyal buluşma, kayıp ve buluntu için örnek kayıtları döndürür. |
| [/swagger](http://localhost:5168/swagger) | Etkileşimli Swagger API dokümantasyonu. |

PowerShell ile hızlı kontrol:

```powershell
Invoke-RestMethod http://localhost:5168/api/durum
Invoke-RestMethod http://localhost:5168/api/ornek/ilanlar
```
