import React, { createContext, useContext, useEffect, useCallback } from 'react';
import type { CartResponse } from '../types';
import { api } from '../services/api';
import { useAuth } from './AuthContext';
import { useAppDispatch, useAppSelector } from '../store';
import { setCart, setIsCartOpen as setIsCartOpenAction, clearCartState } from '../store/cartSlice';

interface CartContextType {
  cart: CartResponse | null;
  itemCount: number;
  loading: boolean;
  isCartOpen: boolean;
  setIsCartOpen: (open: boolean) => void;
  fetchCart: () => Promise<void>;
  addToCart: (productId: number, quantity?: number) => Promise<any>;
  updateQuantity: (productId: number, quantity: number) => Promise<any>;
  removeFromCart: (productId: number) => Promise<any>;
  checkout: (paymentMethod: 'cash' | 'upi') => Promise<any>;
}

const CartContext = createContext<CartContextType | undefined>(undefined);

export const CartProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user } = useAuth();
  const dispatch = useAppDispatch();
  const { items, itemCount, subtotal, tax, total, isCartOpen, loading } = useAppSelector((state) => state.cart);

  const fetchCart = useCallback(async () => {
    if (!user) {
      dispatch(clearCartState());
      return;
    }
    try {
      const data = await api.getCart();
      dispatch(setCart(data));
    } catch {
      dispatch(clearCartState());
    }
  }, [user, dispatch]);

  useEffect(() => {
    fetchCart();
  }, [fetchCart]);

  const addToCart = async (productId: number, quantity = 1) => {
    const res = await api.addToCart(productId, quantity);
    await fetchCart();
    return res;
  };

  const updateQuantity = async (productId: number, quantity: number) => {
    const res = await api.updateCartQuantity(productId, quantity);
    await fetchCart();
    return res;
  };

  const removeFromCart = async (productId: number) => {
    const res = await api.removeFromCart(productId);
    await fetchCart();
    return res;
  };

  const checkout = async (paymentMethod: 'cash' | 'upi') => {
    const res = await api.checkout(paymentMethod);
    await fetchCart();
    return res;
  };

  const cartResponse: CartResponse = {
    items,
    itemCount,
    subtotal,
    tax,
    total,
  };

  return (
    <CartContext.Provider
      value={{
        cart: cartResponse,
        itemCount,
        loading,
        isCartOpen,
        setIsCartOpen: (open) => dispatch(setIsCartOpenAction(open)),
        fetchCart,
        addToCart,
        updateQuantity,
        removeFromCart,
        checkout,
      }}
    >
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => {
  const context = useContext(CartContext);
  if (!context) throw new Error('useCart must be used within CartProvider');
  return context;
};
