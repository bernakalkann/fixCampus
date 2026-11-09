import 'dart:async';
import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/utils/listing_search_filter.dart';
import 'package:fixcampus_mobile/services/listings_service.dart';

const ownerId = '00000000-0000-0000-0000-000000000001';
const categoryId = '10000000-0000-0000-0000-000000000001';
const listingId = '20000000-0000-0000-0000-000000000001';

Map<String, dynamic> listingJson({
  String id = listingId,
  String title = 'API ilanı',
}) => {
  'id': id,
  'sahipId': ownerId,
  'kategoriId': categoryId,
  'kategori': 'Spor',
  'baslik': title,
  'aciklama': 'Açıklama',
  'tur': 'Sosyal',
  'yer': 'Kampüs',
  'bulusmaTarihi': '2030-11-05T12:00:00Z',
  'durum': 'Aktif',
  'olusturulmaTarihi': '2026-11-03T10:00:00Z',
};

SaveListingRequest request({String title = 'Yeni ilan'}) => SaveListingRequest(
  baslik: title,
  aciklama: 'Açıklama',
  kategoriId: categoryId,
  tur: IlanTuru.sosyal,
  yer: 'Kampüs',
  bulusmaTarihi: DateTime.utc(2030, 11, 5, 12),
);

class FakeListingsService implements ListingsService {
  final queries = <ListingQuery>[];
  @override
  Future<ListingPage> searchListings(ListingQuery query) async {
    check();
    queries.add(query);
    final filtered = ListingSearchFilter.filter(
      items
          .where(
            (item) =>
                item.durum != IlanDurumu.gizli &&
                (!query.lostFoundOnly || item.tur != IlanTuru.sosyal) &&
                (!query.activeSocialOnly ||
                    item.tur != IlanTuru.sosyal ||
                    item.durum == IlanDurumu.aktif),
          )
          .toList(),
      query: query.text ?? '',
      tur: query.tur,
      kategoriId: query.kategoriId,
      kategoriler: categories,
    );
    final params = query.toQueryParameters();
    final page = params['sayfa'] as int;
    final size = params['sayfaBoyutu'] as int;
    final totalPages = filtered.isEmpty
        ? 1
        : (filtered.length + size - 1) ~/ size;
    return ListingPage(
      items: filtered.skip((page - 1) * size).take(size).toList(),
      totalCount: filtered.length,
      page: page,
      pageSize: size,
      totalPages: totalPages,
    );
  }

  List<Ilan> items = [Ilan.fromJson(listingJson())];
  final categories = [
    const Kategori(id: categoryId, ad: 'Spor', tur: IlanTuru.sosyal),
    const Kategori(id: 'lost-category', ad: 'Elektronik', tur: IlanTuru.kayip),
    const Kategori(
      id: 'found-category',
      ad: 'Elektronik',
      tur: IlanTuru.buluntu,
    ),
  ];
  Object? error;
  Completer<List<Ilan>>? pendingList;
  Completer<Ilan>? pendingCreate;
  SaveListingRequest? savedRequest;
  int detailsRead = 0;
  int creates = 0;
  int deletes = 0;
  void check() {
    if (error != null) throw error!;
  }

  @override
  Future<List<Kategori>> getCategories() async {
    check();
    return List.unmodifiable(categories);
  }

  @override
  Future<List<Ilan>> getListings() async {
    check();
    return pendingList != null ? pendingList!.future : List.unmodifiable(items);
  }

  @override
  Future<Ilan> getListing(String id) async {
    check();
    detailsRead++;
    return items.firstWhere((i) => i.id == id);
  }

  @override
  Future<Ilan> createListing(SaveListingRequest value) async {
    check();
    savedRequest = value;
    creates++;
    if (pendingCreate != null) return pendingCreate!.future;
    final result = Ilan.fromJson({
      ...listingJson(id: 'new-id'),
      ...value.toJson(),
    });
    items.insert(0, result);
    return result;
  }

  @override
  Future<Ilan> updateListing(String id, SaveListingRequest value) async {
    check();
    savedRequest = value;
    final result = Ilan.fromJson({...listingJson(id: id), ...value.toJson()});
    items[items.indexWhere((i) => i.id == id)] = result;
    return result;
  }

  @override
  Future<void> deleteListing(String id) async {
    check();
    deletes++;
    items.removeWhere((i) => i.id == id);
  }
}
