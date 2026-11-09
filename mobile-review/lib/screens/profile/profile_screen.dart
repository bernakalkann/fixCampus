import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../providers/auth_provider.dart';
import '../../providers/listings_provider.dart';
import '../../routes/app_routes.dart';
import '../../widgets/empty_state.dart';
import '../../widgets/listing_card.dart';
import '../../widgets/status_badge.dart';

/// Profil Ekranı (Kullanıcı Bilgileri, Bölüm, Sınıf, İlanlarım, Oturum ve Düzenleme)
class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  Future<void> _editProfile(BuildContext context) async {
    final auth = context.read<AuthProvider>();
    final user = auth.currentUser;

    final result = await showDialog<_EditProfileResult>(
      context: context,
      builder: (_) => _EditProfileDialog(
        initialName: user?.adSoyad ?? '',
        initialBolum: user?.bolum ?? 'Bilgisayar Mühendisliği',
        initialSinif: user?.sinif ?? '4. Sınıf',
      ),
    );

    if (result == null || !context.mounted) return;

    final success = await auth.updateProfile(
      result.name,
      bolum: result.bolum,
      sinif: result.sinif,
    );

    if (!context.mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          success
              ? 'Profil güncellendi.'
              : auth.message ?? 'Profil güncellenemedi.',
        ),
      ),
    );
  }

  Future<void> _logout(BuildContext context) async {
    final auth = context.read<AuthProvider>();
    final success = await auth.logout();
    if (!context.mounted) return;

    if (success) {
      Navigator.pushNamedAndRemoveUntil(context, AppRoutes.login, (_) => false);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(auth.message ?? 'Çıkış yapılamadı.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final listingsProvider = context.watch<ListingsProvider>();
    final user = auth.currentUser;
    final userListings = listingsProvider.getUserListings(user?.id ?? '');

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(vertical: 14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Profil Kartı
            Container(
              margin: const EdgeInsets.symmetric(horizontal: 16),
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFE4E4E7)),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x05000000),
                    blurRadius: 10,
                    offset: Offset(0, 4),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      CircleAvatar(
                        radius: 26,
                        backgroundColor: auth.isAdmin
                            ? const Color(0xFF18181B)
                            : const Color(0xFFF4F4F5),
                        child: Text(
                          user != null && user.adSoyad.isNotEmpty
                              ? user.adSoyad.substring(0, 1).toUpperCase()
                              : 'B',
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w700,
                            color: auth.isAdmin
                                ? Colors.white
                                : const Color(0xFF18181B),
                          ),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              user?.adSoyad ?? 'Profil bilgisi yüklenemedi',
                              style: const TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w700,
                                color: Color(0xFF18181B),
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              auth.isAdmin ? 'Yönetici' : 'Öğrenci',
                              style: const TextStyle(
                                fontSize: 12,
                                color: Color(0xFF71717A),
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                            const SizedBox(height: 6),
                            StatusBadge.rol(
                              rol: user?.rol ?? KullaniciRolu.ogrenci,
                            ),
                          ],
                        ),
                      ),
                      IconButton(
                        tooltip: 'Profili Düzenle',
                        icon: const Icon(Icons.edit_outlined,
                            size: 20, color: Color(0xFF52525B)),
                        onPressed:
                            auth.isLoading ? null : () => _editProfile(context),
                      ),
                    ],
                  ),

                  // Bölüm ve Sınıf Detay Alanı
                  if (user != null) ...[
                    const SizedBox(height: 14),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 10),
                      decoration: BoxDecoration(
                        color: const Color(0xFFF8FAFC),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: const Color(0xFFE2E8F0)),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: Row(
                              children: [
                                const Icon(Icons.school_outlined,
                                    size: 16, color: Color(0xFF64748B)),
                                const SizedBox(width: 6),
                                Expanded(
                                  child: Text(
                                    user.bolum ?? 'Bilgisayar Mühendisliği',
                                    style: const TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                      color: Color(0xFF334155),
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Container(
                            width: 1,
                            height: 16,
                            color: const Color(0xFFCBD5E1),
                            margin: const EdgeInsets.symmetric(horizontal: 8),
                          ),
                          Row(
                            children: [
                              const Icon(Icons.badge_outlined,
                                  size: 16, color: Color(0xFF64748B)),
                              const SizedBox(width: 6),
                              Text(
                                user.sinif ?? '4. Sınıf',
                                style: const TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: Color(0xFF334155),
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 12),

            // Yönetici Paneli Erişimi (Yalnızca Yönetici Görür)
            if (auth.isAdmin)
              Container(
                margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                decoration: BoxDecoration(
                  color: const Color(0xFF09090B),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: ListTile(
                  leading: const Icon(Icons.shield, color: Colors.white),
                  title: const Text(
                    'Yönetici Paneli',
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w700,
                      fontSize: 14,
                    ),
                  ),
                  subtitle: const Text(
                    'Şikâyetler ve kategori denetimleri',
                    style: TextStyle(color: Color(0xFFA1A1AA), fontSize: 12),
                  ),
                  trailing: const Icon(Icons.arrow_forward_ios,
                      size: 14, color: Colors.white),
                  onTap: () {
                    Navigator.pushNamed(context, AppRoutes.adminPanel);
                  },
                ),
              ),

            if (auth.email != null)
              Padding(
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                child: Text(
                  auth.email!,
                  style:
                      const TextStyle(fontSize: 13, color: Color(0xFF71717A)),
                ),
              ),

            if (auth.message != null)
              Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  auth.message!,
                  style:
                      const TextStyle(fontSize: 13, color: Color(0xFFDC2626)),
                ),
              ),

            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Wrap(
                spacing: 12,
                children: [
                  TextButton(
                    onPressed:
                        auth.isLoading ? null : () => _editProfile(context),
                    child: const Text('Profili düzenle'),
                  ),
                  if (user == null)
                    TextButton(
                      onPressed:
                          auth.isLoading ? null : () => auth.initialize(),
                      child: const Text('Tekrar dene'),
                    ),
                  TextButton(
                    onPressed: auth.isLoading ? null : () => _logout(context),
                    child: const Text('Çıkış yap'),
                  ),
                ],
              ),
            ),
            if (auth.isLoading) const LinearProgressIndicator(),

            const SizedBox(height: 12),

            // İlanlarım Başlığı
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'İlanlarım',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFF18181B),
                    ),
                  ),
                  Text(
                    '${userListings.length} İlan',
                    style: const TextStyle(
                      fontSize: 12,
                      color: Color(0xFF71717A),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 6),

            // İlan Listesi
            if (userListings.isEmpty)
              const EmptyState(
                icon: Icons.post_add_outlined,
                title: 'Henüz İlanınız Yok',
                description:
                    'Oluşturduğunuz ilanlar bu bölümde listelenecektir.',
              )
            else
              ...userListings.map((item) {
                final isAttending = listingsProvider.isUserAttending(
                  item.id,
                  user?.id ?? '',
                );
                final count = listingsProvider.getAttendanceCount(item.id);

                return ListingCard(
                  ilan: item,
                  isAttending: isAttending,
                  attendanceCount: count,
                  onTap: () {
                    Navigator.pushNamed(
                      context,
                      AppRoutes.listingDetail,
                      arguments: item,
                    );
                  },
                );
              }),

            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
}

