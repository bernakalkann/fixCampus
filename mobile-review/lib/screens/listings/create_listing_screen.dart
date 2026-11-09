import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../providers/listings_provider.dart';
import '../../providers/auth_provider.dart';
import '../../security/listing_access.dart';
import '../../security/listing_mutation_policy.dart';
import '../../security/role_session_policy.dart';
import '../../validation/listing_validator.dart';
import '../../services/api_exception.dart';
import '../../services/listings_service.dart';
import '../../widgets/app_button.dart';
import '../../widgets/listings_load_status.dart';

/// Mevcut ilan formunun ortak API'ye bağlanan oluşturma/düzenleme akışı.
class CreateListingScreen extends StatefulWidget {
  const CreateListingScreen({super.key, this.ilan});
  final Ilan? ilan;

  @override
  State<CreateListingScreen> createState() => _CreateListingScreenState();
}

class _CreateListingScreenState extends State<CreateListingScreen> {
  final _form = GlobalKey<FormState>();
  late final TextEditingController _baslik;
  late final TextEditingController _aciklama;
  late final TextEditingController _yer;
  late IlanTuru _tur;
  String? _kategoriId;
  DateTime? _tarih;
  String? _error;
  bool _saving = false;
  static const _validator = ListingValidator();

  RoleAccess _access(AuthProvider auth) => listingAccess(
    auth,
    widget.ilan == null ? ListingMutation.create : ListingMutation.update,
    listing: widget.ilan,
  );

  @override
  void initState() {
    super.initState();
    final ilan = widget.ilan;
    _baslik = TextEditingController(text: ilan?.baslik);
    _aciklama = TextEditingController(text: ilan?.aciklama);
    _yer = TextEditingController(text: ilan?.yer);
    _tur = ilan?.tur ?? IlanTuru.sosyal;
    _kategoriId = ilan?.kategoriId;
    _tarih = ilan?.bulusmaTarihi?.toLocal();
  }

  @override
  void dispose() {
    _baslik.dispose();
    _aciklama.dispose();
    _yer.dispose();
    super.dispose();
  }

