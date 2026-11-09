import 'package:flutter/material.dart';
import '../models/models.dart';

/// Sadeleştirilmiş Minimalist Rozet (Tür, Durum ve Rol Desteği)
class StatusBadge extends StatelessWidget {
  const StatusBadge.tur({
    super.key,
    required this.tur,
  })  : durum = null,
        rol = null,
        customLabel = null,
        customBg = null,
        customTextColor = null,
        isTur = true;

  const StatusBadge.durum({
    super.key,
    required this.durum,
  })  : tur = null,
        rol = null,
        customLabel = null,
        customBg = null,
        customTextColor = null,
        isTur = false;

  const StatusBadge.rol({
    super.key,
    required this.rol,
  })  : tur = null,
        durum = null,
        customLabel = null,
        customBg = null,
        customTextColor = null,
        isTur = false;

  const StatusBadge.custom({
    super.key,
    required this.customLabel,
    this.customBg,
    this.customTextColor,
  })  : tur = null,
        durum = null,
        rol = null,
        isTur = false;

  final IlanTuru? tur;
  final IlanDurumu? durum;
  final KullaniciRolu? rol;
  final String? customLabel;
  final Color? customBg;
  final Color? customTextColor;
  final bool isTur;

  @override
  Widget build(BuildContext context) {
    if (customLabel != null) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: customBg ?? const Color(0xFFF4F4F5),
          borderRadius: BorderRadius.circular(6),
        ),
        child: Text(
          customLabel!,
          style: TextStyle(
            color: customTextColor ?? const Color(0xFF18181B),
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
      );
    }

    if (rol != null) {
      final isAdmin = rol == KullaniciRolu.yonetici;
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: isAdmin ? const Color(0xFF18181B) : const Color(0xFFEFF6FF),
          borderRadius: BorderRadius.circular(6),
        ),
        child: Text(
          isAdmin ? 'Yönetici' : 'Öğrenci',
          style: TextStyle(
            color: isAdmin ? Colors.white : const Color(0xFF2563EB),
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
      );
    }

    if (isTur && tur != null) {
      String label;
      switch (tur!) {
        case IlanTuru.sosyal:
          label = 'Sosyal';
          break;
        case IlanTuru.kayip:
          label = 'Kayıp';
          break;
        case IlanTuru.buluntu:
          label = 'Buluntu';
          break;
      }

      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: const Color(0xFFF4F4F5),
          borderRadius: BorderRadius.circular(6),
          border: Border.all(color: const Color(0xFFE4E4E7), width: 1),
        ),
        child: Text(
          label,
          style: const TextStyle(
            color: Color(0xFF18181B),
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
      );
    }

    if (durum != null) {
      String label;
      switch (durum!) {
        case IlanDurumu.aktif:
          label = 'Aktif';
          break;
        case IlanDurumu.cozuldu:
          label = 'Çözüldü';
          break;
        case IlanDurumu.gizli:
          label = 'Gizli';
          break;
      }

      final isResolved = durum == IlanDurumu.cozuldu;

      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: isResolved ? const Color(0xFFF4F4F5) : Colors.black,
          borderRadius: BorderRadius.circular(6),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: isResolved ? const Color(0xFF71717A) : Colors.white,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
      );
    }

    return const SizedBox.shrink();
  }
}
