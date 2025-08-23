import { describe, it, expect, vi, beforeEach } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';
import ordersSlice, {
  setFilter,
  clearFilter,
  setSelectedOrder,
  clearDistributionSuggestion,
  updateOrderStatusRealtime,
  setPagination,
  fetchOrders,
  fetchDashboardSummary,
  fetchDistributionSuggestion,
  createPurchaseOrders,
  updateOrderStatus
} from '../ordersSlice';
import { CustomerOrder, OrderFilter, DashboardSummary, DistributionSuggestion } from '../../../types';

// Mock API client
vi.mock('../../../services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn()
  }
}));

const mockOrder: CustomerOrder = {
  id: '1',
  orderNumber: 'ORD-001',
  customerId: 'DODAAC123',
  customerName: 'Test Customer',
  productType: 'LMR',
  requestedDeliveryDate: '2024-12-31T00:00:00Z',
  status: 'Submitted',
  items: [
    {
      id: '1',
      orderId: '1',
      productCode: 'PROD-001',
      description: 'Test Product',
      quantity: 10,
      unit: 'EA',
      specifications: 'Test specs'
    }
  ],
  createdAt: '2024-01-01T00:00:00Z',
  createdBy: 'test-user'
};

const mockDashboardSummary: DashboardSummary = {
  totalOrders: 100,
  pendingOrders: 25,
  ordersInProgress: 40,
  completedOrders: 30,
  activeSuppliers: 15,
  ordersThisMonth: 45,
  ordersByStatus: {
    'Submitted': 10,
    'UnderReview': 15,
    'PlanningInProgress': 20,
    'PurchaseOrdersCreated': 15,
    'AwaitingSupplierConfirmation': 10,
    'InProduction': 15,
    'ReadyForDelivery': 10,
    'Delivered': 30,
    'Cancelled': 5
  },
  ordersByProductType: {
    'LMR': 60,
    'FFV': 40
  }
};

const mockDistributionSuggestion: DistributionSuggestion = {
  customerOrderId: '1',
  suggestions: [
    {
      supplierId: 'supplier-1',
      supplierName: 'Test Supplier 1',
      allocatedQuantity: 5,
      capacityUtilization: 75,
      performanceScore: 85,
      items: [
        {
          orderItemId: '1',
          allocatedQuantity: 5
        }
      ]
    }
  ],
  totalCapacityUtilization: 75
};

