import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/models.dart';
import 'status_badge.dart';

/// Sadeleştirilmiş Minimalist İlan Kartı
class ListingCard extends StatelessWidget {
  const ListingCard({
    super.key,
    required this.ilan,
    required this.onTap,
    this.attendanceCount = 0,
    this.isAttending = false,
  });

  final Ilan ilan;
  final VoidCallback onTap;
  final int attendanceCount;
  final bool isAttending;

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('dd.MM.yyyy, HH:mm');

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Rozetler
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  StatusBadge.tur(tur: ilan.tur),
                  StatusBadge.durum(durum: ilan.durum),
                ],
              ),
              const SizedBox(height: 10),

              // İlan Başlığı
              Text(
                ilan.baslik,
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF18181B),
                  height: 1.3,
                ),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 6),

              // Açıklama Özeti
              Text(
                ilan.aciklama,
                style: const TextStyle(
                  fontSize: 13,
                  color: Color(0xFF71717A),
                  height: 1.4,
                ),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 4),

              const Divider(height: 16, color: Color(0xFFF4F4F5)),

              // Alt Bilgi
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  if (ilan.tur == IlanTuru.sosyal && ilan.bulusmaTarihi != null) ...[
                    Expanded(
                      child: Row(
                        children: [
                          const Icon(
                            Icons.schedule_outlined,
                            size: 14,
                            color: Color(0xFF71717A),
                          ),
                          const SizedBox(width: 4),
                          Flexible(
                            child: Text(
                              dateFormat.format(ilan.bulusmaTarihi!.toLocal()),
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w500,
                                color: Color(0xFF27272A),
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      '$attendanceCount katılımcı',
                      style: const TextStyle(
                        fontSize: 11,
                        color: Color(0xFF71717A),
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ] else ...[
                    Text(
                      dateFormat.format(ilan.olusturulmaTarihi.toLocal()),
                      style: const TextStyle(
                        fontSize: 11,
                        color: Color(0xFFA1A1AA),
                      ),
                    ),
                    const Row(
                      children: [
                        Text(
                          'Detay',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: Colors.black,
                          ),
                        ),
                        SizedBox(width: 2),
                        Icon(
                          Icons.chevron_right_rounded,
                          size: 16,
                          color: Colors.black,
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
