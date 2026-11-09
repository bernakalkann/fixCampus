import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../providers/auth_provider.dart';
import '../../providers/listings_provider.dart';
import '../../routes/app_routes.dart';
import '../../security/listing_access.dart';
import '../../security/listing_mutation_policy.dart';
import '../../security/role_session_policy.dart';
import '../../services/api_exception.dart';
import '../../widgets/app_button.dart';
import '../../widgets/status_badge.dart';

class ListingDetailScreen extends StatefulWidget {
  const ListingDetailScreen({super.key, required this.ilan});
  final Ilan ilan;

  @override
  State<ListingDetailScreen> createState() => _ListingDetailScreenState();
}

class _ListingDetailScreenState extends State<ListingDetailScreen> {
  Ilan? _ilan;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _load();
    });
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final ilan = await context.read<ListingsProvider>().getListing(
        widget.ilan.id,
      );
      if (mounted) setState(() => _ilan = ilan);
    } catch (e) {
      if (mounted) {
        setState(
          () => _error = e is ApiException ? e.message : 'İlan yüklenemedi.',
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _delete() async {
    if (!_checkAccess(ListingMutation.delete)) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('İlan silinsin mi?'),
        content: const Text('Bu ilan kalıcı olarak silinecek.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Vazgeç'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Sil'),
          ),
        ],
      ),
    );
    if (!mounted || confirmed != true) return;
    if (!_checkAccess(ListingMutation.delete)) return;
    try {
      await context.read<ListingsProvider>().deleteListing(widget.ilan.id);
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e is ApiException ? e.message : 'İlan silinemedi.'),
          ),
        );
      }
    }
  }

  Future<void> _edit() async {
    if (!_checkAccess(ListingMutation.update)) return;
    final changed = await Navigator.pushNamed(
      context,
      AppRoutes.editListing,
      arguments: _ilan,
    );
    if (mounted && changed == true) await _load();
  }

  bool _checkAccess(ListingMutation action) {
    final access = listingAccess(
      context.read<AuthProvider>(),
      action,
      listing: _loading || _error != null ? null : _ilan,
    );
    if (access == RoleAccess.allowed) return true;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(listingAccessMessage(access))));
    return false;
  }

  void _skeleton(String action) => ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('“$action” işlemi yakında kullanıma sunulacaktır.')),
  );

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<ListingsProvider>();
    final auth = context.watch<AuthProvider>();
    final ilan = _ilan;
    final canManage =
        ilan != null &&
        listingAccess(
          auth,
          ListingMutation.update,
          listing: ilan,
        ) ==
        RoleAccess.allowed;

    final kategori = ilan != null
        ? provider.kategoriler.where((k) => k.id == ilan.kategoriId)
        : null;
    final kategoriAdi =
        kategori != null && kategori.isNotEmpty ? kategori.first.ad : null;

    final isAttending =
        ilan != null &&
        auth.currentUser != null &&
        provider.isUserAttending(ilan.id, auth.currentUser!.id);
    final attendanceCount =
        ilan != null ? provider.getAttendanceCount(ilan.id) : 0;

    return Scaffold(
      appBar: AppBar(title: const Text('İlan Detayı')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(_error!, textAlign: TextAlign.center),
                    const SizedBox(height: 12),
                    TextButton(
                      onPressed: _load,
                      child: const Text('Tekrar dene'),
                    ),
                  ],
                ),
              ),
            )
          : ilan == null
          ? const SizedBox.shrink()
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Rozetler & Kategori
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      StatusBadge.tur(tur: ilan.tur),
                      StatusBadge.durum(durum: ilan.durum),
                      if (kategoriAdi != null)
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 10,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: const Color(0xFFF1F5F9),
                            borderRadius: BorderRadius.circular(6),
                            border: Border.all(color: const Color(0xFFCBD5E1)),
                          ),
                          child: Text(
                            kategoriAdi,
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: Color(0xFF475569),
                            ),
                          ),
                        ),
                    ],
                  ),
                  const SizedBox(height: 14),

                  // Başlık
                  Text(
                    ilan.baslik,
                    style: const TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF0F172A),
                      letterSpacing: -0.5,
                    ),
                  ),
                  const SizedBox(height: 6),

                  // Oluşturulma Tarihi
                  Row(
                    children: [
                      const Icon(
                        Icons.access_time,
                        size: 14,
                        color: Color(0xFF94A3B8),
                      ),
                      const SizedBox(width: 4),
                      Text(
                        DateFormat(
                          'dd.MM.yyyy, HH:mm',
                        ).format(ilan.olusturulmaTarihi.toLocal()),
                        style: const TextStyle(
                          fontSize: 12,
                          color: Color(0xFF64748B),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // Konum ve Buluşma Zamanı Bilgi Kartı
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF8FAFC),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFE2E8F0)),
                    ),
                    child: Column(
                      children: [
                        Row(
                          children: [
                            const Icon(
                              Icons.location_on_outlined,
                              size: 18,
                              color: Color(0xFF2563EB),
                            ),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                'Yer: ${ilan.yer}',
                                style: const TextStyle(
                                  fontSize: 14,
                                  fontWeight: FontWeight.w600,
                                  color: Color(0xFF1E293B),
                                ),
                              ),
                            ),
                          ],
                        ),
                        if (ilan.bulusmaTarihi != null) ...[
                          const Padding(
                            padding: EdgeInsets.symmetric(vertical: 8),
                            child: Divider(height: 1),
                          ),
                          Row(
                            children: [
                              const Icon(
                                Icons.calendar_today_outlined,
                                size: 16,
                                color: Color(0xFF16A34A),
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  DateFormat(
                                    'dd.MM.yyyy, HH:mm',
                                  ).format(ilan.bulusmaTarihi!.toLocal()),
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w600,
                                    color: Color(0xFF1E293B),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Açıklama Kartı
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFE4E4E7)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Açıklama',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                            color: Color(0xFF18181B),
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          ilan.aciklama,
                          style: const TextStyle(
                            fontSize: 14,
                            color: Color(0xFF3F3F46),
                            height: 1.5,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Sahiplik Eylem Çubuğu
                  if (canManage) ...[
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: const Color(0xFFF8FAFC),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: const Color(0xFFE2E8F0)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Row(
                            children: [
                              Icon(
                                Icons.manage_accounts_outlined,
                                size: 18,
                                color: Color(0xFF0F172A),
                              ),
                              SizedBox(width: 8),
                              Text(
                                'İlanınızı Yönetin',
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w700,
                                  color: Color(0xFF0F172A),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          AppButton(
                            label: 'İlanı Düzenle',
                            icon: Icons.edit_outlined,
                            onPressed: provider.isSaving ? null : _edit,
                          ),
                          const SizedBox(height: 10),
                          AppButton(
                            label: 'İlanı Sil',
                            icon: Icons.delete_outline,
                            isOutlined: true,
                            isLoading: provider.isSaving,
                            onPressed: _delete,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // Katılım Butonu (Sosyal İlanlar İçin)
                  if (ilan.tur == IlanTuru.sosyal) ...[
                    AppButton(
                      label: isAttending
                          ? 'Katıldın ($attendanceCount)'
                          : 'Buluşmaya Katıl ($attendanceCount)',
                      icon: isAttending
                          ? Icons.check
                          : Icons.group_add_outlined,
                      isOutlined: isAttending,
                      onPressed: auth.currentUser == null
                          ? null
                          : () {
                              provider.toggleAttendance(
                                ilan.id,
                                auth.currentUser!.id,
                              );
                            },
                    ),
                    const SizedBox(height: 10),
                  ],

                  // Şikayet Butonu
                  AppButton(
                    label: 'İlanı Şikâyet Et (İskelet)',
                    icon: Icons.flag_outlined,
                    isOutlined: true,
                    onPressed: () => _skeleton('Şikâyet Et'),
                  ),
                  const SizedBox(height: 24),
                ],
              ),
            ),
    );
  }
}