class _EditProfileResult {
  const _EditProfileResult({
    required this.name,
    required this.bolum,
    required this.sinif,
  });

  final String name;
  final String bolum;
  final String sinif;
}

class _EditProfileDialog extends StatefulWidget {
  const _EditProfileDialog({
    required this.initialName,
    required this.initialBolum,
    required this.initialSinif,
  });

  final String initialName;
  final String initialBolum;
  final String initialSinif;

  @override
  State<_EditProfileDialog> createState() => _EditProfileDialogState();
}

class _EditProfileDialogState extends State<_EditProfileDialog> {
  late final TextEditingController _nameController;
  late String _bolum;
  late String _sinif;

  static const List<String> _bolumler = [
    'Bilgisayar Mühendisliği',
    'Yazılım Mühendisliği',
    'Elektrik-Elektronik Mühendisliği',
    'Endüstri Mühendisliği',
    'Makine Mühendisliği',
    'Hukuk',
    'Tıp',
    'İletişim',
    'Genel',
  ];

  static const List<String> _siniflar = [
    'Hazırlık',
    '1. Sınıf',
    '2. Sınıf',
    '3. Sınıf',
    '4. Sınıf',
    'Yüksek Lisans / Doktora',
    'Mezun',
  ];

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.initialName);
    _bolum = _bolumler.contains(widget.initialBolum)
        ? widget.initialBolum
        : _bolumler.first;
    _sinif = _siniflar.contains(widget.initialSinif)
        ? widget.initialSinif
        : _siniflar[4]; // 4. Sınıf
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
        title: const Text('Profili düzenle'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              TextField(
                controller: _nameController,
                maxLength: 100,
                decoration: const InputDecoration(labelText: 'Ad soyad'),
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                isExpanded: true,
                initialValue: _bolum,
                decoration: const InputDecoration(labelText: 'Bölüm'),
                items: _bolumler
                    .map((b) => DropdownMenuItem(value: b, child: Text(b)))
                    .toList(),
                onChanged: (val) {
                  if (val != null) setState(() => _bolum = val);
                },
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                isExpanded: true,
                initialValue: _sinif,
                decoration: const InputDecoration(labelText: 'Sınıf'),
                items: _siniflar
                    .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                    .toList(),
                onChanged: (val) {
                  if (val != null) setState(() => _sinif = val);
                },
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Vazgeç'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(
              context,
              _EditProfileResult(
                name: _nameController.text,
                bolum: _bolum,
                sinif: _sinif,
              ),
            ),
            child: const Text('Kaydet'),
          ),
        ],
      );
}
