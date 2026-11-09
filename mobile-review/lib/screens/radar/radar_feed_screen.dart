import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../providers/auth_provider.dart';
import '../../providers/listings_provider.dart';
import '../../routes/app_routes.dart';
import '../../utils/listing_search_filter.dart';
import '../../widgets/category_chip.dart';
import '../../widgets/empty_state.dart';
import '../../widgets/listing_card.dart';
import '../../widgets/listing_search_bar.dart';
import '../../widgets/server_listing_results.dart';
import '../../services/listings_service.dart';
import '../../widgets/pagination_controls.dart';
import '../listings/listing_search_screen.dart';

/// Sosyal Radar Akış Ekranı (Kategori Filtreleme, Metin Arama ve Sayfalama)
class RadarFeedScreen extends StatefulWidget {
  const RadarFeedScreen({
    super.key,
    this.pageSize = ListingSearchFilter.defaultPageSize,
  });

  final int pageSize;

  @override
  State<RadarFeedScreen> createState() => _RadarFeedScreenState();
}

class _RadarFeedScreenState extends State<RadarFeedScreen> {
  final TextEditingController _searchController = TextEditingController();
  String? _selectedCategoryId;
  int _currentPage = 1;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  bool get _hasActiveFilters =>
      _searchController.text.trim().isNotEmpty || _selectedCategoryId != null;

  void _resetFilters() {
    setState(() {
      _searchController.clear();
      _selectedCategoryId = null;
      _currentPage = 1;
    });
  }

  @override
  Widget build(BuildContext context) {
    final listingsProvider = context.watch<ListingsProvider>();
    final auth = context.watch<AuthProvider>();
    final currentUserId = auth.currentUser?.id ?? '';

    final categories = listingsProvider.kategoriler
        .where((k) => k.tur == IlanTuru.sosyal)
        .toList();

    final activeCategoryId = categories.any((k) => k.id == _selectedCategoryId)
        ? _selectedCategoryId
        : null;

    return ServerListingResults(
      provider: listingsProvider,
      query: ListingQuery(
        tur: IlanTuru.sosyal,
        kategoriId: activeCategoryId,
        text: _searchController.text,
        page: _currentPage,
        pageSize: widget.pageSize,
        activeSocialOnly: true,
      ),
      onPageCorrected: (page) {
        if (mounted) {
          setState(() {
            _currentPage = page;
          });
        }
      },
      builder: (pagination, queryLoading, queryError, queryRefresh) {
        return Scaffold(
          body: RefreshIndicator(
            onRefresh: () async {
              await queryRefresh();
            },
            child: CustomScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverToBoxAdapter(
                  child: ListingQueryStatus(
                    loading: queryLoading || listingsProvider.isLoading,
                    error: queryError ?? listingsProvider.error,
                    onRetry: queryRefresh,
                  ),
                ),
                // Başlık ve Gelişmiş Arama Geçişi
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Sosyal Radar',
                                style: TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.w800,
                                  color: Color(0xFF18181B),
                                  letterSpacing: -0.4,
                                ),
                              ),
                              SizedBox(height: 2),
                              Text(
                                'Kampüsteki buluşma ve çalışma ilanları',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: Color(0xFF71717A),
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: 'Detaylı Arama',
                          icon: const Icon(
                            Icons.manage_search,
                            color: Color(0xFF18181B),
                          ),
                          onPressed: () {
                            Navigator.push(
                              context,
                              MaterialPageRoute<void>(
                                builder: (_) => ListingSearchScreen(
                                  initialTur: IlanTuru.sosyal,
                                  initialCategoryId: activeCategoryId,
                                  initialQuery: _searchController.text,
                                ),
                              ),
                            );
                          },
                        ),
                      ],
                    ),
                  ),
                ),

                // Metin Arama Çubuğu
                SliverToBoxAdapter(
                  child: ListingSearchBar(
                    controller: _searchController,
                    hintText:
                        'Sosyal ilanlarda başlık, açıklama veya yer ara...',
                    hasActiveFilters: _hasActiveFilters,
                    totalResults: pagination.totalItems,
                    onResetFilters: _resetFilters,
                    onChanged: (_) {
                      setState(() {
                        _currentPage = 1;
                      });
                    },
                    onClear: () {
                      setState(() {
                        _searchController.clear();
                        _currentPage = 1;
                      });
                    },
                  ),
                ),

                // Kategori Filtreleme Çipleri
                SliverToBoxAdapter(
                  child: SizedBox(
                    height: 48,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 8,
                      ),
                      children: [
                        CategoryChip(
                          label: 'Tümü',
                          isSelected: activeCategoryId == null,
                          onTap: () {
                            setState(() {
                              _selectedCategoryId = null;
                              _currentPage = 1;
                            });
                          },
                        ),
                        ...categories.map(
                          (cat) => CategoryChip(
                            label: cat.ad,
                            isSelected: activeCategoryId == cat.id,
                            onTap: () {
                              setState(() {
                                _selectedCategoryId = activeCategoryId == cat.id
                                    ? null
                                    : cat.id;
                                _currentPage = 1;
                              });
                            },
                          ),
                        ),
                      ],
                    ),
                  ),
                ),

                // İlan Listesi ve Sayfalama
                if (pagination.items.isEmpty &&
                    !queryLoading &&
                    queryError == null &&
                    !listingsProvider.isLoading &&
                    listingsProvider.error == null)
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: EmptyState(
                      icon: Icons.event_busy_outlined,
                      title: 'Buluşma Bulunamadı',
                      description: _hasActiveFilters
                          ? 'Arama veya kategori kriterlerine uygun buluşma bulunmuyor.'
                          : 'Seçilen kategoride henüz aktif bir buluşma bulunmuyor.',
                      actionLabel: _hasActiveFilters
                          ? 'Filtreleri Temizle'
                          : null,
                      onAction: _hasActiveFilters ? _resetFilters : null,
                    ),
                  )
                else ...[
                  SliverPadding(
                    padding: const EdgeInsets.only(bottom: 8, top: 4),
                    sliver: SliverList(
                      delegate: SliverChildBuilderDelegate((context, index) {
                        final item = pagination.items[index];
                        final isAttending = listingsProvider.isUserAttending(
                          item.id,
                          currentUserId,
                        );
                        final count = listingsProvider.getAttendanceCount(
                          item.id,
                        );

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
                      }, childCount: pagination.items.length),
                    ),
                  ),
                  SliverToBoxAdapter(
                    child: PaginationControls(
                      pagination: pagination,
                      onPageChanged: (newPage) {
                        setState(() {
                          _currentPage = newPage;
                        });
                      },
                    ),
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }
}
