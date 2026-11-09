import 'dart:convert';
import 'dart:io';

import 'package:fixcampus_mobile/services/api_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  late HttpServer server;
  late ApiService api;
  late void Function(HttpRequest) handler;
  final paths = <String>[];
  final status = {
    'application': 'fixCampus',
    'status': 'ok',
    'stage': 'api-skeleton',
    'checkedAtUtc': '2026-10-23T12:00:00+00:00',
  };

  setUp(() async {
    paths.clear();
    server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    api = ApiService(
      config: ApiConfig(baseUrl: 'http://127.0.0.1:${server.port}'),
    );
    handler = (request) => reply(request, status);
    server.listen((request) {
      paths.add(request.uri.path);
      handler(request);
    });
  });
  tearDown(() async {
    api.close();
    await server.close(force: true);
  });

  test('Durum yanıtını gerçek HTTP üzerinden okur', () async {
    final result = await api.getStatus();
    expect(result.application, 'fixCampus');
    expect(result.checkedAtUtc, DateTime.utc(2026, 10, 23, 12));
    expect(paths, ['/api/durum']);
  });

  test(
    'Örnek ilan zarfını ve nullable tarihi varlık modeliyle karıştırmaz',
    () async {
      handler = (request) => reply(request, {
        'dataSource': 'sample',
        'isPersisted': false,
        'items': [
          for (final type in ['social', 'lost', 'found'])
            {
              'id': type,
              'type': type,
              'title': 'Örnek',
              'description': 'Açıklama',
              'location': 'Kampüs',
              'startsAtUtc': type == 'social' ? '2026-10-25T11:00:00Z' : null,
            },
        ],
      });
      final result = await api.getSampleListings();
      expect(result.items.length, 3);
      expect(result.isPersisted, isFalse);
      expect(result.items.first.startsAtUtc, DateTime.utc(2026, 10, 25, 11));
      expect(result.items.last.startsAtUtc, isNull);
      expect(paths.single, '/api/ornek/ilanlar');
    },
  );

  test('Veritabanı demo yanıtını ayrı sözleşmeyle okur', () async {
    handler = (request) => reply(request, {
      'id': 'demo',
      'baslik': 'Kayıp defter',
      'aciklama': 'Demo',
      'tur': 'Kayip',
      'yer': 'Kampüs',
      'kategori': 'Kayıp',
      'durum': 'Aktif',
      'olusturulmaTarihi': '2026-10-23T15:00:00+03:00',
      'dataSource': 'database',
      'isPersisted': true,
    });
    final result = await api.getDatabaseDemoListing();
    expect(result.baslik, 'Kayıp defter');
    expect(result.isPersisted, isTrue);
    expect(result.olusturulmaTarihi, DateTime.utc(2026, 10, 23, 12));
    expect(paths.single, '/api/veritabani/ornek-ilan');
  });

  for (final code in [401, 404, 503]) {
    test(
      'HTTP $code yanıtını durum koduyla bildirir, gövdeyi sızdırmaz',
      () async {
        handler = (request) =>
            reply(request, {'detail': 'private diagnostic'}, code);
        await expectLater(
          api.getStatus(),
          throwsA(
            isA<ApiException>()
                .having((e) => e.kind, 'kind', ApiFailure.http)
                .having((e) => e.statusCode, 'statusCode', code)
                .having(
                  (e) => e.message,
                  'message',
                  isNot(contains('private')),
                ),
          ),
        );
      },
    );
  }

  for (final body in [
    <String, dynamic>{'status': 'ok'},
    <Object>[],
    {'items': 3},
  ]) {
    test('Eksik veya farklı biçimli JSON yanıtını reddeder: $body', () async {
      handler = (request) => reply(request, body);
      await expectLater(api.getStatus(), invalidResponse());
    });
  }

  test('Geçersiz JSON için anlaşılır hata verir', () async {
    handler = (request) {
      request.response.headers.contentType = ContentType.json;
      request.response.write('{invalid');
      request.response.close();
    };
    await expectLater(api.getStatus(), invalidResponse());
  });

  for (final value in [
    '2026-02-30T12:00:00Z',
    '2026-10-23T25:00:00Z',
    '2026-10-23T12:00:00+25:00',
    '2026-10-23T12:00:00',
  ]) {
    test('Yeni API sözleşmesinde geçersiz tarihi reddeder: $value', () async {
      handler = (request) => reply(request, {...status, 'checkedAtUtc': value});
      await expectLater(api.getStatus(), invalidResponse());
    });
  }

  test('Yanıt gelmezse zaman aşımını bildirir', () async {
    api.close();
    api = ApiService(
      config: ApiConfig(
        baseUrl: 'http://127.0.0.1:${server.port}',
        timeout: const Duration(milliseconds: 100),
      ),
    );
    handler = (_) {};
    await expectLater(
      api.getStatus(),
      throwsA(
        isA<ApiException>().having((e) => e.kind, 'kind', ApiFailure.timeout),
      ),
    );
  });

  test('Kapalı API bağlantı hatası olarak bildirilir', () async {
    await server.close(force: true);
    await expectLater(
      api.getStatus(),
      throwsA(
        isA<ApiException>().having(
          (e) => e.kind,
          'kind',
          ApiFailure.connection,
        ),
      ),
    );
  });

  test('Base URL alt yolunu kaybetmez', () async {
    api.close();
    api = ApiService(
      config: ApiConfig(baseUrl: 'http://127.0.0.1:${server.port}/backend'),
    );
    await api.getStatus();
    expect(paths.single, '/backend/api/durum');
  });

  test('Hatalı yapılandırmayı istek göndermeden reddeder', () {
    for (final url in [
      'file:///tmp/api',
      '',
      'http://user:pass@localhost',
      'http://localhost?q=1',
    ]) {
      expect(() => ApiConfig(baseUrl: url), throwsArgumentError);
    }
    expect(() => ApiConfig(timeout: Duration.zero), throwsArgumentError);
  });
}

Matcher invalidResponse() => throwsA(
  isA<ApiException>().having((e) => e.kind, 'kind', ApiFailure.invalidResponse),
);

void reply(HttpRequest request, Object body, [int code = 200]) {
  request.response.statusCode = code;
  request.response.headers.contentType = ContentType.json;
  request.response.write(jsonEncode(body));
  request.response.close();
}

