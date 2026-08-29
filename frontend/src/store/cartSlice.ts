import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { CartItem, CartResponse } from '../types';

interface CartState {
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  tax: number;
  total: number;
  isCartOpen: boolean;
  loading: boolean;
}

const initialState: CartState = {
  items: [],
  itemCount: 0,
  subtotal: 0,
  tax: 0,
  total: 0,
  isCartOpen: false,
  loading: false,
};

export const cartSlice = createSlice({
  name: 'cart',
  initialState,
  reducers: {
    setCart: (state, action: PayloadAction<CartResponse>) => {
      state.items = action.payload.items || [];
      state.itemCount = action.payload.itemCount || 0;
      state.subtotal = action.payload.subtotal || 0;
      state.tax = action.payload.tax || 0;
      state.total = action.payload.total || 0;
    },
    setItemCount: (state, action: PayloadAction<number>) => {
      state.itemCount = action.payload;
    },
    setIsCartOpen: (state, action: PayloadAction<boolean>) => {
      state.isCartOpen = action.payload;
    },
    setCartLoading: (state, action: PayloadAction<boolean>) => {
      state.loading = action.payload;
    },
    clearCartState: (state) => {
      state.items = [];
      state.itemCount = 0;
      state.subtotal = 0;
      state.tax = 0;
      state.total = 0;
      state.isCartOpen = false;
    },
  },
});

export const { setCart, setItemCount, setIsCartOpen, setCartLoading, clearCartState } = cartSlice.actions;
export default cartSlice.reducer;
