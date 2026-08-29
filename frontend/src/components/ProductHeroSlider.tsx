import React, { useState, useEffect, useMemo } from 'react';
import {
  Sparkles,
  ShoppingBag,
  Flame,
  Plus,
  Check,
  Truck,
  ShieldCheck,
  Award,
  ChevronLeft,
  ChevronRight,
  ArrowRight
} from 'lucide-react';
import type { Product } from '../types';
import { useCart } from '../context/CartContext';
import { useAuth } from '../context/AuthContext';
import { api } from '../services/api';

interface ProductHeroSliderProps {
  products?: Product[];
  onOpenAuth?: (tab?: 'login' | 'register') => void;
}

export const ProductHeroSlider: React.FC<ProductHeroSliderProps> = ({ products, onOpenAuth }) => {
  const { user } = useAuth();
  const { cart, addToCart } = useCart();
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [featuredItems, setFeaturedItems] = useState<Product[]>([]);
  const [loading, setLoading] = useState(false);
  const [adding, setAdding] = useState(false);

  // Fetch top products directly from database endpoint
  useEffect(() => {
    let isMounted = true;
    const fetchTopFromDb = async () => {
      setLoading(true);
      try {
        const data = await api.getTopProducts(6);
        if (isMounted) {
          if (data && data.length > 0) {
            setFeaturedItems(data);
          } else if (products && products.length > 0) {
            setFeaturedItems(products.slice(0, 6));
          }
        }
      } catch {
        if (isMounted && products && products.length > 0) {
          setFeaturedItems(products.slice(0, 6));
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    };

    fetchTopFromDb();
    return () => {
      isMounted = false;
    };
  }, [products]);

  const rawItems = featuredItems.length > 0 ? featuredItems : products?.slice(0, 6) || [];

  // Deduplicate and clean product names
  const items = useMemo(() => {
    const seenNames = new Set<string>();
    const cleaned: Product[] = [];

    for (const p of rawItems) {
      const cleanName = (p.name || '').replace(/^["']|["']$/g, '').trim();
      if (!seenNames.has(cleanName.toLowerCase())) {
        seenNames.add(cleanName.toLowerCase());
        cleaned.push({
          ...p,
          name: cleanName,
          category: (p.category || 'Stationery').replace(/^["']|["']$/g, '').trim(),
        });
      }
    }
    return cleaned.slice(0, 4);
  }, [rawItems]);

  if (loading && items.length === 0) {
    return (
      <div className="hero-banner-skeleton">
        <div className="skeleton-hero-left">
          <div className="skeleton-pill" />
          <div className="skeleton-title" />
          <div className="skeleton-desc" />
        </div>
        <div className="skeleton-hero-right" />
      </div>
    );
  }

  if (items.length === 0) {
    return null;
  }

  const currentProduct = items[selectedIndex] || items[0];
  const cartItem = cart?.items?.find((item) => item.productId === currentProduct.id);
  const inCartQuantity = cartItem ? cartItem.quantity : 0;

  const getImageUrl = (path?: string, category?: string, name?: string) => {
    if (path && path.trim() !== '') {
      if (path.startsWith('http://') || path.startsWith('https://')) return path;
      const apiBase = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';
      const cleanBase = apiBase.replace('/api', '');
      return `${cleanBase}${path.startsWith('/') ? '' : '/'}${path}`;
    }
    const cat = (category || '').toLowerCase();
    const n = (name || '').toLowerCase();
    if (cat.includes('notebook') || n.includes('notebook') || n.includes('journal')) {
      return 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600&auto=format&fit=crop';
    }
    if (cat.includes('writing') || n.includes('pen') || n.includes('marker')) {
      return 'https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=600&auto=format&fit=crop';
    }
    return 'https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600&auto=format&fit=crop';
  };

  const productImage = getImageUrl(currentProduct.imagePath, currentProduct.category, currentProduct.name);

  const handleAddToCart = async () => {
    if (!user) {
      onOpenAuth?.('login');
      return;
    }
    if (currentProduct.stockQuantity <= 0 || adding) return;

    setAdding(true);
    try {
      await addToCart(currentProduct.id, 1);
    } catch (err) {
      console.error('Failed to add spotlight item to cart:', err);
    } finally {
      setAdding(false);
    }
  };

  return (
    <div className="hero-executive-showcase">
      {/* Background Ambience Layers */}
      <div className="hero-ambient-glow" />

      {/* Left Column: Brand Story & Value Proposition */}
      <div className="hero-showcase-left">
        <div className="hero-badge-container">
          <span className="hero-crown-pill">
            <Award size={13} className="crown-icon" /> Lumina Signature Collection
          </span>
          <span className="hero-stock-status-pill">
            <Sparkles size={12} /> Curated Studio Edition
          </span>
        </div>

        <h1 className="hero-headline">
          Executive Artisan Stationery & Workspace Elegance
        </h1>

        <p className="hero-description">
          Elevate your daily productivity with precision writing instruments, archival-grade notebooks, and handcrafted desk essentials.
        </p>

        {/* Value Highlights */}
        <div className="hero-perks-row">
          <div className="hero-perk-item">
            <Truck size={14} className="perk-icon" />
            <span>Free Desk Delivery</span>
          </div>
          <div className="hero-perk-item">
            <ShieldCheck size={14} className="perk-icon" />
            <span>100% Quality Inspected</span>
          </div>
          <div className="hero-perk-item">
            <Flame size={14} className="perk-icon" />
            <span>Instant Dispatch</span>
          </div>
        </div>

        {/* Featured Selector Chips */}
        {items.length > 1 && (
          <div className="hero-selector-chips-box">
            <span className="selector-label">Featured Spotlight Selection:</span>
            <div className="chips-list">
              {items.map((item, idx) => (
                <button
                  key={item.id}
                  type="button"
                  className={`hero-select-chip ${selectedIndex === idx ? 'active' : ''}`}
                  onClick={() => setSelectedIndex(idx)}
                >
                  <span className="chip-name">{item.name}</span>
                  <span className="chip-price">Rs. {Number(item.price).toFixed(2)}</span>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Right Column: Featured Product Spotlight Card */}
      <div className="hero-showcase-right">
        <div className="spotlight-card">
          {/* Spotlight Image Box */}
          <div className="spotlight-image-container">
            <img
              src={productImage}
              alt={currentProduct.name}
              className="spotlight-img"
              onError={(e) => {
                (e.target as HTMLImageElement).src =
                  'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600&auto=format&fit=crop';
              }}
            />
            <div className="spotlight-badge-overlay">
              <span className="spotlight-tag">
                <Flame size={12} /> Spotlight Choice
              </span>
              {currentProduct.stockQuantity <= 0 ? (
                <span className="badge badge-danger">Out of Stock</span>
              ) : (
                <span className="badge badge-success">In Stock</span>
              )}
            </div>
          </div>

          {/* Spotlight Details */}
          <div className="spotlight-body">
            <div className="spotlight-cat-tag">{currentProduct.category || 'Executive Stationery'}</div>
            <h3 className="spotlight-product-title" title={currentProduct.name}>
              {currentProduct.name}
            </h3>

            <div className="spotlight-footer">
              <div className="spotlight-price-box">
                <span className="spotlight-currency">Rs.</span>
                <span className="spotlight-amount">{Number(currentProduct.price).toFixed(2)}</span>
              </div>

              <button
                type="button"
                className={`btn btn-primary spotlight-cart-btn ${currentProduct.stockQuantity <= 0 ? 'disabled' : ''}`}
                onClick={handleAddToCart}
                disabled={currentProduct.stockQuantity <= 0 || adding}
              >
                {inCartQuantity > 0 ? (
                  <>
                    <Check size={15} />
                    <span>In Cart ({inCartQuantity})</span>
                  </>
                ) : (
                  <>
                    <ShoppingBag size={15} />
                    <span>{adding ? 'Adding...' : 'Add to Cart'}</span>
                  </>
                )}
              </button>
            </div>
          </div>

          {/* Next / Prev Subtle Navigation */}
          {items.length > 1 && (
            <div className="spotlight-nav-row">
              <button
                type="button"
                className="spotlight-arrow-btn"
                onClick={() => setSelectedIndex((prev) => (prev === 0 ? items.length - 1 : prev - 1))}
                aria-label="Previous item"
              >
                <ChevronLeft size={15} />
              </button>
              <span className="spotlight-counter">
                {selectedIndex + 1} of {items.length}
              </span>
              <button
                type="button"
                className="spotlight-arrow-btn"
                onClick={() => setSelectedIndex((prev) => (prev + 1) % items.length)}
                aria-label="Next item"
              >
                <ChevronRight size={15} />
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
