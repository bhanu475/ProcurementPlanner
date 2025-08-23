import { configureStore } from '@reduxjs/toolkit';
import authSlice from './slices/authSlice';
import ordersSlice from './slices/ordersSlice';
import suppliersSlice from './slices/suppliersSlice';
import customerOrdersSlice from './slices/customerOrdersSlice';

export const store = configureStore({
  reducer: {
    auth: authSlice,
    orders: ordersSlice,
    suppliers: suppliersSlice,
    customerOrders: customerOrdersSlice,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;