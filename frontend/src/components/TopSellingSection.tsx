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
  const [loadingAction, setLoadingAction] = useState(false);
  const touchStartX = useRef<number | null>(null);

  // Take top 6 products for the sliding showcase
  const topProducts = products.filter(p => p.isVisible !== false).slice(0, 6);

  // Auto-play sliding every 4.5 seconds
  useEffect(() => {
    if (topProducts.length <= 1 || isPaused) return;

    const timer = setInterval(() => {
      setCurrentIndex((prev) => (prev + 1) % topProducts.length);
    }, 4500);

    return () => clearInterval(timer);
  }, [topProducts.length, isPaused]);

  if (topProducts.length === 0) return null;

  const currentProduct = topProducts[currentIndex];
  const isOutOfStock = currentProduct.stockQuantity <= 0 || currentProduct.isOutOfStock;

  const handlePrev = () => {
    setCurrentIndex((prev) => (prev === 0 ? topProducts.length - 1 : prev - 1));
  };

  const handleNext = () => {
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

      {/* Single Sliding Showcase (1 Item at a Time) */}
      <div className="single-slide-showcase-container">
        <div 
          className="single-slide-track"
          style={{ transform: `translateX(-${currentIndex * 100}%)` }}
        >
          {topProducts.map((product, idx) => {
            const itemInCart = cart?.items.find((item) => item.productId === product.id);
            const itemQty = itemInCart ? itemInCart.quantity : 0;
            const itemOutOfStock = product.stockQuantity <= 0 || product.isOutOfStock;
            const itemLowStock = !itemOutOfStock && (product.stockQuantity <= product.lowStockThreshold || product.isLowStock);

            return (
              <div key={product.id} className="single-slide-item">
                <div className="spotlight-card">
                  {/* Left Column: Big Product Image */}
                  <div className="spotlight-image-col">
                    <div className="spotlight-rank-tag">
                      <Sparkles size={13} /> #{idx + 1} Best Seller
                    </div>
                    <img 
                      src={product.imagePath || 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&auto=format&fit=crop&q=80'} 
                      alt={product.name} 
                      className="spotlight-hero-img"
                    />
                    <div className="spotlight-img-overlay"></div>
                  </div>

                  {/* Right Column: Rich Info & Interactive Action */}
                  <div className="spotlight-content-col">
                    <div className="spotlight-meta-top">
                      <span className="spotlight-category-chip">{product.category}</span>
                      {itemOutOfStock ? (
                        <span className="spotlight-stock-badge out-of-stock">
                          <XCircle size={13} /> Sold Out
                        </span>
                      ) : itemLowStock ? (
                        <span className="spotlight-stock-badge low-stock">
                          <AlertTriangle size={13} /> Only {product.stockQuantity} Left
                        </span>
                      ) : (
                        <span className="spotlight-stock-badge in-stock">
                          <CheckCircle2 size={13} /> In Stock ({product.stockQuantity} Units)
                        </span>
                      )}
                    </div>

                    <h3 className="spotlight-title">{product.name}</h3>
                    
                    <p className="spotlight-description">
                      {product.description || 'Precision handcrafted stationery crafted from archival grade materials for seamless writing and desk productivity.'}
                    </p>

                    <div className="spotlight-pricing-row">
                      <div className="spotlight-price-box">
                        <span className="spotlight-price-label">Price</span>
                        <span className="spotlight-price-val">Rs. {product.price.toFixed(2)}</span>
                      </div>
                      <div className="spotlight-verified-pill">
                        <TrendingUp size={14} /> Top Pick this Week
                      </div>
                    </div>

                    {/* Add to Cart / Stepper */}
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
                            disabled={loadingAction || itemQty >= product.stockQuantity}
                            aria-label="Increase Quantity"
                          >
                            <Plus size={16} />
                          </button>
                          <span className="spotlight-in-cart-label">In Your Cart</span>
                        </div>
                      ) : (
                        <button
                          className="btn-spotlight-add-cart"
                          onClick={handleAddToCart}
                          disabled={itemOutOfStock || loadingAction}
                        >
                          {loadingAction ? (
                            <>
                              <Loader2 size={18} className="spin-icon" />
                              <span>Adding to Cart...</span>
                            </>
                          ) : itemOutOfStock ? (
                            <>
                              <XCircle size={18} />
                              <span>Currently Sold Out</span>
                            </>
                          ) : (
                            <>
                              <ShoppingCart size={18} />
                              <span>Add to Bag • Rs. {product.price.toFixed(2)}</span>
                            </>
                          )}
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Carousel Indicator Dots */}
        <div className="slider-indicator-dots">
          {topProducts.map((_, dotIdx) => (
            <button
              key={dotIdx}
              className={`indicator-dot ${dotIdx === currentIndex ? 'active' : ''}`}
              onClick={() => setCurrentIndex(dotIdx)}
              aria-label={`Go to slide ${dotIdx + 1}`}
            />
          ))}
        </div>
      </div>
    </section>
  );
};
