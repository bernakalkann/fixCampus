import '../models/models.dart';

/// Ortak API'nin filtre ve sayfalama parametreleri.
class ListingQuery {
  const ListingQuery({
    this.tur,
    this.kategoriId,
    this.text,
    this.page = 1,
    this.pageSize = 12,
    this.lostFoundOnly = false,
    this.activeSocialOnly = false,
  });

  final IlanTuru? tur;
  final String? kategoriId;
  final String? text;
  final int page;
  final int pageSize;
  final bool lostFoundOnly;
  final bool activeSocialOnly;

  ListingQuery copyWith({int? page}) => ListingQuery(
    tur: tur,
    kategoriId: kategoriId,
    text: text,
    page: page ?? this.page,
    pageSize: pageSize,
    lostFoundOnly: lostFoundOnly,
    activeSocialOnly: activeSocialOnly,
  );

  Map<String, dynamic> toQueryParameters() => {
    if (tur != null) 'tur': tur!.apiValue,
    if (kategoriId != null && kategoriId!.trim().isNotEmpty)
      'kategoriId': kategoriId!.trim(),
    if (text != null && text!.trim().isNotEmpty) 'q': text!.trim(),
    if (lostFoundOnly) 'kayipBuluntu': true,
    if (activeSocialOnly) 'aktifSosyal': true,
    'sayfa': page < 1 ? 1 : page,
    'sayfaBoyutu': pageSize < 1 ? 12 : pageSize.clamp(1, 50),
  };
}

class ListingPage {
  const ListingPage({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.totalPages,
  });

  final List<Ilan> items;
  final int totalCount;
  final int page;
  final int pageSize;
  final int totalPages;
  bool get hasNext => page < totalPages;
  bool get hasPrevious => page > 1;

  factory ListingPage.fromJson(Map<String, dynamic> json) {
    final items = json['items'];
    final total = json['totalCount'];
    final page = json['page'];
    final size = json['pageSize'];
    final pages = json['totalPages'];
    if (items is! List ||
        total is! int ||
        total < 0 ||
        page is! int ||
        page < 1 ||
        size is! int ||
        size < 1 ||
        size > 50 ||
        pages is! int ||
        pages < 1) {
      throw const FormatException('Geçersiz sayfalama yanıtı.');
    }
    if (items.length > size ||
        items.length > total ||
        pages != (total == 0 ? 1 : (total + size - 1) ~/ size) ||
        (page > pages && items.isNotEmpty)) {
      throw const FormatException('Tutarsız sayfalama yanıtı.');
    }
    return ListingPage(
      items: List.unmodifiable(
        items.map((item) {
          if (item is! Map<String, dynamic>) {
            throw const FormatException('İlan nesnesi bekleniyor.');
          }
          return Ilan.fromJson(item);
        }),
      ),
      totalCount: total,
      page: page,
      pageSize: size,
      totalPages: pages,
    );
  }
}
