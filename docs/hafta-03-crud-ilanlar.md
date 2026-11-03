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
| **Zülfikar Durak** | Doğrulama & Sahiplik (Lider) | Sosyal/Kayıp/Buluntu tarih kuralları, metin uzunluk sınırları, kategori/tür tutarlılığı ve ilanın sadece sahibi tarafından düzenlenebilmesini sağlayan 403 Forbidden kontrolleri |

---

## 🌐 API Endpointleri

Tüm API endpointleri `/swagger` arayüzünde OpenAPI 3.0 ile belgelenmiştir ve geçerli oturum gerektirir.

| Yöntem | Endpoint | Yetki | Açıklama / Gövde |
|---|---|---|---|
| `GET` | `/api/ilanlar` | Oturum Gerekli | İlanları listeler (`Gizli` ilanlar gösterilmez). Parametreler: `tur` (`Sosyal`, `Kayip`, `Buluntu`), `kategoriId`, `q` (arama), `sayfa`, `sayfaBoyutu` |
| `GET` | `/api/ilanlar/{id}` | Oturum Gerekli | Belirtilen ilanın tüm detaylarını döner (`Gizli` ise `404 NotFound`). |
| `POST` | `/api/ilanlar` | Oturum Gerekli | Yeni ilan oluşturur. `SahipId` oturum açan kullanıcının UUID'si olarak otomatik atanır. |
| `PUT` | `/api/ilanlar/{id}` | Yalnızca İlan Sahibi | İlanı günceller. Başka bir kullanıcının ilanına müdahalede `403 Forbidden` döner. |
| `DELETE` | `/api/ilanlar/{id}` | Yalnızca İlan Sahibi | İlanı siler. Yetkisiz istekte `403 Forbidden` döner. |
| `GET` | `/api/kategoriler` | Oturum Gerekli | İlan türüne göre kategorileri döner (`tur` filtresi desteklenir). |

---

## 🛡️ Doğrulama ve Sahiplik Kuralları (Zülfikar Durak)

1. **Tür Kontrolü:** İlan türü yalnızca `Sosyal`, `Kayip` veya `Buluntu` değerlerini alabilir.
2. **Sosyal Buluşma Tarihi (Oluşturma ve Güncelleme):**
   - Tür `Sosyal` ise `BulusmaTarihi` **zorunludur** ve **gelecek bir zaman** olmalıdır (UTC).
   - Tür `Kayip` veya `Buluntu` ise `BulusmaTarihi` **boş (null)** olmak zorundadır.
3. **Kategori Uyumu:**
   - Seçilen kategori veritabanında mevcut olmalıdır.
   - Kategorinin türü (`Kategori.Tur`) ilanın türü (`Ilan.Tur`) ile birebir uyuşmalıdır.
4. **Metin Doğrulamaları:**
   - `Baslik`: En az 2, en fazla 150 karakter olmalı, boşluklardan arındırılmalıdır.
   - `Aciklama`: Boş olamaz, en fazla 3000 karakter olmalıdır.
   - `Yer`: Boş olamaz, en fazla 200 karakter olmalıdır.
5. **Sahiplik ve Gizlilik Kontrolü:**
   - `Durum == "Gizli"` olan ilanlar hiçbir listede ve detay sayfasında gösterilmez (`404 NotFound`), güncellenemez ve silinemez.
   - İlan güncelleme ve silme işlemlerinde; isteği atan kullanıcının kimliği (`ClaimTypes.NameIdentifier`) ilanın `SahipId` değeri ile eşleşmelidir; eşleşmeyen durumlarda sunucu `403 Forbidden` döner.
