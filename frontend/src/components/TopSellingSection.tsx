import React, { useState, useEffect, useRef } from 'react';
import { Flame, Sparkles, TrendingUp, ChevronLeft, ChevronRight, ShoppingCart, Plus, Minus, CheckCircle2, AlertTriangle, XCircle, Loader2 } from 'lucide-react';
import type { Product } from '../types';
import { useCart } from '../context/CartContext';
import { useAuth } from '../context/AuthContext';

interface TopSellingSectionProps {
  products: Product[];
  onOpenAuth: (tab?: 'login' | 'register') => void;
}

export const TopSellingSection: React.FC<TopSellingSectionProps> = ({ products, onOpenAuth }) => {
  const { user } = useAuth();
  const { cart, addToCart, updateQuantity, removeFromCart } = useCart();
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const [slideDirection, setSlideDirection] = useState<'next' | 'prev'>('next');
  const [loadingAction, setLoadingAction] = useState(false);
  const touchStartX = useRef<number | null>(null);

  // Take top 6 visible products
  const topProducts = products.filter(p => p.isVisible !== false).slice(0, 6);

  // Auto-play sliding every 4.5 seconds
  useEffect(() => {
    if (topProducts.length <= 1 || isPaused) return;

    const timer = setInterval(() => {
      setSlideDirection('next');
      setCurrentIndex((prev) => (prev + 1) % topProducts.length);
    }, 4500);

    return () => clearInterval(timer);
  }, [topProducts.length, isPaused]);

  if (topProducts.length === 0) return null;

  const currentProduct = topProducts[currentIndex];
  const itemInCart = cart?.items.find((item) => item.productId === currentProduct.id);
  const itemQty = itemInCart ? itemInCart.quantity : 0;
  const isOutOfStock = currentProduct.stockQuantity <= 0 || currentProduct.isOutOfStock;
  const isLowStock = !isOutOfStock && (currentProduct.stockQuantity <= currentProduct.lowStockThreshold || currentProduct.isLowStock);

  const handlePrev = () => {
    setSlideDirection('prev');
    setCurrentIndex((prev) => (prev === 0 ? topProducts.length - 1 : prev - 1));
  };

  const handleNext = () => {
    setSlideDirection('next');
    setCurrentIndex((prev) => (prev + 1) % topProducts.length);
  };

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX;
  };

  const handleTouchEnd = (e: React.TouchEvent) => {
    if (touchStartX.current === null) return;
    const touchEndX = e.changedTouches[0].clientX;
    const diff = touchStartX.current - touchEndX;

    if (diff > 50) {
      handleNext();
    } else if (diff < -50) {
      handlePrev();
    }
    touchStartX.current = null;
  };

  const handleAddToCart = async () => {
    if (!user) {
      onOpenAuth();
      return;
    }
    if (isOutOfStock || loadingAction) return;

    setLoadingAction(true);
    try {
      await addToCart(currentProduct.id, 1);
    } catch (err) {
      console.error('Failed to add to cart:', err);
    } finally {
      setLoadingAction(false);
    }
  };

  const handleUpdateQty = async (newQty: number) => {
    if (!user) {
      onOpenAuth();
      return;
    }
    if (loadingAction) return;

    setLoadingAction(true);
    try {
      if (newQty <= 0) {
        await removeFromCart(currentProduct.id);
      } else {
        await updateQuantity(currentProduct.id, newQty);
      }
    } catch (err) {
      console.error('Failed to update cart item:', err);
    } finally {
      setLoadingAction(false);
    }
  };

  const getImageUrl = (path?: string, category?: string, name?: string) => {
    if (path && path.trim() !== '') {
      if (path.startsWith('http://') || path.startsWith('https://')) {
        return path;
      }
      const apiBase = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';
      const cleanBase = apiBase.replace('/api', '');
      return `${cleanBase}${path.startsWith('/') ? '' : '/'}${path}`;
    }

    const cat = (category || '').toLowerCase();
    const n = (name || '').toLowerCase();

    if (cat.includes('notebook') || n.includes('notebook') || n.includes('journal')) {
      return 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=1200&auto=format&fit=crop&q=85';
    }
    if (cat.includes('writing') || n.includes('pen') || n.includes('marker') || n.includes('highlighter')) {
      return 'https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=1200&auto=format&fit=crop&q=85';
    }
    if (cat.includes('desk') || n.includes('pad') || n.includes('sticky')) {
      return 'https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=1200&auto=format&fit=crop&q=85';
    }
    if (cat.includes('school') || n.includes('ruler') || n.includes('geometry')) {
      return 'https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=1200&auto=format&fit=crop&q=85';
    }

    return 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=1200&auto=format&fit=crop&q=85';
  };

  return (
    <section 
      className="top-selling-unified-banner"
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
      onTouchStart={handleTouchStart}
      onTouchEnd={handleTouchEnd}
    >
      {/* 70% Left Column: Full Edge-to-Edge Product Image */}
      <div className="unified-image-panel">
        <div className="unified-rank-tag">
          <Sparkles size={13} /> #{currentIndex + 1} Best Seller
        </div>
        <img 
          key={currentProduct.id}
          src={getImageUrl(currentProduct.imagePath, currentProduct.category, currentProduct.name)} 
          alt={currentProduct.name} 
          className={`unified-hero-img slide-anim-${slideDirection}`}
          onError={(e) => {
            (e.target as HTMLImageElement).src = 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=1200&auto=format&fit=crop&q=85';
          }}
        />
        <div className="unified-img-overlay"></div>

        {/* Thumbnail Stream Strip for quick image switching */}
        {topProducts.length > 1 && (
          <div className="unified-thumbs-stream">
            {topProducts.map((p, idx) => (
              <button
                key={p.id}
                type="button"
                className={`unified-thumb-btn ${idx === currentIndex ? 'active' : ''}`}
                onClick={() => {
                  setSlideDirection(idx > currentIndex ? 'next' : 'prev');
                  setCurrentIndex(idx);
                }}
                title={p.name}
              >
                <img
                  src={getImageUrl(p.imagePath, p.category, p.name)}
                  alt={p.name}
                  className="unified-thumb-img"
                  onError={(e) => {
                    (e.target as HTMLImageElement).src = 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=150&auto=format&fit=crop';
                  }}
                />
              </button>
            ))}
          </div>
        )}
      </div>

      {/* 30% Right Column: Header, Info, Controls & Actions */}
      <div className="unified-details-panel">
        {/* Top Header Row */}
        <div className="unified-panel-header">
          <div className="unified-badge-pill">
            <Flame size={13} className="flame-icon" />
            <span>Studio Bestseller</span>
          </div>

          <div className="unified-nav-controls">
            <button 
              className="unified-nav-btn prev" 
              onClick={handlePrev}
              aria-label="Previous Featured Product"
              title="Previous"
            >
              <ChevronLeft size={16} />
            </button>
            <span className="unified-counter">
              <strong>0{currentIndex + 1}</strong>/0{topProducts.length}
            </span>
            <button 
              className="unified-nav-btn next" 
              onClick={handleNext}
              aria-label="Next Featured Product"
              title="Next"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>

        {/* Product Meta & Title */}
        <div className="unified-product-body">
          <div className="unified-meta-row">
            <span className="unified-category-chip">{currentProduct.category}</span>
            {isOutOfStock ? (
              <span className="unified-stock-chip out-of-stock">
                <XCircle size={12} /> Sold Out
              </span>
            ) : isLowStock ? (
              <span className="unified-stock-chip low-stock">
                <AlertTriangle size={12} /> Only {currentProduct.stockQuantity} Left
              </span>
            ) : (
              <span className="unified-stock-chip in-stock">
                <CheckCircle2 size={12} /> In Stock
              </span>
            )}
          </div>

          <h3 className="unified-product-title">{currentProduct.name}</h3>

          <p className="unified-product-desc">
            {currentProduct.description || 'Precision handcrafted stationery crafted from archival grade materials for seamless writing and desk productivity.'}
          </p>

          <div className="unified-pricing-box">
            <div className="unified-price-stack">
              <span className="unified-price-label">Price</span>
              <span className="unified-price-num">Rs. {currentProduct.price.toFixed(2)}</span>
            </div>
            <div className="unified-top-pick-tag">
              <TrendingUp size={13} /> Top Pick
            </div>
          </div>
        </div>

        {/* Action Button & Stepper */}
        <div className="unified-action-container">
          {itemQty > 0 ? (
            <div className="unified-stepper-box">
              <button
                className="unified-stepper-btn minus"
                onClick={() => handleUpdateQty(itemQty - 1)}
                disabled={loadingAction}
                aria-label="Decrease Quantity"
              >
                <Minus size={15} />
              </button>
              <span className="unified-stepper-val">
                {loadingAction ? <Loader2 size={15} className="spin-icon" /> : itemQty}
              </span>
              <button
                className="unified-stepper-btn plus"
                onClick={() => handleUpdateQty(itemQty + 1)}
                disabled={loadingAction || itemQty >= currentProduct.stockQuantity}
                aria-label="Increase Quantity"
              >
                <Plus size={15} />
              </button>
              <span className="unified-in-cart-text">In Bag</span>
            </div>
          ) : (
            <button
              className="btn-unified-add-cart"
              onClick={handleAddToCart}
              disabled={isOutOfStock || loadingAction}
            >
              {loadingAction ? (
                <>
                  <Loader2 size={16} className="spin-icon" />
                  <span>Adding...</span>
                </>
              ) : isOutOfStock ? (
                <>
                  <XCircle size={16} />
                  <span>Sold Out</span>
                </>
              ) : (
                <>
                  <ShoppingCart size={16} />
                  <span>Add to Bag • Rs. {currentProduct.price.toFixed(2)}</span>
                </>
              )}
            </button>
          )}

          {/* Indicator Dots */}
          <div className="unified-dots-row">
            {topProducts.map((_, dotIdx) => (
              <button
                key={dotIdx}
                className={`unified-dot ${dotIdx === currentIndex ? 'active' : ''}`}
                onClick={() => {
                  setSlideDirection(dotIdx > currentIndex ? 'next' : 'prev');
                  setCurrentIndex(dotIdx);
                }}
                aria-label={`Go to slide ${dotIdx + 1}`}
              />
            ))}
          </div>
        </div>
      </div>
    </section>
  );
};