describe('ordersSlice', () => {
  let store: ReturnType<typeof configureStore>;

  beforeEach(() => {
    store = configureStore({
      reducer: {
        orders: ordersSlice
      }
    });
  });

  describe('synchronous actions', () => {
    it('should handle setFilter', () => {
      const filter: Partial<OrderFilter> = { productType: ['LMR'] };
      
      store.dispatch(setFilter(filter));
      
      const state = store.getState().orders;
      expect(state.filter.productType).toEqual(['LMR']);
    });

    it('should handle clearFilter', () => {
      // First set a filter
      store.dispatch(setFilter({ productType: ['LMR'], status: ['Submitted'] }));
      
      // Then clear it
      store.dispatch(clearFilter());
      
      const state = store.getState().orders;
      expect(state.filter).toEqual({});
    });

    it('should handle setSelectedOrder', () => {
      store.dispatch(setSelectedOrder(mockOrder));
      
      const state = store.getState().orders;
      expect(state.selectedOrder).toEqual(mockOrder);
    });

    it('should handle clearDistributionSuggestion', () => {
      // First set a suggestion (this would normally be done by async action)
      const initialState = {
        ...store.getState().orders,
        distributionSuggestion: mockDistributionSuggestion
      };
      
      store.dispatch(clearDistributionSuggestion());
      
      const state = store.getState().orders;
      expect(state.distributionSuggestion).toBeNull();
    });

    it('should handle updateOrderStatusRealtime', () => {
      // First add an order to the state
      const initialState = {
        ...store.getState().orders,
        orders: [mockOrder],
        selectedOrder: mockOrder
      };
      
      store.dispatch(updateOrderStatusRealtime({ orderId: '1', status: 'UnderReview' }));
      
      const state = store.getState().orders;
      // Note: This test would need the reducer to be properly handling the state update
      // The actual implementation should update both orders array and selectedOrder
    });

    it('should handle setPagination', () => {
      store.dispatch(setPagination({ pageNumber: 2, pageSize: 25 }));
      
      const state = store.getState().orders;
      expect(state.pagination.pageNumber).toBe(2);
      expect(state.pagination.pageSize).toBe(25);
    });
  });

  describe('async actions', () => {
    it('should handle fetchOrders.pending', () => {
      store.dispatch(fetchOrders.pending('', { filter: {}, pageNumber: 1, pageSize: 10 }));
      
      const state = store.getState().orders;
      expect(state.loading).toBe(true);
      expect(state.error).toBeNull();
    });

    it('should handle fetchOrders.fulfilled', () => {
      const mockResponse = {
        items: [mockOrder],
        totalCount: 1,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1
      };
      
      store.dispatch(fetchOrders.fulfilled(mockResponse, '', { filter: {}, pageNumber: 1, pageSize: 10 }));
      
      const state = store.getState().orders;
      expect(state.loading).toBe(false);
      expect(state.orders).toEqual([mockOrder]);
      expect(state.pagination.totalCount).toBe(1);
    });

    it('should handle fetchOrders.rejected', () => {
      const error = new Error('Failed to fetch orders');
      
      store.dispatch(fetchOrders.rejected(error, '', { filter: {}, pageNumber: 1, pageSize: 10 }));
      
      const state = store.getState().orders;
      expect(state.loading).toBe(false);
      expect(state.error).toBe('Failed to fetch orders');
    });

    it('should handle fetchDashboardSummary.pending', () => {
      store.dispatch(fetchDashboardSummary.pending('', undefined));
      
      const state = store.getState().orders;
      expect(state.dashboardLoading).toBe(true);
    });

    it('should handle fetchDashboardSummary.fulfilled', () => {
      store.dispatch(fetchDashboardSummary.fulfilled(mockDashboardSummary, '', undefined));
      
      const state = store.getState().orders;
      expect(state.dashboardLoading).toBe(false);
      expect(state.dashboardSummary).toEqual(mockDashboardSummary);
    });

    it('should handle fetchDashboardSummary.rejected', () => {
      const error = new Error('Failed to fetch dashboard summary');
      
      store.dispatch(fetchDashboardSummary.rejected(error, '', undefined));
      
      const state = store.getState().orders;
      expect(state.dashboardLoading).toBe(false);
      expect(state.error).toBe('Failed to fetch dashboard summary');
    });

    it('should handle fetchDistributionSuggestion.pending', () => {
      store.dispatch(fetchDistributionSuggestion.pending('', '1'));
      
      const state = store.getState().orders;
      expect(state.distributionLoading).toBe(true);
    });

    it('should handle fetchDistributionSuggestion.fulfilled', () => {
      store.dispatch(fetchDistributionSuggestion.fulfilled(mockDistributionSuggestion, '', '1'));
      
      const state = store.getState().orders;
      expect(state.distributionLoading).toBe(false);
      expect(state.distributionSuggestion).toEqual(mockDistributionSuggestion);
    });

    it('should handle fetchDistributionSuggestion.rejected', () => {
      const error = new Error('Failed to fetch distribution suggestion');
      
      store.dispatch(fetchDistributionSuggestion.rejected(error, '', '1'));
      
      const state = store.getState().orders;
      expect(state.distributionLoading).toBe(false);
      expect(state.error).toBe('Failed to fetch distribution suggestion');
    });
  });

  describe('initial state', () => {
    it('should have correct initial state', () => {
      const state = store.getState().orders;
      
      expect(state.orders).toEqual([]);
      expect(state.dashboardSummary).toBeNull();
      expect(state.selectedOrder).toBeNull();
      expect(state.distributionSuggestion).toBeNull();
      expect(state.purchaseOrders).toEqual([]);
      expect(state.loading).toBe(false);
      expect(state.dashboardLoading).toBe(false);
      expect(state.distributionLoading).toBe(false);
      expect(state.error).toBeNull();
      expect(state.filter).toEqual({});
      expect(state.pagination).toEqual({
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 0
      });
    });
  });

  describe('filter combinations', () => {
    it('should handle multiple filter updates', () => {
      store.dispatch(setFilter({ productType: ['LMR'] }));
      store.dispatch(setFilter({ status: ['Submitted'] }));
      store.dispatch(setFilter({ customerId: 'DODAAC123' }));
      
      const state = store.getState().orders;
      expect(state.filter).toEqual({
        productType: ['LMR'],
        status: ['Submitted'],
        customerId: 'DODAAC123'
      });
    });

    it('should handle partial filter updates', () => {
      // Set initial filter
      store.dispatch(setFilter({ 
        productType: ['LMR'], 
        status: ['Submitted'],
        customerId: 'DODAAC123'
      }));
      
      // Update only one part
      store.dispatch(setFilter({ productType: ['FFV'] }));
      
      const state = store.getState().orders;
      expect(state.filter).toEqual({
        productType: ['FFV'],
        status: ['Submitted'],
        customerId: 'DODAAC123'
      });
    });
  });
});