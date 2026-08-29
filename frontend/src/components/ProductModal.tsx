import React, { useState, useEffect } from 'react';
import { X, Upload, Package } from 'lucide-react';
import type { Product } from '../types';
import { api } from '../services/api';

interface ProductModalProps {
  isOpen: boolean;
  product: Product | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const ProductModal: React.FC<ProductModalProps> = ({ isOpen, product, onClose, onSuccess }) => {
  const [name, setName] = useState('');
  const [category, setCategory] = useState('');
  const [price, setPrice] = useState<number | ''>('');
  const [stockQuantity, setStockQuantity] = useState<number | ''>('');
  const [lowStockThreshold, setLowStockThreshold] = useState<number | ''>(5);
  const [isVisible, setIsVisible] = useState(true);
  const [imageUrl, setImageUrl] = useState('');
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (product) {
      setName(product.name || '');
      setCategory(product.category || '');
      setPrice(product.price || 0);
      setStockQuantity(product.stockQuantity || 0);
      setLowStockThreshold(product.lowStockThreshold || 5);
      setIsVisible(product.isVisible !== false);
      setImageFile(null);
      setImageUrl(product.imagePath || '');
      if (product.imagePath) {
        if (product.imagePath.startsWith('http://') || product.imagePath.startsWith('https://')) {
          setImagePreview(product.imagePath);
        } else {
          const apiBase = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';
          const cleanBase = apiBase.replace('/api', '');
          setImagePreview(`${cleanBase}${product.imagePath}`);
        }
      } else {
        setImagePreview(null);
      }
    } else {
      setName('');
      setCategory('');
      setPrice('');
      setStockQuantity('');
      setLowStockThreshold(5);
      setIsVisible(true);
      setImageFile(null);
      setImageUrl('');
      setImagePreview(null);
    }
    setError('');
  }, [product, isOpen]);

  if (!isOpen) return null;

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      setImageFile(file);
      setImagePreview(URL.createObjectURL(file));
    }
  };

  const handleUrlChange = (url: string) => {
    setImageUrl(url);
    if (url.trim()) {
      setImagePreview(url.trim());
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || Number(price) <= 0 || Number(stockQuantity) < 0) {
      setError('Please provide valid product name, positive price, and stock quantity.');
      return;
    }

    setError('');
    setLoading(true);

    const formData = new FormData();
    formData.append('Name', name.trim());
    formData.append('Category', category.trim() || 'Uncategorized');
    formData.append('Price', price.toString());
    formData.append('StockQuantity', stockQuantity.toString());
    formData.append('LowStockThreshold', lowStockThreshold.toString());
    formData.append('IsVisible', isVisible.toString());

    if (imageUrl.trim()) {
      formData.append('ImageUrl', imageUrl.trim());
    }

    if (imageFile) {
      formData.append('Image', imageFile);
    }

    try {
      if (product) {
        await api.updateProduct(product.id, formData);
      } else {
        await api.createProduct(formData);
      }
      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err.message || 'Failed to save product.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-backdrop">
      <div className="modal-card product-modal">
        <div className="modal-header">
          <h2 className="modal-title">{product ? 'Edit Product' : 'Add New Product'}</h2>
          <button className="modal-close-btn" onClick={onClose}>
            <X size={20} />
          </button>
        </div>

        {error && <div className="alert-box alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="product-form">
          <div className="form-grid">
            <div className="form-group span-2">
              <label className="form-label">Product Name *</label>
              <input
                type="text"
                className="form-input"
                placeholder="e.g. A5 Spiral Notebook"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Category</label>
              <input
                type="text"
                className="form-input"
                placeholder="e.g. Office Supplies"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Price ($) *</label>
              <input
                type="number"
                step="0.01"
                min="0.01"
                className="form-input"
                placeholder="0.00"
                value={price}
                onChange={(e) => setPrice(e.target.value === '' ? '' : parseFloat(e.target.value))}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Initial Stock Quantity *</label>
              <input
                type="number"
                min="0"
                className="form-input"
                placeholder="0"
                value={stockQuantity}
                onChange={(e) => setStockQuantity(e.target.value === '' ? '' : parseInt(e.target.value))}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">Low Stock Threshold</label>
              <input
                type="number"
                min="0"
                className="form-input"
                placeholder="5"
                value={lowStockThreshold}
                onChange={(e) => setLowStockThreshold(e.target.value === '' ? '' : parseInt(e.target.value))}
              />
            </div>

            <div className="form-group span-2">
              <label className="form-label">Product Image (Cloudinary URL or Upload)</label>
              <div className="image-upload-wrapper">
                <div className="image-preview-area">
                  {imagePreview ? (
                    <img src={imagePreview} alt="Preview" className="img-preview" onError={(e) => { (e.target as HTMLImageElement).src = 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=500'; }} />
                  ) : (
                    <div className="no-img-placeholder">
                      <Package size={32} />
                      <span>No image selected</span>
                    </div>
                  )}
                </div>
                <div className="image-input-options-stack">
                  <input
                    type="url"
                    className="form-input url-input-field"
                    placeholder="Paste Cloudinary / Image URL (e.g. https://res.cloudinary.com/...)"
                    value={imageUrl}
                    onChange={(e) => handleUrlChange(e.target.value)}
                  />
                  <label className="file-upload-btn">
                    <Upload size={16} />
                    <span>Choose File</span>
                    <input type="file" accept="image/*" onChange={handleImageChange} hidden />
                  </label>
                </div>
              </div>
            </div>

            <div className="form-group span-2">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={isVisible}
                  onChange={(e) => setIsVisible(e.target.checked)}
                />
                <span>Make product visible in public storefront</span>
              </label>
            </div>
          </div>

          <div className="modal-footer-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Saving...' : product ? 'Update Product' : 'Create Product'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
