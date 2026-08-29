import React from 'react';
import { X, Trash2, Plus, Minus, ShoppingBag, ArrowRight } from 'lucide-react';
import { useCart } from '../context/CartContext';

interface CartDrawerProps {
  onOpenCheckout: () => void;
}

export const CartDrawer: React.FC<CartDrawerProps> = ({ onOpenCheckout }) => {
  const { cart, isCartOpen, setIsCartOpen, updateQuantity, removeFromCart } = useCart();

  if (!isCartOpen) return null;

  const items = cart?.items || [];
  const subtotal = cart?.subtotal || 0;
  const tax = cart?.tax || 0;
  const total = cart?.total || 0;

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
      return 'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=200&auto=format&fit=crop';
    }
    if (cat.includes('writing') || n.includes('pen') || n.includes('marker') || n.includes('highlighter')) {
      return 'https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=200&auto=format&fit=crop';
    }
    return 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=200&auto=format&fit=crop';
  };

  return (
    <div className="drawer-backdrop" onClick={() => setIsCartOpen(false)}>
      <div className="drawer-card" onClick={(e) => e.stopPropagation()}>
        {/* Drawer Header */}
        <div className="drawer-header">
          <div className="drawer-title-group">
            <ShoppingBag size={22} className="drawer-icon" />
            <h2>Your Shopping Cart</h2>
            <span className="drawer-item-badge">{items.length} items</span>
          </div>
          <button className="drawer-close-btn" onClick={() => setIsCartOpen(false)}>
            <X size={20} />
          </button>
        </div>

        {/* Drawer Body - Items List */}
        <div className="drawer-body">
          {items.length === 0 ? (
            <div className="empty-cart-state">
              <ShoppingBag size={64} className="empty-icon" />
              <h3>Your cart is empty</h3>
              <p>Explore our stationery collection and add items to your cart.</p>
            </div>
          ) : (
            <div className="cart-items-list">
              {items.map((item) => {
                const imgUrl = getImageUrl(item.imagePath, item.category, item.productName);
                return (
                  <div key={item.productId} className="cart-item-card">
                    {/* Thumbnail */}
                    <div className="cart-item-thumb">
                      {imgUrl ? (
                        <img src={imgUrl} alt={item.productName} />
                      ) : (
                        <div className="thumb-placeholder">
                          <ShoppingBag size={20} />
                        </div>
                      )}
                    </div>

                    {/* Details */}
                    <div className="cart-item-details">
                      <h4 className="cart-item-title">{item.productName}</h4>
                      <div className="cart-item-unit-price">Rs. {item.price.toFixed(2)} each</div>

                      {/* Quantity Controls */}
                      <div className="quantity-controls">
                        <button
                          className="qty-btn"
                          onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                        >
                          <Minus size={14} />
                        </button>
                        <span className="qty-val">{item.quantity}</span>
                        <button
                          className="qty-btn"
                          onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                          disabled={item.quantity >= item.stockQuantity}
                        >
                          <Plus size={14} />
                        </button>
                      </div>
                    </div>

                    {/* Subtotal & Delete */}
                    <div className="cart-item-end">
                      <div className="cart-item-subtotal">Rs. {item.subtotal.toFixed(2)}</div>
                      <button
                        className="remove-item-btn"
                        onClick={() => removeFromCart(item.productId)}
                        title="Remove Item"
                      >
                        <Trash2 size={16} />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Drawer Footer - Price & Checkout */}
        {items.length > 0 && (
          <div className="drawer-footer">
            <div className="price-summary-box">
              <div className="summary-row">
                <span>Subtotal</span>
                <span>Rs. {subtotal.toFixed(2)}</span>
              </div>
              <div className="summary-row">
                <span>Tax (10%)</span>
                <span>Rs. {tax.toFixed(2)}</span>
              </div>
              <div className="summary-row total-row">
                <span>Total Amount</span>
                <span>Rs. {total.toFixed(2)}</span>
              </div>
            </div>

            <button
              className="btn btn-primary btn-full checkout-btn"
              onClick={() => {
                setIsCartOpen(false);
                onOpenCheckout();
              }}
            >
              <span>Proceed to Checkout</span>
              <ArrowRight size={18} />
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
