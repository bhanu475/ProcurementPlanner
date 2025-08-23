import { createSlice, PayloadAction, createAsyncThunk } from '@reduxjs/toolkit';
import { 
  CustomerOrder, 
  OrderFilter, 
  PagedResult, 
  DashboardSummary, 
  OrderStatus,
  DistributionSuggestion,
  PurchaseOrder,
  DistributionPlan
} from '../../types';
import apiClient from '../../services/api';

interface OrdersState {
  orders: CustomerOrder[];
  dashboardSummary: DashboardSummary | null;
  selectedOrder: CustomerOrder | null;
  distributionSuggestion: DistributionSuggestion | null;
  purchaseOrders: PurchaseOrder[];
  loading: boolean;
  dashboardLoading: boolean;
  distributionLoading: boolean;
  error: string | null;
  filter: OrderFilter;
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

const initialState: OrdersState = {
  orders: [],
  dashboardSummary: null,
  selectedOrder: null,
  distributionSuggestion: null,
  purchaseOrders: [],
  loading: false,
  dashboardLoading: false,
  distributionLoading: false,
  error: null,
  filter: {},
  pagination: {
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  },
};

// Async thunks
export const fetchOrders = createAsyncThunk(
  'orders/fetchOrders',
  async (params: { filter?: OrderFilter; pageNumber?: number; pageSize?: number }) => {
    const response = await apiClient.get('/orders', { params });
    return response.data as PagedResult<CustomerOrder>;
  }
);

export const fetchDashboardSummary = createAsyncThunk(
  'orders/fetchDashboardSummary',
  async () => {
    const response = await apiClient.get('/orders/dashboard');
    return response.data as DashboardSummary;
  }
);

export const fetchDistributionSuggestion = createAsyncThunk(
  'orders/fetchDistributionSuggestion',
  async (customerOrderId: string) => {
    const response = await apiClient.get(`/procurement/suggestions?customerOrderId=${customerOrderId}`);
    return response.data as DistributionSuggestion;
  }
);

export const createPurchaseOrders = createAsyncThunk(
  'orders/createPurchaseOrders',
  async (params: { customerOrderId: string; distributionPlan: DistributionPlan }) => {
    const response = await apiClient.post('/procurement/distribute', params);
    return response.data as PurchaseOrder[];
  }
);

export const updateOrderStatus = createAsyncThunk(
  'orders/updateOrderStatus',
  async (params: { orderId: string; status: OrderStatus }) => {
    const response = await apiClient.put(`/orders/${params.orderId}/status`, { status: params.status });
    return response.data as CustomerOrder;
  }
);

const ordersSlice = createSlice({
  name: 'orders',
  initialState,
  reducers: {
    setFilter: (state, action: PayloadAction<OrderFilter>) => {
      state.filter = { ...state.filter, ...action.payload };
    },
    clearFilter: (state) => {
      state.filter = {};
    },
    setSelectedOrder: (state, action: PayloadAction<CustomerOrder | null>) => {
      state.selectedOrder = action.payload;
    },
    clearDistributionSuggestion: (state) => {
      state.distributionSuggestion = null;
    },
    updateOrderStatusRealtime: (state, action: PayloadAction<{ orderId: string; status: OrderStatus }>) => {
      const order = state.orders.find(o => o.id === action.payload.orderId);
      if (order) {
        order.status = action.payload.status;
      }
      if (state.selectedOrder && state.selectedOrder.id === action.payload.orderId) {
        state.selectedOrder.status = action.payload.status;
      }
    },
    setPagination: (state, action: PayloadAction<{ pageNumber: number; pageSize: number }>) => {
      state.pagination.pageNumber = action.payload.pageNumber;
      state.pagination.pageSize = action.payload.pageSize;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch orders
      .addCase(fetchOrders.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchOrders.fulfilled, (state, action) => {
        state.loading = false;
        state.orders = action.payload.items;
        state.pagination = {
          pageNumber: action.payload.pageNumber,
          pageSize: action.payload.pageSize,
          totalCount: action.payload.totalCount,
          totalPages: action.payload.totalPages,
        };
      })
      .addCase(fetchOrders.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch orders';
      })
      // Fetch dashboard summary
      .addCase(fetchDashboardSummary.pending, (state) => {
        state.dashboardLoading = true;
      })
      .addCase(fetchDashboardSummary.fulfilled, (state, action) => {
        state.dashboardLoading = false;
        state.dashboardSummary = action.payload;
      })
      .addCase(fetchDashboardSummary.rejected, (state, action) => {
        state.dashboardLoading = false;
        state.error = action.error.message || 'Failed to fetch dashboard summary';
      })
      // Fetch distribution suggestion
      .addCase(fetchDistributionSuggestion.pending, (state) => {
        state.distributionLoading = true;
      })
      .addCase(fetchDistributionSuggestion.fulfilled, (state, action) => {
        state.distributionLoading = false;
        state.distributionSuggestion = action.payload;
      })
      .addCase(fetchDistributionSuggestion.rejected, (state, action) => {
        state.distributionLoading = false;
        state.error = action.error.message || 'Failed to fetch distribution suggestion';
      })
      // Create purchase orders
      .addCase(createPurchaseOrders.fulfilled, (state, action) => {
        state.purchaseOrders = [...state.purchaseOrders, ...action.payload];
        // Update the order status to indicate purchase orders were created
        if (state.selectedOrder) {
          state.selectedOrder.status = 'PurchaseOrdersCreated';
        }
      })
      // Update order status
      .addCase(updateOrderStatus.fulfilled, (state, action) => {
        const index = state.orders.findIndex(o => o.id === action.payload.id);
        if (index !== -1) {
          state.orders[index] = action.payload;
        }
        if (state.selectedOrder && state.selectedOrder.id === action.payload.id) {
          state.selectedOrder = action.payload;
        }
      });
  },
});

export const { 
  setFilter, 
  clearFilter, 
  setSelectedOrder, 
  clearDistributionSuggestion,
  updateOrderStatusRealtime,
  setPagination
} = ordersSlice.actions;

export default ordersSlice.reducer;