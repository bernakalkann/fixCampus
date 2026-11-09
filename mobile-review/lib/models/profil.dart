import 'json_fields.dart';
import 'model_enums.dart';

/// Ortak API veri modeli: Profil. Kimlikler UUID metni olarak taşınır.
class Profil {
  const Profil({
    required this.id,
    required this.adSoyad,
    required this.rol,
    required this.olusturulmaTarihi,
    this.bolum,
    this.sinif,
  });

  final String id;
  final String adSoyad;
  final KullaniciRolu rol;
  final DateTime olusturulmaTarihi;
  final String? bolum;
  final String? sinif;

  factory Profil.fromJson(Map<String, dynamic> json) => Profil(
        id: JsonFields.string(json, 'id'),
        adSoyad: JsonFields.string(json, 'adSoyad'),
        rol: KullaniciRolu.fromJson(JsonFields.string(json, 'rol')),
        olusturulmaTarihi: JsonFields.dateTime(json, 'olusturulmaTarihi'),
        bolum: json['bolum'] as String?,
        sinif: json['sinif'] as String?,
      );

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{
      'id': id,
      'adSoyad': adSoyad,
      'rol': rol.apiValue,
      'olusturulmaTarihi': olusturulmaTarihi.toUtc().toIso8601String(),
    };
    if (bolum != null) map['bolum'] = bolum;
    if (sinif != null) map['sinif'] = sinif;
    return map;
  }

  Profil copyWith({
    String? id,
    String? adSoyad,
    KullaniciRolu? rol,
    DateTime? olusturulmaTarihi,
    String? bolum,
    String? sinif,
  }) =>
      Profil(
        id: id ?? this.id,
        adSoyad: adSoyad ?? this.adSoyad,
        rol: rol ?? this.rol,
        olusturulmaTarihi: olusturulmaTarihi ?? this.olusturulmaTarihi,
        bolum: bolum ?? this.bolum,
        sinif: sinif ?? this.sinif,
      );
}
