import React, { useState } from 'react';
import {
  X,
  CreditCard,
  QrCode,
  CheckCircle2,
  ShieldCheck,
  Truck,
  Copy,
  Check,
  Loader2,
  Package,
  ShoppingBag,
  ArrowRight
} from 'lucide-react';
import { useCart } from '../context/CartContext';

interface CheckoutModalProps {
  isOpen: boolean;
  onClose: () => void;
  onOrderComplete: () => void;
  onViewMyOrders?: () => void;
}

export const CheckoutModal: React.FC<CheckoutModalProps> = ({
  isOpen,
  onClose,
  onOrderComplete,
  onViewMyOrders,
}) => {
  const { cart, checkout } = useCart();
  const [paymentMethod, setPaymentMethod] = useState<'cash' | 'upi' | 'card'>('cash');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [copiedUpi, setCopiedUpi] = useState(false);
  const [successResult, setSuccessResult] = useState<any | null>(null);

  // Card form state for card mode
  const [cardNumber, setCardNumber] = useState('');
  const [cardExpiry, setCardExpiry] = useState('');
  const [cardCvv, setCardCvv] = useState('');

  if (!isOpen) return null;

  const items = cart?.items || [];
  const subtotal = cart?.subtotal || 0;
  const tax = cart?.tax || 0;
  const total = cart?.total || 0;

  const handleCopyUpi = () => {
    navigator.clipboard.writeText('stationery.shop@upi');
    setCopiedUpi(true);
    setTimeout(() => setCopiedUpi(false), 2000);
  };

  const handlePlaceOrder = async () => {
    if (items.length === 0) {
      setError('Your cart is empty. Please add stationery items before checking out.');
      return;
    }

    if (paymentMethod === 'card') {
      if (!cardNumber.trim() || cardNumber.replace(/\s/g, '').length < 12) {
        setError('Please enter a valid 16-digit card number.');
        return;
      }
    }

    setError('');
    setSubmitting(true);

    try {
      // Backend handles 'cash' or 'upi' (card acts as online/cashless)
      const backendPayment = paymentMethod === 'upi' ? 'upi' : 'cash';
      const res = await checkout(backendPayment);
      setSuccessResult(res);
    } catch (err: any) {
      setError(err.message || 'Checkout failed. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleCloseModal = () => {
    if (successResult) {
      setSuccessResult(null);
      onOrderComplete();
    } else {
      onClose();
    }
  };

  const formatCardNumber = (val: string) => {
    const cleaned = val.replace(/\D/g, '').slice(0, 16);
    const parts = cleaned.match(/.{1,4}/g);
    return parts ? parts.join(' ') : cleaned;
  };

  const formatExpiry = (val: string) => {
    const cleaned = val.replace(/\D/g, '').slice(0, 4);
    if (cleaned.length >= 3) {
      return `${cleaned.slice(0, 2)}/${cleaned.slice(2)}`;
    }
    return cleaned;
  };

  return (
    <div className="modal-backdrop" onClick={handleCloseModal}>
      <div className="modal-card checkout-modal" onClick={(e) => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="modal-header">
          <div className="modal-title-group">
            <h2 className="modal-title">
              {successResult ? 'Order Confirmation' : 'Complete Checkout'}
            </h2>
            <p className="modal-subtitle">
              {successResult
                ? 'Your order has been recorded and scheduled for fulfillment'
                : 'Review your order items, select payment method & confirm'}
            </p>
          </div>
          <button className="modal-close-btn" onClick={handleCloseModal} aria-label="Close Checkout">
            <X size={20} />
          </button>
        </div>

        {/* Success View */}
        {successResult ? (
          <div className="checkout-success-view">
            <div className="success-icon-wrapper">
              <CheckCircle2 size={56} className="success-icon" />
            </div>

            <h3 className="success-heading">Order Placed Successfully!</h3>
            <p className="success-msg">
              {successResult.message || 'Thank you for your purchase. Your order is being prepared.'}
            </p>

            {/* Receipt Summary Card */}
            <div className="order-details-card">
              <div className="receipt-row">
                <span className="receipt-label">Order Reference #</span>
                <strong className="receipt-value highlight">#{successResult.order?.id || 'ORD-NEW'}</strong>
              </div>
              <div className="receipt-row">
                <span className="receipt-label">Payment Method</span>
                <span className="receipt-value">{paymentMethod.toUpperCase()}</span>
              </div>
              <div className="receipt-row">
                <span className="receipt-label">Items Purchased</span>
                <span className="receipt-value">{items.length} stationery items</span>
              </div>
              <div className="receipt-row">
                <span className="receipt-label">Total Amount Paid</span>
                <strong className="receipt-value total-price">Rs. {Number(successResult.order?.totalAmount || total).toFixed(2)}</strong>
              </div>
              <div className="receipt-row status-row">
                <span className="receipt-label">Delivery Status</span>
                <span className="badge badge-success">
                  <Truck size={13} /> Processing Dispatch
                </span>
              </div>
            </div>

            {/* Actions */}
            <div className="checkout-success-actions">
              {onViewMyOrders && (
                <button
                  type="button"
                  className="btn btn-secondary btn-full"
                  onClick={() => {
                    setSuccessResult(null);
                    onViewMyOrders();
                  }}
                >
                  <Package size={16} />
                  <span>View in My Orders</span>
                </button>
              )}

              <button
                type="button"
                className="btn btn-primary btn-full"
                onClick={() => {
                  setSuccessResult(null);
                  onOrderComplete();
                }}
              >
                <ShoppingBag size={16} />
                <span>Continue Shopping</span>
              </button>
            </div>
          </div>
        ) : (
          <div className="checkout-body">
            {error && <div className="alert-box alert-error">{error}</div>}

            {/* Payment Method Selector */}
            <div className="payment-section">
              <label className="section-label">Choose Payment Method</label>
              <div className="payment-options-grid">
                <button
                  type="button"
                  className={`payment-card ${paymentMethod === 'cash' ? 'active' : ''}`}
                  onClick={() => setPaymentMethod('cash')}
                >
                  <div className="payment-card-icon">
                    <Truck size={22} />
                  </div>
                  <div className="payment-card-info">
                    <h4>Cash on Delivery</h4>
                    <p>Pay upon delivery / desk pickup</p>
                  </div>
                  {paymentMethod === 'cash' && <Check size={18} className="payment-check-icon" />}
                </button>

                <button
                  type="button"
                  className={`payment-card ${paymentMethod === 'upi' ? 'active' : ''}`}
                  onClick={() => setPaymentMethod('upi')}
                >
                  <div className="payment-card-icon">
                    <QrCode size={22} />
                  </div>
                  <div className="payment-card-info">
                    <h4>UPI / Online QR</h4>
                    <p>Scan with GPay / PhonePe / Paytm</p>
                  </div>
                  {paymentMethod === 'upi' && <Check size={18} className="payment-check-icon" />}
                </button>

                <button
                  type="button"
                  className={`payment-card ${paymentMethod === 'card' ? 'active' : ''}`}
                  onClick={() => setPaymentMethod('card')}
                >
                  <div className="payment-card-icon">
                    <CreditCard size={22} />
                  </div>
                  <div className="payment-card-info">
                    <h4>Credit / Debit Card</h4>
                    <p>Visa, MasterCard, RuPay</p>
                  </div>
                  {paymentMethod === 'card' && <Check size={18} className="payment-check-icon" />}
                </button>
              </div>
            </div>

            {/* UPI QR Display Panel */}
            {paymentMethod === 'upi' && (
              <div className="upi-qr-box">
                <div className="upi-qr-header">
                  <span className="upi-qr-title">Instant UPI Fast Pay</span>
                  <span className="upi-badge">
                    <ShieldCheck size={13} /> Verified Merchant
                  </span>
                </div>

                <div className="upi-qr-content">
                  <div className="qr-image-wrapper">
                    <img
                      src={`https://api.qrserver.com/v1/create-qr-code/?size=140x140&data=upi://pay?pa=stationery.shop@upi&pn=LuminaAtelier&am=${total.toFixed(2)}&cu=USD`}
                      alt="UPI QR Code"
                      className="qr-img"
                    />
                  </div>
                  <div className="upi-details">
                    <p className="upi-id-label">Scan QR or transfer to UPI ID:</p>
                    <div className="upi-id-box">
                      <span className="upi-id-text">stationery.shop@upi</span>
                      <button
                        type="button"
                        className="btn-copy-upi"
                        onClick={handleCopyUpi}
                        title="Copy UPI ID"
                      >
                        {copiedUpi ? <Check size={14} className="copied" /> : <Copy size={14} />}
                        <span>{copiedUpi ? 'Copied' : 'Copy'}</span>
                      </button>
                    </div>
                    <div className="upi-amount-hint">
                      Payable: <strong>Rs. {total.toFixed(2)}</strong>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Card Details Panel */}
            {paymentMethod === 'card' && (
              <div className="card-input-panel">
                <div className="form-group">
                  <label className="form-label">Card Number</label>
                  <div className="input-with-icon">
                    <CreditCard size={18} className="input-icon" />
                    <input
                      type="text"
                      className="form-input"
                      placeholder="1234 5678 9012 3456"
                      value={cardNumber}
                      onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
                      maxLength={19}
                    />
                  </div>
                </div>

                <div className="card-row-split">
                  <div className="form-group">
                    <label className="form-label">Expiry (MM/YY)</label>
                    <input
                      type="text"
                      className="form-input"
                      placeholder="MM/YY"
                      value={cardExpiry}
                      onChange={(e) => setCardExpiry(formatExpiry(e.target.value))}
                      maxLength={5}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">CVV</label>
                    <input
                      type="password"
                      className="form-input"
                      placeholder="123"
                      value={cardCvv}
                      onChange={(e) => setCardCvv(e.target.value.replace(/\D/g, '').slice(0, 4))}
                      maxLength={4}
                    />
                  </div>
                </div>
              </div>
            )}

            {/* Order Items & Price Summary Box */}
            <div className="order-summary-box">
              <div className="summary-header">
                <span className="summary-title">Order Items ({items.length})</span>
                <span className="free-delivery-badge">
                  <Truck size={13} /> Free Executive Delivery
                </span>
              </div>

              <div className="order-items-preview">
                {items.length === 0 ? (
                  <p className="empty-items-text">No items currently in cart.</p>
                ) : (
                  items.map((item) => (
                    <div key={item.productId} className="preview-row">
                      <div className="preview-item-info">
                        <span className="preview-item-name">{item.productName}</span>
                        <span className="preview-item-qty">× {item.quantity}</span>
                      </div>
                      <span className="preview-item-price">Rs. {item.subtotal.toFixed(2)}</span>
                    </div>
                  ))
                )}
              </div>

              {/* Price Breakdown */}
              <div className="checkout-total-breakdown">
                <div className="breakdown-row">
                  <span>Subtotal</span>
                  <span>Rs. {subtotal.toFixed(2)}</span>
                </div>
                <div className="breakdown-row">
                  <span>Tax (10%)</span>
                  <span>Rs. {tax.toFixed(2)}</span>
                </div>
                <div className="breakdown-row total-highlight">
                  <span>Total Amount Due</span>
                  <span>Rs. {total.toFixed(2)}</span>
                </div>
                <div className="server-secure-badge">
                  <ShieldCheck size={14} className="security-icon" />
                  <span>256-Bit Encrypted & Verified Server Checkout</span>
                </div>
              </div>
            </div>

            {/* Submit Button */}
            <button
              type="button"
              className="btn btn-primary btn-full checkout-submit-btn"
              onClick={handlePlaceOrder}
              disabled={submitting || items.length === 0}
            >
              {submitting ? (
                <>
                  <Loader2 size={18} className="spin-icon" />
                  <span>Placing Your Order...</span>
                </>
              ) : (
                <>
                  <span>Confirm Order (${total.toFixed(2)})</span>
                  <ArrowRight size={18} />
                </>
              )}
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
