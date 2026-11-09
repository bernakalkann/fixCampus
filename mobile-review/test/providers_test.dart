import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:fixcampus_mobile/services/api_exception.dart';
import 'package:fixcampus_mobile/services/listings_service.dart';
import 'support/listings_fakes.dart';

void main() {
  late FakeListingsService service;
  late ListingsProvider provider;
  setUp(() {
    service = FakeListingsService();
    provider = ListingsProvider(service: service);
  });
  tearDown(() => provider.dispose());

  test(
    'Başlangıçta demo kayıt yok; liste ve kategoriler API den gelir',
    () async {
      expect(provider.ilanlar, isEmpty);
      expect(provider.kategoriler, isEmpty);
      expect(await provider.load(), isTrue);
      expect(provider.ilanlar.single.id, listingId);
      expect(provider.kategoriler.first.id, categoryId);
      expect(provider.radarListings.single.id, listingId);
      expect(provider.getUserListings(ownerId).single.id, listingId);
      expect(provider.getLostFoundListings(), isEmpty);
      expect(() => provider.ilanlar.clear(), throwsUnsupportedError);
    },
  );

  test('CRUD onayından sonra sunucu kimliği ve yanıtı uygulanır', () async {
    await provider.load();
    final created = await provider.createListing(request());
    expect(provider.ilanlar.first.id, created.id);
    await provider.updateListing(listingId, request(title: 'Güncel başlık'));
    expect(provider.ilanlar.last.baslik, 'Güncel başlık');
    expect((await provider.getListing(listingId)).baslik, 'Güncel başlık');
    await provider.deleteListing(listingId);
    expect(provider.ilanlar.any((i) => i.id == listingId), isFalse);
  });

  test(
    '400/401/403/404/503 ve bağlantı hatasında kayıtlar değiştirilmez',
    () async {
      await provider.load();
      for (final status in [400, 401, 403, 404, 503]) {
        service.error = ListingApiException(
          ApiFailure.http,
          'Reddedildi',
          statusCode: status,
        );
        await expectLater(
          provider.updateListing(listingId, request()),
          throwsA(isA<ListingApiException>()),
        );
        await expectLater(
          provider.deleteListing(listingId),
          throwsA(isA<ListingApiException>()),
        );
        expect(provider.ilanlar.single.baslik, 'API ilanı');
        expect(provider.isSaving, isFalse);
      }
      service.error = const ApiException(ApiFailure.connection, 'Çevrimdışı');
      expect(await provider.load(), isFalse);
      expect(provider.error, 'Çevrimdışı');
      expect(provider.ilanlar.single.id, listingId);
      service.error = null;
      expect(await provider.load(), isTrue);
      expect(provider.error, isNull);
    },
  );

  test('Sunucu yanıtı beklenir; çift gönderim engellenir', () async {
    service.pendingCreate = Completer<Ilan>();
    final saving = provider.createListing(request());
    expect(provider.isSaving, isTrue);
    expect(provider.ilanlar, isEmpty);
    await expectLater(
      provider.createListing(request()),
      throwsA(isA<ApiException>()),
    );
    service.pendingCreate!.complete(Ilan.fromJson(listingJson()));
    await saving;
    expect(provider.ilanlar.single.id, listingId);
  });

  test(
    'Hesap değişince liste temizlenir ve önceki istek sonucu uygulanmaz',
    () async {
      service.pendingList = Completer<List<Ilan>>();
      final loading = provider.load();
      provider.setSession(ownerId);
      await Future<void>.delayed(Duration.zero);
      provider.setSession(null);
      service.pendingList!.complete([Ilan.fromJson(listingJson())]);
      await loading;
      await Future<void>.delayed(Duration.zero);
      expect(provider.ilanlar, isEmpty);
      expect(provider.kategoriler, isEmpty);
      expect(provider.isLoading, isFalse);
    },
  );

  test('Hesap değişince eski oluşturma yanıtı yeni hesaba eklenmez', () async {
    service.pendingCreate = Completer<Ilan>();
    final saving = provider.createListing(request());
    provider.setSession(ownerId);
    service.pendingCreate!.complete(Ilan.fromJson(listingJson()));
    await expectLater(saving, throwsA(isA<ApiException>()));
    await Future<void>.delayed(Duration.zero);
    expect(provider.ilanlar.where((i) => i.baslik == 'Yeni ilan'), isEmpty);
  });

  test('Geç gelen liste yanıtı yeni oluşturulan ilanı silmez', () async {
    service.pendingList = Completer<List<Ilan>>();
    final loading = provider.load();
    final created = await provider.createListing(request());
    service.pendingList!.complete([]);
    await loading;
    expect(provider.ilanlar.single.id, created.id);
  });

  test('404 detay yanıtı eski ilanı listeden kaldırır', () async {
    await provider.load();
    service.error = const ListingApiException(
      ApiFailure.http,
      'Silinmiş',
      statusCode: 404,
    );
    await expectLater(
      provider.getListing(listingId),
      throwsA(isA<ListingApiException>()),
    );
    expect(provider.ilanlar, isEmpty);
  });

  test('Mevcut katılım iskeletinin durumu korunur', () async {
    await provider.load();
    provider.toggleAttendance(listingId, ownerId);
    expect(provider.getAttendanceCount(listingId), 1);
    expect(provider.isUserAttending(listingId, ownerId), isTrue);
    provider.toggleAttendance(listingId, ownerId);
    expect(provider.getAttendanceCount(listingId), 0);
  });
}

