import '../models/models.dart';

/// Sayfalanmış ilan listesi sonucu.
class ListingPaginationResult {
  const ListingPaginationResult({
    required this.items,
    required this.currentPage,
    required this.totalPages,
    required this.totalItems,
    required this.pageSize,
  });

  final List<Ilan> items;
  final int currentPage;
  final int totalPages;
  final int totalItems;
  final int pageSize;

  bool get hasPrevious => currentPage > 1;
  bool get hasNext => currentPage < totalPages;

  int get startItem => totalItems == 0 ? 0 : (currentPage - 1) * pageSize + 1;
  int get endItem =>
      totalItems == 0 ? 0 : (startItem + items.length - 1).clamp(0, totalItems);
}

/// İlanlar için ortak kategori filtreleme, Türkçe duyarlı metin arama ve sayfalama yardımcısı.
class ListingSearchFilter {
  const ListingSearchFilter._();

  static const int defaultPageSize = 5;

  /// Türkçe karakterleri duyarsızlaştırarak metni küçük harfe çevirir.
  static String normalize(String input) {
    return input
        .trim()
        .replaceAll('İ', 'i')
        .replaceAll('I', 'ı')
        .replaceAll('Ş', 'ş')
        .replaceAll('Ğ', 'ğ')
        .replaceAll('Ü', 'ü')
        .replaceAll('Ö', 'ö')
        .replaceAll('Ç', 'ç')
        .toLowerCase();
  }

  /// Verilen ilan listesini tür, kategori ve arama metnine göre filtreler ve tarihe göre sıralar.
  static List<Ilan> filter(
    List<Ilan> source, {
    String query = '',
    IlanTuru? tur,
    String? kategoriId,
    List<Kategori> kategoriler = const [],
    bool onlyActive = false,
  }) {
    final normalizedQuery = normalize(query);
    final queryTokens = normalizedQuery.isEmpty
        ? const <String>[]
        : normalizedQuery.split(RegExp(r'\s+')).where((t) => t.isNotEmpty).toList();

    final categoryNames = <String, String>{
      for (final k in kategoriler) k.id: normalize(k.ad),
    };

    final filtered = source.where((ilan) {
      if (onlyActive && ilan.durum != IlanDurumu.aktif) {
        return false;
      }
      if (tur != null && ilan.tur != tur) {
        return false;
      }
      if (kategoriId != null && ilan.kategoriId != kategoriId) {
        return false;
      }
      if (queryTokens.isNotEmpty) {
        final searchable =
            '${normalize(ilan.baslik)} ${normalize(ilan.aciklama)} ${normalize(ilan.yer)} ${categoryNames[ilan.kategoriId] ?? ''}';
        for (final token in queryTokens) {
          if (!searchable.contains(token)) {
            return false;
          }
        }
      }
      return true;
    }).toList();

    filtered.sort(
      (a, b) => b.olusturulmaTarihi.compareTo(a.olusturulmaTarihi),
    );
    return filtered;
  }

  /// Filtrelenmiş ilan listesini belirtilen sayfa ve sayfa boyutuna göre böler.
  static ListingPaginationResult paginate(
    List<Ilan> items, {
    int page = 1,
    int pageSize = defaultPageSize,
  }) {
    final safePageSize = pageSize < 1 ? defaultPageSize : pageSize;
    final totalItems = items.length;
    final totalPages =
        totalItems == 0 ? 1 : ((totalItems + safePageSize - 1) ~/ safePageSize);
    final safePage = page.clamp(1, totalPages);

    if (totalItems == 0) {
      return ListingPaginationResult(
        items: const [],
        currentPage: 1,
        totalPages: 1,
        totalItems: 0,
        pageSize: safePageSize,
      );
    }

    final start = (safePage - 1) * safePageSize;
    final end = (start + safePageSize).clamp(0, totalItems);
    return ListingPaginationResult(
      items: List.unmodifiable(items.sublist(start, end)),
      currentPage: safePage,
      totalPages: totalPages,
      totalItems: totalItems,
      pageSize: safePageSize,
    );
  }
}

