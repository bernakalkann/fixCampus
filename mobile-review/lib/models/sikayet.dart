import 'json_fields.dart';
import 'model_enums.dart';

/// Ortak API veri modeli: Sikayet. Kimlikler UUID metni olarak taşınır.
class Sikayet {
  const Sikayet({
    required this.id,
    required this.ilanId,
    required this.bildirenId,
    required this.neden,
    required this.durum,
    required this.olusturulmaTarihi,
  });

  final String id;
  final String ilanId;
  final String bildirenId;
  final String neden;
  final SikayetDurumu durum;
  final DateTime olusturulmaTarihi;

  factory Sikayet.fromJson(Map<String, dynamic> json) => Sikayet(
        id: JsonFields.string(json, 'id'),
        ilanId: JsonFields.string(json, 'ilanId'),
        bildirenId: JsonFields.string(json, 'bildirenId'),
        neden: JsonFields.string(json, 'neden'),
        durum: SikayetDurumu.fromJson(JsonFields.string(json, 'durum')),
        olusturulmaTarihi: JsonFields.dateTime(json, 'olusturulmaTarihi'),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'ilanId': ilanId,
        'bildirenId': bildirenId,
        'neden': neden,
        'durum': durum.apiValue,
        'olusturulmaTarihi': olusturulmaTarihi.toUtc().toIso8601String(),
      };
}
