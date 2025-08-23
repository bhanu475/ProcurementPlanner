import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Supplier } from '../../types';

export interface SuppliersState {
  suppliers: Supplier[];
  loading: boolean;
  error: string | null;
}

const initialState: SuppliersState = {
  suppliers: [],
  loading: false,
  error: null,
};

const suppliersSlice = createSlice({
  name: 'suppliers',
  initialState,
  reducers: {
    fetchSuppliersStart: (state) => {
      state.loading = true;
      state.error = null;
    },
    fetchSuppliersSuccess: (state, action: PayloadAction<Supplier[]>) => {
      state.suppliers = action.payload;
      state.loading = false;
    },
    fetchSuppliersFailure: (state, action: PayloadAction<string>) => {
      state.loading = false;
      state.error = action.payload;
    },
  },
});

export const { fetchSuppliersStart, fetchSuppliersSuccess, fetchSuppliersFailure } = suppliersSlice.actions;
export default suppliersSlice.reducer;