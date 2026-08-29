import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Search,
  RefreshCw,
  ChevronLeft,
  ChevronRight,
  Package,
  AlertCircle,
  ArrowUpDown,
  X
} from 'lucide-react';
import type { Product } from '../types';
import { api } from '../services/api';
import { ProductCard } from '../components/ProductCard';
import { TopSellingSection } from '../components/TopSellingSection';
import { subscribeToStockEvents } from '../services/sse';

interface CatalogPageProps {
  onOpenAuth: (tab?: 'login' | 'register') => void;
  onOpenMyOrders?: () => void;
}

export const CatalogPage: React.FC<CatalogPageProps> = ({ onOpenAuth }) => {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const stockFilter = 'available';
  const [sortBy, setSortBy] = useState<'default' | 'price-asc' | 'price-desc' | 'name-asc' | 'name-desc'>('default');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalProducts, setTotalProducts] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // 350ms search debounce timer
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchInput.trim());
      setPage(1);
    }, 350);

    return () => clearTimeout(handler);
  }, [searchInput]);

  const normalizeCat = (c: string) => {
    if (!c) return 'Stationery';
    const lower = c.trim().toLowerCase();
    if (lower.includes('writ') || lower.includes('pen') || lower.includes('ink')) return 'Writing';
    if (lower.includes('note') || lower.includes('journal') || lower.includes('paper')) return 'Notebooks';
    if (lower.includes('desk') || lower.includes('mat') || lower.includes('organizer') || lower.includes('sticky')) return 'Desk Accessories';
    if (lower.includes('art') || lower.includes('paint') || lower.includes('sketch')) return 'Art Supplies';
    if (lower.includes('office') || lower.includes('tape') || lower.includes('stapler')) return 'Office Supplies';
    if (lower.includes('school') || lower.includes('draft') || lower.includes('ruler')) return 'School & Drafting';
    return c.trim().charAt(0).toUpperCase() + c.trim().slice(1);
  };

  const loadCategories = async () => {
    try {
      const cats = await api.getCategories();
      const cleanCats = Array.from(new Set((cats || []).map(normalizeCat))).sort();
      setCategories(cleanCats.length > 0 ? cleanCats : ['Art Supplies', 'Desk Accessories', 'Notebooks', 'Office Supplies', 'School & Drafting', 'Writing']);
    } catch {
      setCategories(['Art Supplies', 'Desk Accessories', 'Notebooks', 'Office Supplies', 'School & Drafting', 'Writing']);
    }
  };

  const loadProducts = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const res = await api.getProducts({
        search: debouncedSearch,
        category: selectedCategory,
        stockFilter,
        page,
        pageSize: 12,
      });
      setProducts(res.products || []);
      setTotalPages(res.totalPages || 1);
      setTotalProducts(res.totalProducts || 0);
    } catch (err: any) {
      setError(err.message || 'Failed to load products.');
    } finally {
      setLoading(false);
    }
  }, [debouncedSearch, selectedCategory, stockFilter, page]);

  useEffect(() => {
    loadCategories();
  }, []);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  // Real-time Server-Sent Events (SSE) listener for stock updates
  useEffect(() => {
    const unsubscribe = subscribeToStockEvents(() => {
      loadProducts();
    });
    return () => unsubscribe();
  }, [loadProducts]);

  // Client-side sorting
  const sortedProducts = useMemo(() => {
    const list = [...products];
    if (sortBy === 'price-asc') list.sort((a, b) => a.price - b.price);
    else if (sortBy === 'price-desc') list.sort((a, b) => b.price - a.price);
    else if (sortBy === 'name-asc') list.sort((a, b) => a.name.localeCompare(b.name));
    else if (sortBy === 'name-desc') list.sort((a, b) => b.name.localeCompare(a.name));
    return list;
  }, [products, sortBy]);

  const hasActiveFilters = Boolean(debouncedSearch || selectedCategory || sortBy !== 'default');

  const handleResetFilters = () => {
    setSearchInput('');
    setDebouncedSearch('');
    setSelectedCategory('');
    setSortBy('default');
    setPage(1);
  };

  return (
    <div className="catalog-page">
      {/* Static Top Selling Products Showcase (NO SLIDING IMAGES) */}
      {!debouncedSearch && !selectedCategory && page === 1 && (
        <TopSellingSection products={products} onOpenAuth={onOpenAuth} />
      )}

      {/* Main Filter & Navigation Section */}
      <div className="catalog-control-panel">
        <div className="filter-bar">
          {/* Search Input with Clear Button */}
          <div className="search-input-wrapper">
            <Search size={18} className="search-icon" />
            <input
              type="text"
              className="search-input"
              placeholder="Search stationery by title, item or category..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              aria-label="Search stationery"
            />
            {searchInput && (
              <button
                type="button"
                className="search-clear-btn"
                onClick={() => {
                  setSearchInput('');
                  setDebouncedSearch('');
                  setPage(1);
                }}
                title="Clear search"
                aria-label="Clear search"
              >
                <X size={15} />
              </button>
            )}
          </div>

          {/* Controls Group */}
          <div className="filter-dropdowns-group">
            {/* Sort Selector */}
            <div className="filter-select-group">
              <ArrowUpDown size={15} className="filter-icon" />
              <select
                className="filter-select"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as any)}
                aria-label="Sort products"
              >
                <option value="default">Featured / Curated</option>
                <option value="price-asc">Price: Low to High</option>
                <option value="price-desc">Price: High to Low</option>
                <option value="name-asc">Name: A to Z</option>
                <option value="name-desc">Name: Z to A</option>
              </select>
            </div>

            <button
              className="btn btn-secondary btn-icon-only refresh-catalog-btn"
              onClick={() => loadProducts()}
              title="Refresh Catalog"
              aria-label="Refresh Catalog"
            >
              <RefreshCw size={16} />
            </button>
          </div>
        </div>

        {/* Category Pills Bar */}
        <div className="category-pills-bar">
          <button
            className={`category-pill ${selectedCategory === '' ? 'active' : ''}`}
            onClick={() => {
              setSelectedCategory('');
              setPage(1);
            }}
          >
            <span>All Stationery</span>
          </button>
          {categories.map((cat) => (
            <button
              key={cat}
              className={`category-pill ${selectedCategory === cat ? 'active' : ''}`}
              onClick={() => {
                setSelectedCategory(cat);
                setPage(1);
              }}
            >
              <span>{cat}</span>
            </button>
          ))}
        </div>

        {/* Active Filter Chips & Results Counter Bar */}
        <div className="catalog-meta-bar">
          <div className="meta-left">
            <span className="results-count-text">
              Showing <strong>{sortedProducts.length}</strong> of <strong>{totalProducts}</strong> stationery items
            </span>

            {hasActiveFilters && (
              <div className="active-filter-chips">
                {debouncedSearch && (
                  <span className="filter-chip">
                    Search: "{debouncedSearch}"
                    <button
                      type="button"
                      onClick={() => {
                        setSearchInput('');
                        setDebouncedSearch('');
                      }}
                      aria-label="Clear search filter"
                    >
                      <X size={12} />
                    </button>
                  </span>
                )}
                {selectedCategory && (
                  <span className="filter-chip">
                    Category: {selectedCategory}
                    <button type="button" onClick={() => setSelectedCategory('')}><X size={12} /></button>
                  </span>
                )}
                {sortBy !== 'default' && (
                  <span className="filter-chip">
                    Sort: {sortBy.replace('-', ' ')}
                    <button type="button" onClick={() => setSortBy('default')}><X size={12} /></button>
                  </span>
                )}
                <button
                  type="button"
                  className="clear-all-filters-btn"
                  onClick={handleResetFilters}
                >
                  Clear All
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Error State */}
      {error && (
        <div className="alert-box alert-error">
          <AlertCircle size={20} />
          <span>{error}</span>
        </div>
      )}

      {/* Main Grid View */}
      {loading ? (
        <div className="skeleton-grid">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="skeleton-card">
              <div className="skeleton-thumb"></div>
              <div className="skeleton-line short"></div>
              <div className="skeleton-line title"></div>
              <div className="skeleton-line price"></div>
            </div>
          ))}
        </div>
      ) : sortedProducts.length === 0 ? (
        <div className="empty-catalog-state">
          <Package size={64} className="empty-icon" />
          <h3>No Stationery Products Found</h3>
          <p>We couldn't find any products matching your current filters.</p>
          <button type="button" className="btn btn-primary btn-sm mt-4" onClick={handleResetFilters}>
            Reset All Filters
          </button>
        </div>
      ) : (
        <>
          <div className="products-grid">
            {sortedProducts.map((product) => (
              <ProductCard key={product.id} product={product} onOpenAuth={onOpenAuth} />
            ))}
          </div>

          {/* Pagination Footer */}
          {totalPages > 1 && (
            <div className="pagination-bar">
              <button
                className="pagination-btn"
                disabled={page <= 1}
                onClick={() => {
                  setPage((p) => Math.max(1, p - 1));
                  window.scrollTo({ top: 400, behavior: 'smooth' });
                }}
              >
                <ChevronLeft size={18} />
                <span>Previous</span>
              </button>

              <div className="page-indicators-group">
                <span className="page-indicator">
                  Page <strong>{page}</strong> of <strong>{totalPages}</strong>
                </span>
              </div>

              <button
                className="pagination-btn"
                disabled={page >= totalPages}
                onClick={() => {
                  setPage((p) => Math.min(totalPages, p + 1));
                  window.scrollTo({ top: 400, behavior: 'smooth' });
                }}
              >
                <span>Next</span>
                <ChevronRight size={18} />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};
