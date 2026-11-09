import 'package:flutter/material.dart';
import '../utils/listing_search_filter.dart';

/// İlan listeleri için sayfalama (Önceki / Sonraki ve sayfa durumu) kontrol çubuğu.
class PaginationControls extends StatelessWidget {
  const PaginationControls({
    super.key,
    required this.pagination,
    required this.onPageChanged,
  });

  final ListingPaginationResult pagination;
  final ValueChanged<int> onPageChanged;

  @override
  Widget build(BuildContext context) {
    if (pagination.totalItems == 0 || pagination.totalPages <= 1) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          color: const Color(0xFFF8FAFC),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: const Color(0xFFE4E4E7)),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            OutlinedButton.icon(
              onPressed: pagination.hasPrevious
                  ? () => onPageChanged(pagination.currentPage - 1)
                  : null,
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 6,
                ),
                minimumSize: Size.zero,
                tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              icon: const Icon(Icons.chevron_left, size: 16),
              label: const Text(
                'Önceki',
                style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
              ),
            ),
            Flexible(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      'Sayfa ${pagination.currentPage} / ${pagination.totalPages}',
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: Color(0xFF18181B),
                      ),
                      textAlign: TextAlign.center,
                    ),
                    Text(
                      '${pagination.startItem}-${pagination.endItem} / ${pagination.totalItems} ilan',
                      style: const TextStyle(
                        fontSize: 11,
                        color: Color(0xFF71717A),
                      ),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
            ),
            OutlinedButton.icon(
              onPressed: pagination.hasNext
                  ? () => onPageChanged(pagination.currentPage + 1)
                  : null,
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 6,
                ),
                minimumSize: Size.zero,
                tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              icon: const Icon(Icons.chevron_right, size: 16),
              label: const Text(
                'Sonraki',
                style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

