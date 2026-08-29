import React, { useState } from 'react';
import { ShoppingCart, AlertTriangle, CheckCircle2, XCircle, Plus, Minus, Loader2 } from 'lucide-react';
import type { Product } from '../types';
import { useCart } from '../context/CartContext';
import { useAuth } from '../context/AuthContext';

interface ProductCardProps {
  product: Product;
  onOpenAuth: (tab?: 'login' | 'register') => void;
}

export const ProductCard: React.FC<ProductCardProps> = ({ product, onOpenAuth }) => {
  const { user } = useAuth();
  const { cart, addToCart, updateQuantity, removeFromCart } = useCart();
  const [loadingAction, setLoadingAction] = useState(false);

  const cartItem = cart?.items.find((item) => item.productId === product.id);
  const inCartQuantity = cartItem ? cartItem.quantity : 0;

  const isOutOfStock = product.stockQuantity <= 0 || product.isOutOfStock;
  const isLowStock = !isOutOfStock && (product.stockQuantity <= product.lowStockThreshold || product.isLowStock);

  const handleAddToCart = async () => {
    if (!user) {
      onOpenAuth();
      return;
    }
    if (isOutOfStock || loadingAction) return;

    setLoadingAction(true);
    try {
      await addToCart(product.id, 1);
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
        await removeFromCart(product.id);
      } else {
        await updateQuantity(product.id, newQty);
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
      return 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=500&auto=format&fit=crop';
    }
    if (cat.includes('writing') || n.includes('pen') || n.includes('marker') || n.includes('highlighter')) {
      return 'https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=500&auto=format&fit=crop';
    }
    if (cat.includes('desk') || n.includes('pad') || n.includes('sticky')) {
      return 'https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=500&auto=format&fit=crop';
    }
    if (cat.includes('school') || n.includes('ruler') || n.includes('geometry')) {
      return 'https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=500&auto=format&fit=crop';
    }

    return 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=500&auto=format&fit=crop';
  };

  const imageUrl = getImageUrl(product.imagePath, product.category, product.name);

  return (
    <div className={`product-card ${isOutOfStock ? 'card-out-of-stock' : ''} ${inCartQuantity > 0 ? 'card-in-cart' : ''}`}>
      {/* Stock Status Badge (No numbers shown to customer) */}
      <div className="card-badge-container">
        {isOutOfStock ? (
          <span className="badge badge-danger">
            <XCircle size={13} /> Out of Stock
          </span>
        ) : isLowStock ? (
          <span className="badge badge-warning">
            <AlertTriangle size={13} /> Limited Availability
          </span>
        ) : (
          <span className="badge badge-success">
            <CheckCircle2 size={13} /> In Stock
          </span>
        )}
      </div>

      {/* Image Preview */}
      <div className="product-image-box">
        <img
          src={imageUrl}
          alt={product.name}
          className="product-img"
          onError={(e) => {
            (e.target as HTMLImageElement).src = 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=500&auto=format&fit=crop';
          }}
        />
        <div className="product-image-overlay" />
      </div>

      {/* Product Details */}
      <div className="product-body">
        <span className="product-category">{product.category || 'Stationery'}</span>
        <h3 className="product-name" title={product.name}>{product.name}</h3>

        <div className="product-footer">
          <div className="product-price-tag">
            <span className="currency">Rs.</span>
            <span className="amount">{product.price.toFixed(2)}</span>
          </div>

          {/* Stepper controls (+ and -) when item is in cart vs single Add to Cart button */}
          {inCartQuantity > 0 ? (
            <div className="product-card-qty-bar">
              <button
                type="button"
                className="qty-btn qty-btn-minus"
                onClick={() => handleUpdateQty(inCartQuantity - 1)}
                disabled={loadingAction}
                title="Decrease quantity"
                aria-label="Decrease quantity"
              >
                <Minus size={14} />
              </button>

              <span className="qty-count-text">
                {loadingAction ? <Loader2 size={12} className="spin-icon" /> : <strong>{inCartQuantity}</strong>}
              </span>

              <button
                type="button"
                className="qty-btn qty-btn-plus"
                onClick={() => handleUpdateQty(inCartQuantity + 1)}
                disabled={loadingAction || inCartQuantity >= product.stockQuantity}
                title="Increase quantity"
                aria-label="Increase quantity"
              >
                <Plus size={14} />
              </button>
            </div>
          ) : (
            <button
              type="button"
              className={`btn btn-primary add-to-cart-btn ${isOutOfStock ? 'disabled' : ''}`}
              onClick={handleAddToCart}
              disabled={isOutOfStock || loadingAction}
              aria-label={isOutOfStock ? 'Out of Stock' : `Add ${product.name} to Cart`}
            >
              {loadingAction ? (
                <Loader2 size={15} className="spin-icon" />
              ) : (
                <ShoppingCart size={14} />
              )}
              <span>{isOutOfStock ? 'Out of Stock' : 'Add to Cart'}</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

