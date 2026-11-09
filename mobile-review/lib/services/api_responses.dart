/// API'nin gösterim yanıtlarıdır; lib/models altındaki varlıklardan ayrıdır.
class ApiStatus {
  const ApiStatus({
    required this.application,
    required this.status,
    required this.stage,
    required this.checkedAtUtc,
  });

  final String application;
  final String status;
  final String stage;
  final DateTime checkedAtUtc;

  factory ApiStatus.fromJson(Map<String, dynamic> json) => ApiStatus(
    application: _text(json, 'application'),
    status: _text(json, 'status'),
    stage: _text(json, 'stage'),
    checkedAtUtc: _date(json, 'checkedAtUtc'),
  );
}

class SampleListingsResponse {
  const SampleListingsResponse({
    required this.dataSource,
    required this.isPersisted,
    required this.items,
  });

  final String dataSource;
  final bool isPersisted;
  final List<SampleListing> items;

  factory SampleListingsResponse.fromJson(Map<String, dynamic> json) {
    final items = json['items'];
    if (items is! List) throw const FormatException('items liste olmalıdır.');
    return SampleListingsResponse(
      dataSource: _text(json, 'dataSource'),
      isPersisted: _boolean(json, 'isPersisted'),
      items: List.unmodifiable(
        items.map((item) => SampleListing.fromJson(_object(item))),
      ),
    );
  }
}

class SampleListing {
  const SampleListing({
    required this.id,
    required this.type,
    required this.title,
    required this.description,
    required this.location,
    required this.startsAtUtc,
  });

  final String id;
  final String type;
  final String title;
  final String description;
  final String location;
  final DateTime? startsAtUtc;

  factory SampleListing.fromJson(Map<String, dynamic> json) => SampleListing(
    id: _text(json, 'id'),
    type: _text(json, 'type'),
    title: _text(json, 'title'),
    description: _text(json, 'description'),
    location: _text(json, 'location'),
    startsAtUtc: json['startsAtUtc'] == null
        ? null
        : _date(json, 'startsAtUtc'),
  );
}

/// Development ortamındaki tek Supabase demo kaydının yanıtı.
class DatabaseDemoListing {
  const DatabaseDemoListing({
    required this.id,
    required this.baslik,
    required this.aciklama,
    required this.tur,
    required this.yer,
    required this.kategori,
    required this.durum,
    required this.olusturulmaTarihi,
    required this.dataSource,
    required this.isPersisted,
  });

  final String id;
  final String baslik;
  final String aciklama;
  final String tur;
  final String yer;
  final String kategori;
  final String durum;
  final DateTime olusturulmaTarihi;
  final String dataSource;
  final bool isPersisted;

  factory DatabaseDemoListing.fromJson(Map<String, dynamic> json) =>
      DatabaseDemoListing(
        id: _text(json, 'id'),
        baslik: _text(json, 'baslik'),
        aciklama: _text(json, 'aciklama'),
        tur: _text(json, 'tur'),
        yer: _text(json, 'yer'),
        kategori: _text(json, 'kategori'),
        durum: _text(json, 'durum'),
        olusturulmaTarihi: _date(json, 'olusturulmaTarihi'),
        dataSource: _text(json, 'dataSource'),
        isPersisted: _boolean(json, 'isPersisted'),
      );
}

Map<String, dynamic> _object(Object? value) {
  if (value is! Map<String, dynamic>) {
    throw const FormatException('JSON nesnesi bekleniyor.');
  }
  return value;
}

String _text(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is! String) throw FormatException('$key metin olmalıdır.');
  return value;
}

bool _boolean(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is! bool) throw FormatException('$key boolean olmalıdır.');
  return value;
}

DateTime _date(Map<String, dynamic> json, String key) {
  final value = _text(json, key);
  final match = RegExp(
    r'^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$',
  ).firstMatch(value);
  if (match == null) {
    throw FormatException('$key saat dilimli tarih olmalıdır.');
  }
  final parts = [for (var i = 1; i <= 6; i++) int.parse(match.group(i)!)];
  final [year, month, day, hour, minute, second] = parts;
  final offset = match.group(7)!;
  final offsetHours = offset == 'Z' ? 0 : int.parse(offset.substring(1, 3));
  final offsetMinutes = offset == 'Z' ? 0 : int.parse(offset.substring(4, 6));
  if (year < 1 ||
      month < 1 ||
      month > 12 ||
      day < 1 ||
      day > DateTime.utc(year, month + 1, 0).day ||
      hour > 23 ||
      minute > 59 ||
      second > 59 ||
      offsetHours > 14 ||
      offsetMinutes > 59 ||
      (offsetHours == 14 && offsetMinutes != 0)) {
    throw FormatException('$key geçerli bir tarih olmalıdır.');
  }
  final parsed = DateTime.tryParse(value);
  if (parsed == null) {
    throw FormatException('$key geçerli bir tarih olmalıdır.');
  }
  return parsed.toUtc();
}
