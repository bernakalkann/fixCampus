import 'json_fields.dart';
import 'model_enums.dart';

/// Ortak API veri modeli: Kategori. Kimlikler UUID metni olarak taşınır.
class Kategori {
  const Kategori({
    required this.id,
    required this.ad,
    required this.tur,
  });

  final String id;
  final String ad;
  final IlanTuru tur;

  factory Kategori.fromJson(Map<String, dynamic> json) => Kategori(
        id: JsonFields.string(json, 'id'),
        ad: JsonFields.string(json, 'ad'),
        tur: IlanTuru.fromJson(JsonFields.string(json, 'tur')),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'ad': ad,
        'tur': tur.apiValue,
      };
}
