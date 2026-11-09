import 'dart:convert';
import 'dart:typed_data';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:supabase/supabase.dart';
import 'package:fixcampus_mobile/core/network/api_client.dart';
import 'package:fixcampus_mobile/core/network/web_api_auth_client.dart';
import 'package:fixcampus_mobile/services/auth_service.dart';

class RecordingAdapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  String responseString = '{}';

  @override
  Future<ResponseBody> fetch(RequestOptions options,
      Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    requests.add(options);
    return ResponseBody.fromString(responseString, 200, headers: {
      'content-type': ['application/json']
    });
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  test('API her istekte yenilenen tokenı kullanır; çıkışta başlığı kaldırır',
      () async {
    String? token = 'ilk-token';
    final adapter = RecordingAdapter();
    final dio = Dio(BaseOptions(baseUrl: 'https://api.example.com'))
      ..httpClientAdapter = adapter;
    final api = ApiClient(dio: dio, tokenProvider: () => token);
    await api.dio.get('/api/profil');
    expect(adapter.requests.last.headers['Authorization'], 'Bearer ilk-token');
    token = 'yenilenmis-token';
    await api.dio.get('/api/profil');
    expect(adapter.requests.last.headers['Authorization'],
        'Bearer yenilenmis-token');
    token = null;
    await api.dio.get('/api/profil',
        options: Options(headers: {'authorization': 'Bearer eski'}));
    expect(
        adapter.requests.last.headers.keys
            .any((name) => name.toLowerCase() == 'authorization'),
        false);
    dio.close();
  });

  test(
      'ApiAuthService saveProfile PUT /api/profil isteğinde adSoyad, bolum ve sinif gönderir',
      () async {
    final adapter = RecordingAdapter();
    adapter.responseString = jsonEncode({
      'id': '00000000-0000-0000-0000-000000000001',
      'adSoyad': 'Berke Mahmut Acar',
      'rol': 'Ogrenci',
      'olusturulmaTarihi': '2026-10-21T00:00:00Z',
      'bolum': 'Yazılım Mühendisliği',
      'sinif': '3. Sınıf',
    });
    final dio = Dio(BaseOptions(baseUrl: 'https://api.example.com'))
      ..httpClientAdapter = adapter;
    final api = ApiClient(dio: dio, tokenProvider: () => 'token');

    final transport = WebApiAuthClient(
      apiBaseUrl: 'https://api.example.com',
      supabaseUrl: 'https://project.supabase.co',
      inner: MockClient((_) async => http.Response('{}', 200)),
    );
    final client = SupabaseClient(
      'https://project.supabase.co',
      'public-key',
      httpClient: transport,
    );

    final service = ApiAuthService(client, api, transport);
    final profile = await service.saveProfile(
      'Berke Mahmut Acar',
      bolum: 'Yazılım Mühendisliği',
      sinif: '3. Sınıf',
    );

    expect(adapter.requests.last.method, 'PUT');
    expect(adapter.requests.last.path, '/api/profil');
    expect(adapter.requests.last.data, {
      'adSoyad': 'Berke Mahmut Acar',
      'bolum': 'Yazılım Mühendisliği',
      'sinif': '3. Sınıf',
    });
    expect(profile.bolum, 'Yazılım Mühendisliği');
    expect(profile.sinif, '3. Sınıf');

    await client.dispose();
    dio.close();
  });
}

