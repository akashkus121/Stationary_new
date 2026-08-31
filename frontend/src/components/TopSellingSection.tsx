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

  return (
    <section 
      className="top-selling-section"
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
      onTouchStart={handleTouchStart}
      onTouchEnd={handleTouchEnd}
    >
      {/* Header Bar with Title & Controls */}
      <div className="section-header-row">
        <div className="section-header-left">
          <div className="section-badge-pill">
            <Flame size={14} className="flame-icon" />
            <span>Studio Bestsellers</span>
          </div>
          <h2 className="section-heading">Top Selling Stationery</h2>
          <p className="section-subheading">
            Featured spotlight on our most sought-after precision instruments and archival essentials.
          </p>
        </div>

        <div className="section-header-right">
          <div className="slider-nav-arrows">
            <button 
              className="slider-nav-btn prev" 
              onClick={handlePrev}
              aria-label="Previous Featured Product"
              title="Previous"
            >
              <ChevronLeft size={18} />
            </button>
            <span className="slider-counter">
              <strong>0{currentIndex + 1}</strong> / 0{topProducts.length}
            </span>
            <button 
              className="slider-nav-btn next" 
              onClick={handleNext}
              aria-label="Next Featured Product"
              title="Next"
            >
              <ChevronRight size={18} />
            </button>
          </div>
        </div>
      </div>

      {/* Guaranteed 100% Full-Width Spotlight Showcase */}
      <div className="spotlight-showcase-wrapper">
        <div 
          key={currentProduct.id} 
          className={`spotlight-card slide-anim-${slideDirection}`}
        >
          {/* Left Column (70% Width): Edge-to-Edge Full Screen Product Photo */}
          <div className="spotlight-image-col">
            <div className="spotlight-rank-tag">
              <Sparkles size={13} /> #{currentIndex + 1} Best Seller
            </div>
            <img 
              src={currentProduct.imagePath || 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=1200&auto=format&fit=crop&q=85'} 
              alt={currentProduct.name} 
              className="spotlight-hero-img"
            />
            <div className="spotlight-img-overlay"></div>
          </div>

          {/* Right Column (30% Width): Rich Details & Interactive Action */}
          <div className="spotlight-content-col">
            <div className="spotlight-meta-top">
              <span className="spotlight-category-chip">{currentProduct.category}</span>
              {isOutOfStock ? (
                <span className="spotlight-stock-badge out-of-stock">
                  <XCircle size={13} /> Sold Out
                </span>
              ) : isLowStock ? (
                <span className="spotlight-stock-badge low-stock">
                  <AlertTriangle size={13} /> Only {currentProduct.stockQuantity} Left
                </span>
              ) : (
                <span className="spotlight-stock-badge in-stock">
                  <CheckCircle2 size={13} /> In Stock ({currentProduct.stockQuantity} Units)
                </span>
              )}
            </div>

            <h3 className="spotlight-title">{currentProduct.name}</h3>
            
            <p className="spotlight-description">
              {currentProduct.description || 'Precision handcrafted stationery crafted from archival grade materials for seamless writing and desk productivity.'}
            </p>

            <div className="spotlight-pricing-row">
              <div className="spotlight-price-box">
                <span className="spotlight-price-label">Price</span>
                <span className="spotlight-price-val">Rs. {currentProduct.price.toFixed(2)}</span>
              </div>
              <div className="spotlight-verified-pill">
                <TrendingUp size={14} /> Top Pick
              </div>
            </div>

            {/* Add to Cart / Quantity Stepper */}
            <div className="spotlight-actions-row">
              {itemQty > 0 ? (
                <div className="spotlight-stepper-box">
                  <button
                    className="spotlight-stepper-btn minus"
                    onClick={() => handleUpdateQty(itemQty - 1)}
                    disabled={loadingAction}
                    aria-label="Decrease Quantity"
                  >
                    <Minus size={16} />
                  </button>
                  <span className="spotlight-stepper-value">
                    {loadingAction ? <Loader2 size={16} className="spin-icon" /> : itemQty}
                  </span>
                  <button
                    className="spotlight-stepper-btn plus"
                    onClick={() => handleUpdateQty(itemQty + 1)}
                    disabled={loadingAction || itemQty >= currentProduct.stockQuantity}
                    aria-label="Increase Quantity"
                  >
                    <Plus size={16} />
                  </button>
                  <span className="spotlight-in-cart-label">In Bag</span>
                </div>
              ) : (
                <button
                  className="btn-spotlight-add-cart"
                  onClick={handleAddToCart}
                  disabled={isOutOfStock || loadingAction}
                >
                  {loadingAction ? (
                    <>
                      <Loader2 size={18} className="spin-icon" />
                      <span>Adding to Bag...</span>
                    </>
                  ) : isOutOfStock ? (
                    <>
                      <XCircle size={18} />
                      <span>Sold Out</span>
                    </>
                  ) : (
                    <>
                      <ShoppingCart size={18} />
                      <span>Add to Bag • Rs. {currentProduct.price.toFixed(2)}</span>
                    </>
                  )}
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Carousel Indicator Dots */}
        <div className="slider-indicator-dots">
          {topProducts.map((_, dotIdx) => (
            <button
              key={dotIdx}
              className={`indicator-dot ${dotIdx === currentIndex ? 'active' : ''}`}
              onClick={() => {
                setSlideDirection(dotIdx > currentIndex ? 'next' : 'prev');
                setCurrentIndex(dotIdx);
              }}
              aria-label={`Go to slide ${dotIdx + 1}`}
            />
          ))}
        </div>
      </div>
    </section>
  );
};
