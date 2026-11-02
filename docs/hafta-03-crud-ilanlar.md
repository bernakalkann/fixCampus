# Hafta 3 — İlan CRUD API, Swagger, Web Akışları ve Doğrulama

**Lider:** Zülfikar Durak  
**Çalışma Dönemi:** 3. Hafta

---

## 📌 Hafta Özeti ve Kapsam

Bu hafta; fixCampus platformunun temel işlevi olan kampüs ilanlarının (Sosyal Radar buluşmaları ve Kayıp & Buluntu eşya duyuruları) uçtan uca yönetimi gerçekleştirilmiştir. API katmanında tam CRUD desteği ve Swagger dokümantasyonu, web arayüzünde ilan yönetimi ekranları ve iş kurallarını denetleyen katı doğrulama/sahiplik kontrolleri devreye alınmıştır.

### Görev Dağılımı

| Sorumlu Üye | Hafta Görevi | Kapsam |
|---|---|---|
| **Berna Kalkan** | CRUD API & Sözleşmeler | İlan REST API endpointleri (`GET`, `POST`, `PUT`, `DELETE`), DTO/Contracts modelleri, kategori listeleme ve Swagger OpenAPI şemaları |
| **Berke Mahmut Acar** | Web İlan Sayfaları | `IlanlarController` MVC akışları, İlan Ekleme, İlan Detayı, İlan Düzenleme, Silme onay penceresi ve "İlanlarım" listesi arayüzleri |
| **Zülfikar Durak** | Doğrulama & Sahiplik (Lider) | Sosyal/Kayıp/Buluntu tarih kuralları, metin uzunluk sınırları, kategori/tür tutarlılığı ve ilanın sadece sahibi veya yönetici tarafından düzenlenebilmesini sağlayan 403 Forbidden kontrolleri |

---

## 🌐 API Endpointleri

Tüm API endpointleri `/swagger` arayüzünde OpenAPI 3.0 ile belgelenmiştir. Yazma/silme/güncelleme işlemleri Supabase Bearer token veya aktif çerez oturumu gerektirir.

| Yöntem | Endpoint | Yetki | Açıklama / Gövde |
|---|---|---|---|
| `GET` | `/api/ilanlar` | Herkese Açık | İlanları listeler. Parametreler: `tur` (Sosyal, Kayip, Buluntu), `kategoriId`, `q` (arama), `sayfa`, `sayfaBoyutu` |
| `GET` | `/api/ilanlar/{id}` | Herkese Açık | Belirtilen ilanın tüm detaylarını (sahip, kategori bilgisi vb.) döner. |
| `POST` | `/api/ilanlar` | Oturum Gerekli | Yeni ilan oluşturur. `SahipId` oturum açan kullanıcının UUID'si olarak otomatik atanır. |
| `PUT` | `/api/ilanlar/{id}` | Sahip / Yönetici | İlanı günceller. Başka bir kullanıcının ilanına müdahalede `403 Forbidden` döner. |
| `DELETE` | `/api/ilanlar/{id}` | Sahip / Yönetici | İlanı siler. İlana bağlı katılım ve fotoğraflar ilişkisel olarak temizlenir. Yetkisiz istekte `403 Forbidden` döner. |
| `GET` | `/api/ilanlar/benim` | Oturum Gerekli | Yalnızca giriş yapmış kullanıcının oluşturduğu ilanları döner. |
| `GET` | `/api/ilanlar/kategoriler` | Herkese Açık | İlan türüne göre kategorileri döner (`tur` filtresi desteklenir). |

---

## 🛡️ Doğrulama ve Sahiplik Kuralları (Zülfikar Durak)

1. **Tür Kontrolü:** İlan türü yalnızca `Sosyal`, `Kayip` veya `Buluntu` değerlerini alabilir.
2. **Sosyal Buluşma Tarihi:**
   - Tür `Sosyal` ise `BulusmaTarihi` **zorunludur** ve **gelecek bir zaman** olmalıdır (UTC).
   - Tür `Kayip` veya `Buluntu` ise `BulusmaTarihi` **boş (null)** olmak zorundadır.
3. **Kategori Uyumu:**
   - Seçilen kategori veritabanında mevcut olmalıdır.
   - Kategorinin türü (`Kategori.Tur`) ilanın türü (`Ilan.Tur`) ile birebir uyuşmalıdır.
4. **Metin Doğrulamaları:**
   - `Baslik`: En az 3, en fazla 150 karakter olmalı, boşluklardan arındırılmalıdır.
   - `Aciklama`: En az 5, en fazla 3000 karakter olmalıdır.
   - `Yer`: En az 2, en fazla 200 karakter olmalıdır.
5. **Sahiplik Kontrolü (403 Forbidden):**
   - İlan güncelleme ve silme işlemlerinde; isteği atan kullanıcının kimliği (`ClaimTypes.NameIdentifier`) ilanın `SahipId` değeri ile eşleşmelidir.
   - Sistem yöneticisi (`Yonetici` rolü) denetim amacıyla tüm ilanları düzenleyebilir veya silebilir.
   - Eşleşmeyen durumlarda sunucu kesinlikle `403 Forbidden` döner.

