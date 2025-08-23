import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { CustomerOrder, PagedResult, OrderFilter, ApiError } from '../../types';
import { customerApiService, CreateOrderRequest, OrderTrackingResponse } from '../../services/customerApi';

export interface CustomerOrdersState {
  orders: CustomerOrder[];
  currentOrder: CustomerOrder | null;
  orderTracking: OrderTrackingResponse | null;
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
  loading: boolean;
  submitting: boolean;
  error: string | null;
  filter: OrderFilter;
}

const initialState: CustomerOrdersState = {
  orders: [],
  currentOrder: null,
  orderTracking: null,
  totalCount: 0,
  currentPage: 1,
  pageSize: 10,
  totalPages: 0,
  loading: false,
  submitting: false,
  error: null,
  filter: {},
};

// Async thunks
export const submitOrder = createAsyncThunk(
  'customerOrders/submitOrder',
  async (orderData: CreateOrderRequest, { rejectWithValue }) => {
    try {
      const order = await customerApiService.submitOrder(orderData);
      return order;
    } catch (error) {
      const apiError = error as ApiError;
      return rejectWithValue(apiError.response?.data?.message || 'Failed to submit order');
    }
  }
);

export const fetchMyOrders = createAsyncThunk(
  'customerOrders/fetchMyOrders',
  async (
    { filter, pageNumber, pageSize }: { filter?: OrderFilter; pageNumber?: number; pageSize?: number },
    { rejectWithValue }
  ) => {
    try {
      const result = await customerApiService.getMyOrders(filter, pageNumber, pageSize);
      return result;
    } catch (error) {
      const apiError = error as ApiError;
      return rejectWithValue(apiError.response?.data?.message || 'Failed to fetch orders');
    }
  }
);

export const fetchOrderTracking = createAsyncThunk(
  'customerOrders/fetchOrderTracking',
  async (orderId: string, { rejectWithValue }) => {
    try {
      const tracking = await customerApiService.getOrderTracking(orderId);
      return tracking;
    } catch (error) {
      const apiError = error as ApiError;
      return rejectWithValue(apiError.response?.data?.message || 'Failed to fetch order tracking');
    }
  }
);

export const fetchOrderById = createAsyncThunk(
  'customerOrders/fetchOrderById',
  async (orderId: string, { rejectWithValue }) => {
    try {
      const order = await customerApiService.getOrderById(orderId);
      return order;
    } catch (error) {
      const apiError = error as ApiError;
      return rejectWithValue(apiError.response?.data?.message || 'Failed to fetch order');
    }
  }
);

export const cancelOrder = createAsyncThunk(
  'customerOrders/cancelOrder',
  async ({ orderId, reason }: { orderId: string; reason?: string }, { rejectWithValue }) => {
    try {
      await customerApiService.cancelOrder(orderId, reason);
      return orderId;
    } catch (error: unknown) {
      const apiError = error as ApiError;
      return rejectWithValue(apiError.response?.data?.message || 'Failed to cancel order');
    }
  }
);

const customerOrdersSlice = createSlice({
  name: 'customerOrders',
  initialState,
  reducers: {
    setFilter: (state, action: PayloadAction<OrderFilter>) => {
      state.filter = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
    clearCurrentOrder: (state) => {
      state.currentOrder = null;
    },
    clearOrderTracking: (state) => {
      state.orderTracking = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Submit order
      .addCase(submitOrder.pending, (state) => {
        state.submitting = true;
        state.error = null;
      })
      .addCase(submitOrder.fulfilled, (state, action) => {
        state.submitting = false;
        state.orders.unshift(action.payload);
        state.totalCount += 1;
      })
      .addCase(submitOrder.rejected, (state, action) => {
        state.submitting = false;
        state.error = action.payload as string;
      })
      
      // Fetch my orders
      .addCase(fetchMyOrders.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchMyOrders.fulfilled, (state, action) => {
        state.loading = false;
        if (action.payload) {
          state.orders = action.payload.items || [];
          state.totalCount = action.payload.totalCount || 0;
          state.currentPage = action.payload.pageNumber || 1;
          state.pageSize = action.payload.pageSize || 10;
          state.totalPages = action.payload.totalPages || 0;
        }
      })
      .addCase(fetchMyOrders.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      
      // Fetch order tracking
      .addCase(fetchOrderTracking.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchOrderTracking.fulfilled, (state, action) => {
        state.loading = false;
        if (action.payload) {
          state.orderTracking = action.payload;
          state.currentOrder = action.payload.order || null;
        }
      })
      .addCase(fetchOrderTracking.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      
      // Fetch order by ID
      .addCase(fetchOrderById.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchOrderById.fulfilled, (state, action) => {
        state.loading = false;
        state.currentOrder = action.payload;
      })
      .addCase(fetchOrderById.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      
      // Cancel order
      .addCase(cancelOrder.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(cancelOrder.fulfilled, (state, action) => {
        state.loading = false;
        // Update the order status in the list
        const orderIndex = state.orders.findIndex(order => order.id === action.payload);
        if (orderIndex !== -1) {
          state.orders[orderIndex].status = 'Cancelled';
        }
        // Update current order if it's the same
        if (state.currentOrder?.id === action.payload) {
          state.currentOrder.status = 'Cancelled';
        }
      })
      .addCase(cancelOrder.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });
  },
});

export const { setFilter, clearError, clearCurrentOrder, clearOrderTracking } = customerOrdersSlice.actions;
export default customerOrdersSlice.reducer;