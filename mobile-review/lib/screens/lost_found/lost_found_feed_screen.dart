import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
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

/// Kayıp & Buluntu Akış Ekranı (Tür/Kategori Filtreleme, Metin Arama ve Sayfalama)
class LostFoundFeedScreen extends StatefulWidget {
  const LostFoundFeedScreen({
    super.key,
    this.pageSize = ListingSearchFilter.defaultPageSize,
  });

  final int pageSize;

  @override
  State<LostFoundFeedScreen> createState() => _LostFoundFeedScreenState();
}

class _LostFoundFeedScreenState extends State<LostFoundFeedScreen> {
  final TextEditingController _searchController = TextEditingController();
  IlanTuru? _filterTur;
  String? _selectedCategoryId;
  int _currentPage = 1;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  bool get _hasActiveFilters =>
      _searchController.text.trim().isNotEmpty ||
      _filterTur != null ||
      _selectedCategoryId != null;

  void _resetFilters() {
    setState(() {
      _searchController.clear();
      _filterTur = null;
      _selectedCategoryId = null;
      _currentPage = 1;
    });
  }

  void _selectTur(IlanTuru? tur, List<Kategori> allCategories) {
    setState(() {
      _filterTur = tur;
      if (tur != null && _selectedCategoryId != null) {
        final stillValid = allCategories.any(
          (k) => k.id == _selectedCategoryId && k.tur == tur,
        );
        if (!stillValid) {
          _selectedCategoryId = null;
        }
      }
      _currentPage = 1;
    });
  }

  @override
  Widget build(BuildContext context) {
    final listingsProvider = context.watch<ListingsProvider>();

    final categories = listingsProvider.kategoriler.where((k) {
      final isLostFound = k.tur == IlanTuru.kayip || k.tur == IlanTuru.buluntu;
      return isLostFound && (_filterTur == null || k.tur == _filterTur);
    }).toList();

    final activeCategoryId = categories.any((k) => k.id == _selectedCategoryId)
        ? _selectedCategoryId
        : null;

    return ServerListingResults(
      provider: listingsProvider,
      query: ListingQuery(
        tur: _filterTur,
        kategoriId: activeCategoryId,
        text: _searchController.text,
        page: _currentPage,
        pageSize: widget.pageSize,
        lostFoundOnly: true,
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
                // Başlık ve Detaylı Arama Geçişi
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
                                'Kayıp & Buluntu',
                                style: TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.w800,
                                  color: Color(0xFF18181B),
                                  letterSpacing: -0.4,
                                ),
                              ),
                              SizedBox(height: 2),
                              Text(
                                'Kayıp ve bulunan eşya bildirimleri',
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
                                  initialTur: _filterTur,
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
                    hintText: 'Kayıp veya buluntu eşya, konum ara...',
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

                // Sade Segment Seçici
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 4,
                    ),
                    child: Container(
                      decoration: BoxDecoration(
                        color: const Color(0xFFF4F4F5),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      padding: const EdgeInsets.all(3),
                      child: Row(
                        children: [
                          Expanded(
                            child: _SimpleSegment(
                              label: 'Tümü',
                              isSelected: _filterTur == null,
                              onTap: () => _selectTur(
                                null,
                                listingsProvider.kategoriler,
                              ),
                            ),
                          ),
                          Expanded(
                            child: _SimpleSegment(
                              label: 'Kayıp',
                              isSelected: _filterTur == IlanTuru.kayip,
                              onTap: () => _selectTur(
                                IlanTuru.kayip,
                                listingsProvider.kategoriler,
                              ),
                            ),
                          ),
                          Expanded(
                            child: _SimpleSegment(
                              label: 'Bulunan',
                              isSelected: _filterTur == IlanTuru.buluntu,
                              onTap: () => _selectTur(
                                IlanTuru.buluntu,
                                listingsProvider.kategoriler,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),

                // Kategori Çipleri
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
                          onTap: () => setState(() {
                            _selectedCategoryId = null;
                            _currentPage = 1;
                          }),
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

                // Liste ve Sayfalama
                if (pagination.items.isEmpty &&
                    !queryLoading &&
                    queryError == null &&
                    !listingsProvider.isLoading &&
                    listingsProvider.error == null)
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: EmptyState(
                      icon: Icons.search_off_outlined,
                      title: 'İlan Bulunamadı',
                      description: 'Kriterlere uygun ilan bulunmuyor.',
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
                        return ListingCard(
                          ilan: item,
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

class _SimpleSegment extends StatelessWidget {
  const _SimpleSegment({
    required this.label,
    required this.isSelected,
    required this.onTap,
  });

  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 8),
        decoration: BoxDecoration(
          color: isSelected ? Colors.white : Colors.transparent,
          borderRadius: BorderRadius.circular(6),
        ),
        child: Center(
          child: Text(
            label,
            style: TextStyle(
              fontSize: 12,
              fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
              color: isSelected ? Colors.black : const Color(0xFF71717A),
            ),
          ),
        ),
      ),
    );
  }
}
