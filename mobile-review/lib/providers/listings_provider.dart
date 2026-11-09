import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/api_exception.dart';
import '../services/listings_service.dart';

/// Ortak Web API'den okunan ilanlar ve yalnızca onaylanan değişiklikler.
class ListingsProvider extends ChangeNotifier {
  ListingsProvider({ListingsService? service}) : _service = service;

  final ListingsService? _service;
  List<Kategori> _kategoriler = [];
  List<Ilan> _ilanlar = [];
  final List<Katilim> _katilimlar = [];
  bool _isLoading = false;
  bool _isSaving = false;
  bool _disposed = false;
  String? _error;
  String? _userId;
  int _sessionVersion = 0;
  int _dataVersion = 0;
  int _loadVersion = 0;

  List<Kategori> get kategoriler => List.unmodifiable(_kategoriler);
  List<Ilan> get ilanlar => List.unmodifiable(_ilanlar);
  List<Katilim> get katilimlar => List.unmodifiable(_katilimlar);
  int get dataRevision => _dataVersion;
  int get sessionRevision => _sessionVersion;
  bool get isLoading => _isLoading;
  bool get isSaving => _isSaving;
  String? get error => _error;

  /// ProxyProvider güncellemesi sırasında eski hesap verisi hemen temizlenir.
  /// Bildirim ve yükleme build bittikten sonra yapılır.
  void setSession(String? userId) {
    if (_userId == userId) return;
    _userId = userId;
    final version = ++_sessionVersion;
    ++_loadVersion;
    _ilanlar = [];
    _kategoriler = [];
    _katilimlar.clear();
    _error = null;
    _isSaving = false;
    _isLoading = userId != null;
    Future.microtask(() async {
      if (!_isCurrent(version)) return;
      _notify();
      if (userId != null) await load();
    });
  }

  List<Ilan> get radarListings => _ilanlar
      .where((i) => i.tur == IlanTuru.sosyal && i.durum == IlanDurumu.aktif)
      .toList();

  List<Ilan> getLostFoundListings({IlanTuru? turFilter}) => _ilanlar.where((i) {
    final lostFound = i.tur == IlanTuru.kayip || i.tur == IlanTuru.buluntu;
    return lostFound && (turFilter == null || i.tur == turFilter);
  }).toList();

  List<Ilan> getUserListings(String userId) =>
      _ilanlar.where((i) => i.sahipId == userId).toList();

  Future<bool> load() async {
    final session = _sessionVersion;
    final load = ++_loadVersion;
    final data = _dataVersion;
    _isLoading = true;
    _error = null;
    _notify();
    try {
      final service = _requireService();
      // İkisi de başarılı olmadan eski listeyi kısmen değiştirme.
      final results = await Future.wait<Object>([
        service.getCategories(),
        service.getListings(),
      ]);
      if (!_isCurrent(session) || load != _loadVersion) return false;
      _kategoriler = results[0] as List<Kategori>;
      if (data == _dataVersion) {
        _ilanlar = List.of(results[1] as List<Ilan>);
        ++_dataVersion;
      }
      return true;
    } catch (e) {
      if (_isCurrent(session) && load == _loadVersion) _error = _message(e);
      return false;
    } finally {
      if (_isCurrent(session) && load == _loadVersion) {
        _isLoading = false;
        _notify();
      }
    }
  }

  /// Filtreli sonuçları ortak akış listesinin yerine yazmaz.
  /// Ekran, filtre değişince ListingQuery(page: 1, ...) ile tekrar sorgular.
  Future<ListingPage> searchListings(ListingQuery query) async {
    final session = _sessionVersion;
    final result = await _requireService().searchListings(query);
    _checkSession(session);
    return result;
  }

  Future<Ilan> getListing(String id) async {
    final session = _sessionVersion;
    final data = _dataVersion;
    try {
      final ilan = await _requireService().getListing(id);
      _checkSession(session);
      // Önceden başlayan detay isteği yeni güncellemeyi ezmesin.
      if (data == _dataVersion) {
        _upsert(ilan);
        _notify();
      }
      return ilan;
    } on ListingApiException catch (e) {
      if (_isCurrent(session) && e.statusCode == 404) {
        _ilanlar.removeWhere((i) => i.id == id);
        _notify();
      }
      rethrow;
    }
  }

  Future<Ilan> createListing(SaveListingRequest request) => _save(
    () => _requireService().createListing(request),
    (ilan) => _upsert(ilan),
  );

  Future<Ilan> updateListing(String id, SaveListingRequest request) => _save(
    () => _requireService().updateListing(id, request),
    (ilan) => _upsert(ilan),
  );

  Future<void> deleteListing(String id) =>
      _save<void>(() => _requireService().deleteListing(id), (_) {
        _ilanlar.removeWhere((i) => i.id == id);
        _katilimlar.removeWhere((k) => k.ilanId == id);
      });

  Future<T> _save<T>(
    Future<T> Function() action,
    void Function(T) apply,
  ) async {
    if (_isSaving) {
      throw const ApiException(ApiFailure.http, 'Devam eden işlemi bekleyin.');
    }
    final session = _sessionVersion;
    _isSaving = true;
    _notify();
    try {
      final result = await action();
      _checkSession(session);
      ++_dataVersion;
      apply(result);
      return result;
    } finally {
      if (_isCurrent(session)) {
        _isSaving = false;
        _notify();
      }
    }
  }

  void _upsert(Ilan ilan) {
    final index = _ilanlar.indexWhere((i) => i.id == ilan.id);
    if (index < 0) {
      _ilanlar = [ilan, ..._ilanlar];
    } else {
      _ilanlar[index] = ilan;
    }
  }

  ListingsService _requireService() =>
      _service ??
      (throw const ApiException(
        ApiFailure.connection,
        'İlan servisi yapılandırılmamış.',
      ));

  bool _isCurrent(int version) => !_disposed && version == _sessionVersion;

  void _checkSession(int version) {
    if (!_isCurrent(version)) {
      throw const ApiException(
        ApiFailure.http,
        'Oturum değişti. Yeniden deneyin.',
      );
    }
  }

  static String _message(Object error) => error is ApiException
      ? error.message
      : 'İlanlar yüklenemedi. Tekrar deneyin.';

  void _notify() {
    if (!_disposed) notifyListeners();
  }

  // Katılım servisi 5. hafta görevidir; mevcut ekran iskeletinin yerel durumu.
  bool isUserAttending(String ilanId, String userId) =>
      _katilimlar.any((k) => k.ilanId == ilanId && k.profilId == userId);

  int getAttendanceCount(String ilanId) =>
      _katilimlar.where((k) => k.ilanId == ilanId).length;

  void toggleAttendance(String ilanId, String userId) {
    final index = _katilimlar.indexWhere(
      (k) => k.ilanId == ilanId && k.profilId == userId,
    );
    if (index >= 0) {
      _katilimlar.removeAt(index);
    } else {
      _katilimlar.add(
        Katilim(
          id: 'katilim-${DateTime.now().microsecondsSinceEpoch}',
          ilanId: ilanId,
          profilId: userId,
          katilimTarihi: DateTime.now().toUtc(),
        ),
      );
    }
    _notify();
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