  Future<void> _selectDate() async {
    final now = DateTime.now();
    final first = DateTime(now.year, now.month, now.day);
    final last = DateTime(now.year + 5, 12, 31);
    final initial =
        _tarih != null && !_tarih!.isBefore(first) && !_tarih!.isAfter(last)
        ? _tarih!
        : now;
    final date = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: first,
      lastDate: last,
    );
    if (!mounted || date == null) return;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(initial),
    );
    if (!mounted || time == null) return;
    setState(
      () => _tarih = DateTime(
        date.year,
        date.month,
        date.day,
        time.hour,
        time.minute,
      ),
    );
  }

  Future<void> _submit() async {
    if (_saving || !_form.currentState!.validate()) return;
    final access = _access(context.read<AuthProvider>());
    if (access != RoleAccess.allowed) {
      setState(() => _error = listingAccessMessage(access));
      return;
    }
    final dateError = _validator.meetingDate(_tur, _tarih);
    if (dateError != null) {
      setState(() => _error = dateError);
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    final provider = context.read<ListingsProvider>();
    final request = SaveListingRequest(
      baslik: _baslik.text,
      aciklama: _aciklama.text,
      kategoriId: _kategoriId!,
      tur: _tur,
      yer: _yer.text,
      bulusmaTarihi: _tur == IlanTuru.sosyal ? _tarih : null,
    );
    try {
      final original = widget.ilan;
      if (original != null) {
        await provider.updateListing(original.id, request);
        if (mounted) Navigator.pop(context, true);
      } else {
        await provider.createListing(request);
        if (!mounted) return;
        _baslik.clear();
        _aciklama.clear();
        _yer.clear();
        // Dropdown reset'i eski onChanged değerini geri çağırabilir.
        // Önce alanları sıfırla, ardından yeni ilan durumunu uygula.
        _form.currentState!.reset();
        setState(() {
          _kategoriId = null;
          _tarih = null;
          _tur = IlanTuru.sosyal;
        });
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('İlan yayınlandı.')));
      }
    } catch (e) {
      if (mounted) {
        setState(
          () => _error = e is ApiException
              ? e.message
              : 'İlan kaydedilemedi. Tekrar deneyin.',
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final access = _access(context.watch<AuthProvider>());
    if (access != RoleAccess.allowed) {
      return Scaffold(
        appBar: AppBar(title: const Text('İlan İşlemi')),
        body: Center(child: Text(listingAccessMessage(access))),
      );
    }
    final provider = context.watch<ListingsProvider>();
    final categories = provider.kategoriler
        .where((k) => _validator.acceptsCategory(_tur, k))
        .toList();
    final selected = categories.any((k) => k.id == _kategoriId)
        ? _kategoriId
        : null;
    final busy = _saving || provider.isSaving;
    return Scaffold(
      appBar: widget.ilan == null
          ? null
          : AppBar(title: const Text('İlanı Düzenle')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Başlık ve Açıklayıcı Kart
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFE2E8F0)),
                ),
                child: Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: const Color(0xFF18181B),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Icon(
                        widget.ilan == null
                            ? Icons.add_circle_outline
                            : Icons.edit_outlined,
                        color: Colors.white,
                        size: 20,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            widget.ilan == null ? 'Yeni İlan' : 'İlan Bilgileri',
                            style: const TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w800,
                              color: Color(0xFF0F172A),
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            widget.ilan == null
                                ? 'Kampüsteki arkadaşlarınla paylaş'
                                : 'İlan detaylarını güncelleyin',
                            style: const TextStyle(
                              fontSize: 12,
                              color: Color(0xFF64748B),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              ListingsLoadStatus(provider: provider),

              // İlan Türü Dropdown
              DropdownButtonFormField<IlanTuru>(
                key: ValueKey(_tur),
                initialValue: _tur,
                decoration: InputDecoration(
                  labelText: 'İlan türü',
                  prefixIcon: const Icon(Icons.campaign_outlined, size: 20),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                items: const [
                  DropdownMenuItem(
                    value: IlanTuru.sosyal,
                    child: Text('Sosyal'),
                  ),
                  DropdownMenuItem(value: IlanTuru.kayip, child: Text('Kayıp')),
                  DropdownMenuItem(
                    value: IlanTuru.buluntu,
                    child: Text('Buluntu'),
                  ),
                ],
                onChanged: busy
                    ? null
                    : (value) => setState(() {
                        _tur = value!;
                        _kategoriId = null;
                        _tarih = null;
                      }),
              ),
              const SizedBox(height: 16),

              // İlan Başlığı (TextFormField 0)
              TextFormField(
                controller: _baslik,
                enabled: !busy,
                maxLength: 150,
                decoration: InputDecoration(
                  labelText: 'İlan Başlığı',
                  hintText: 'Örn: Final Çalışma Grubu / Mavi Defter',
                  prefixIcon: const Icon(Icons.title_outlined, size: 20),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                validator: _validator.title,
              ),
              const SizedBox(height: 16),

              // Kategori Dropdown
              DropdownButtonFormField<String>(
                key: ValueKey('${_tur.name}:$selected'),
                initialValue: selected,
                isExpanded: true,
                decoration: InputDecoration(
                  labelText: 'Kategori',
                  prefixIcon: const Icon(Icons.category_outlined, size: 20),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                hint: Text(
                  categories.isEmpty
                      ? 'Kategori yüklenemedi'
                      : 'Kategori seçin',
                ),
                items: categories
                    .map(
                      (k) => DropdownMenuItem(value: k.id, child: Text(k.ad)),
                    )
                    .toList(),
                onChanged: busy
                    ? null
                    : (value) => setState(() => _kategoriId = value),
                validator: (_) => _validator.category(
                  selected,
                  type: _tur,
                  categories: provider.kategoriler,
                ),
              ),
              const SizedBox(height: 16),

              // Yer (TextFormField 1)
              TextFormField(
                controller: _yer,
                enabled: !busy,
                maxLength: 200,
                decoration: InputDecoration(
                  labelText: 'Yer',
                  hintText: 'Örn: Merkez Kütüphane 2. Kat',
                  prefixIcon: const Icon(Icons.location_on_outlined, size: 20),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                validator: _validator.location,
              ),
              const SizedBox(height: 16),

              // Açıklama (TextFormField 2)
              TextFormField(
                controller: _aciklama,
                enabled: !busy,
                maxLength: 3000,
                maxLines: 4,
                decoration: InputDecoration(
                  labelText: 'Açıklama',
                  hintText: 'Detaylı bilgi, notlar veya koşullar...',
                  prefixIcon: const Icon(Icons.notes_outlined, size: 20),
                  alignLabelWithHint: true,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                validator: _validator.description,
              ),

              // Sosyal İlan Buluşma Zamanı
              if (_tur == IlanTuru.sosyal) ...[
                const SizedBox(height: 16),
                OutlinedButton.icon(
                  onPressed: busy ? null : _selectDate,
                  icon: const Icon(Icons.schedule, size: 18),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 14,
                    ),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                  label: Text(
                    _tarih == null
                        ? 'Buluşma tarihi ve saati seçin'
                        : DateFormat('dd.MM.yyyy HH:mm').format(_tarih!),
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                ),
              ],
              if (_error != null) ...[
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFEF2F2),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: const Color(0xFFFCA5A5)),
                  ),
                  child: Row(
                    children: [
                      const Icon(
                        Icons.error_outline,
                        color: Color(0xFFDC2626),
                        size: 18,
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          _error!,
                          style: const TextStyle(
                            color: Color(0xFFDC2626),
                            fontSize: 13,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 24),
              AppButton(
                label: widget.ilan == null
                    ? 'İlanı Yayınla'
                    : 'Değişiklikleri Kaydet',
                icon: widget.ilan == null
                    ? Icons.check_circle_outline
                    : Icons.save_outlined,
                isLoading: busy,
                onPressed: provider.isLoading || categories.isEmpty
                    ? null
                    : _submit,
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}
