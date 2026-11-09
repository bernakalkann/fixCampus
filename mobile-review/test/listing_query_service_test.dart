import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fixcampus_mobile/core/network/api_client.dart';
import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:fixcampus_mobile/services/listings_service.dart';

import 'listings_service_test.dart' show ListingsAdapter;
import 'support/listings_fakes.dart';

class PendingQueryService extends FakeListingsService {
  final pending = Completer<ListingPage>();

  @override
  Future<ListingPage> searchListings(ListingQuery query) => pending.future;
}

void main() {
  late ListingsAdapter adapter;
  late Dio dio;
  late ApiListingsService service;

  setUp(() {
    adapter = ListingsAdapter();
    dio = Dio(BaseOptions(baseUrl: 'https://api.example.com'))
      ..httpClientAdapter = adapter;
    service = ApiListingsService(
      ApiClient(dio: dio, tokenProvider: () => 'student-token'),
    );
  });
  tearDown(() => dio.close());

  Map<String, dynamic> pageJson() => {
    'items': [listingJson()],
    'totalCount': 5,
    'page': 2,
    'pageSize': 2,
    'totalPages': 3,
  };

  test(
    'Filtreler ve sayfa API ye tek istekle, Bearer ile gönderilir',
    () async {
      adapter.body = pageJson();
      final page = await service.searchListings(
        const ListingQuery(
          tur: IlanTuru.sosyal,
          kategoriId: ' $categoryId ',
          text: '  C++ & %_ kampüs  ',
          page: 2,
          pageSize: 2,
        ),
      );
      expect(adapter.requests, hasLength(1));
      final sent = adapter.requests.single;
      expect(sent.path, '/api/ilanlar/sayfali');
      expect(sent.queryParameters, {
        'tur': 'Sosyal',
        'kategoriId': categoryId,
        'q': 'C++ & %_ kampüs',
        'sayfa': 2,
        'sayfaBoyutu': 2,
      });
      expect(sent.headers['Authorization'], 'Bearer student-token');
      expect(page.items.single.id, listingId);
      expect(page.totalCount, 5);
      expect(page.hasNext, isTrue);
      expect(page.hasPrevious, isTrue);
    },
  );

  test('Boş filtreler gönderilmez ve sınırlar API ile aynıdır', () {
    expect(
      const ListingQuery(
        text: ' ',
        kategoriId: ' ',
        page: -1,
        pageSize: 0,
      ).toQueryParameters(),
      {'sayfa': 1, 'sayfaBoyutu': 12},
    );
    expect(const ListingQuery(pageSize: 999).toQueryParameters(), {
      'sayfa': 1,
      'sayfaBoyutu': 50,
    });
  });

  test(
    'Boş veya aralık dışı sayfa geçerlidir; tutarsız yanıt reddedilir',
    () async {
      adapter.body = {
        'items': [],
        'totalCount': 0,
        'page': 99,
        'pageSize': 12,
        'totalPages': 1,
      };
      expect(
        (await service.searchListings(const ListingQuery(page: 99))).items,
        isEmpty,
      );
      for (final bad in [
        <String, dynamic>{'items': []},
        {...pageJson(), 'page': 0},
        {...pageJson(), 'totalPages': 9},
        {
          ...pageJson(),
          'items': [42],
        },
        {...pageJson(), 'pageSize': 51},
      ]) {
        adapter.body = bad;
        await expectLater(
          service.searchListings(const ListingQuery()),
          throwsA(isA<ListingApiException>()),
        );
      }
    },
  );

  test('Sorgu hatası boş sonuç gibi gösterilmez', () async {
    adapter.status = 401;
    adapter.body = {'message': 'private detail'};
    await expectLater(
      service.searchListings(const ListingQuery()),
      throwsA(
        isA<ListingApiException>().having(
          (error) => error.statusCode,
          'status',
          401,
        ),
      ),
    );
  });

  test('Hesap değişince önceki hesabın sorgu yanıtı reddedilir', () async {
    final fake = PendingQueryService();
    final provider = ListingsProvider(service: fake);
    addTearDown(provider.dispose);
    provider.setSession('first-student');
    await Future<void>.delayed(Duration.zero);
    final result = provider.searchListings(const ListingQuery());
    final check = expectLater(result, throwsA(isA<Exception>()));
    provider.setSession('second-student');
    fake.pending.complete(
      const ListingPage(
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 12,
        totalPages: 1,
      ),
    );
    await check;
  });
}
