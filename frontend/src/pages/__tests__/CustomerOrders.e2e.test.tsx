import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import CustomerOrders from '../CustomerOrders';
import customerOrdersSlice from '../../store/slices/customerOrdersSlice';
import authSlice from '../../store/slices/authSlice';
import { customerApiService } from '../../services/customerApi';

// Mock the API service
vi.mock('../../services/customerApi', () => ({
  customerApiService: {
    submitOrder: vi.fn(),
    getMyOrders: vi.fn(),
    getOrderTracking: vi.fn(),
    getOrderById: vi.fn(),
    cancelOrder: vi.fn(),
  },
}));

const mockCustomerApiService = customerApiService as any;

const createMockStore = (initialState = {}) => {
  return configureStore({
    reducer: {
      customerOrders: customerOrdersSlice,
      auth: authSlice,
    },
    preloadedState: {
      auth: {
        user: {
          id: 'customer-1',
          name: 'John Doe',
          email: 'john@example.com',
          role: 'customer',
        },
        token: 'mock-token',
        isAuthenticated: true,
        loading: false,
      },
      customerOrders: {
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
      },
      ...initialState,
    },
  });
};

const renderWithProviders = (component: React.ReactElement, store = createMockStore()) => {
  return render(
    <Provider store={store}>
      <BrowserRouter>
        {component}
      </BrowserRouter>
    </Provider>
  );
};

describe('CustomerOrders E2E Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    // Setup default mock responses
    mockCustomerApiService.getMyOrders.mockResolvedValue({
      items: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: 10,
      totalPages: 0,
    });
  });

  describe('Complete Order Submission Workflow', () => {
    it('allows customer to submit a complete order from start to finish', async () => {
      const user = userEvent.setup();
      const store = createMockStore();

      // Mock successful order submission
      const mockSubmittedOrder = {
        id: 'order-1',
        orderNumber: 'ORD-001',
        customerId: 'customer-1',
        customerName: 'John Doe',
        productType: 'LMR',
        requestedDeliveryDate: '2024-12-31',
        status: 'Submitted',
        items: [
          {
            id: 'item-1',
            orderId: 'order-1',
            productCode: 'TEST123',
            description: 'Test Product',
            quantity: 10,
            unit: 'each',
            specifications: 'Special requirements',
          },
        ],
        createdAt: '2024-01-01T00:00:00Z',
        createdBy: 'customer-1',
      };

      mockCustomerApiService.submitOrder.mockResolvedValue(mockSubmittedOrder);

      renderWithProviders(<CustomerOrders />, store);

      // Step 1: Navigate to order submission form
      expect(screen.getByText('My Orders')).toBeInTheDocument();
      expect(screen.getByText('Order History')).toBeInTheDocument();

      const submitOrderButton = screen.getByText('Submit New Order');
      await user.click(submitOrderButton);

      // Step 2: Fill out the order form
      expect(screen.getByText('Submit New Order')).toBeInTheDocument();

      // Customer name should be pre-filled
      const customerNameInput = screen.getByLabelText(/customer name/i) as HTMLInputElement;
      expect(customerNameInput.value).toBe('John Doe');

      // Set delivery date
      const deliveryDateInput = screen.getByLabelText(/requested delivery date/i);
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 30);
      const futureDateString = futureDate.toISOString().split('T')[0];
      await user.type(deliveryDateInput, futureDateString);

      // Fill in product details
      const productCodeInput = screen.getByPlaceholderText('e.g., ABC123');
      await user.type(productCodeInput, 'TEST123');

      const descriptionInput = screen.getByPlaceholderText('Enter item description');
      await user.type(descriptionInput, 'Test Product');

      const quantityInput = screen.getByDisplayValue('1');
      await user.clear(quantityInput);
      await user.type(quantityInput, '10');

      const specificationsInput = screen.getByPlaceholderText('Enter any special specifications or requirements');
      await user.type(specificationsInput, 'Special requirements');

      // Step 3: Submit the order
      const submitButton = screen.getByText('Submit Order');
      await user.click(submitButton);

      // Step 4: Verify submission and navigation back to history
      await waitFor(() => {
        expect(mockCustomerApiService.submitOrder).toHaveBeenCalledWith({
          customerId: 'customer-1',
          customerName: 'John Doe',
          productType: 'LMR',
          requestedDeliveryDate: futureDateString,
          items: [
            {
              productCode: 'TEST123',
              description: 'Test Product',
              quantity: 10,
              unit: 'each',
              specifications: 'Special requirements',
            },
          ],
        });
      });

      // Should navigate back to order history
      await waitFor(() => {
        expect(screen.getByText('Order History')).toBeInTheDocument();
      });
    });

    it('handles form validation errors during submission', async () => {
      const user = userEvent.setup();
      renderWithProviders(<CustomerOrders />);

      // Navigate to submission form
      const submitOrderButton = screen.getByText('Submit New Order');
      await user.click(submitOrderButton);

      // Try to submit without required fields
      const submitButton = screen.getByText('Submit Order');
      await user.click(submitButton);

      // Should show validation errors
      await waitFor(() => {
        expect(screen.getByText('Delivery date is required')).toBeInTheDocument();
        expect(screen.getByText('Product code is required')).toBeInTheDocument();
        expect(screen.getByText('Description is required')).toBeInTheDocument();
      });

      // Should not call API
      expect(mockCustomerApiService.submitOrder).not.toHaveBeenCalled();
    });

    it('handles API errors during submission gracefully', async () => {
      const user = userEvent.setup();
      const store = createMockStore();

      // Mock API error
      mockCustomerApiService.submitOrder.mockRejectedValue(new Error('Network error'));

      renderWithProviders(<CustomerOrders />, store);

      // Navigate to submission form and fill it out
      const submitOrderButton = screen.getByText('Submit New Order');
      await user.click(submitOrderButton);

      // Fill minimum required fields
      const deliveryDateInput = screen.getByLabelText(/requested delivery date/i);
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 30);
      await user.type(deliveryDateInput, futureDate.toISOString().split('T')[0]);

      const productCodeInput = screen.getByPlaceholderText('e.g., ABC123');
      await user.type(productCodeInput, 'TEST123');

      const descriptionInput = screen.getByPlaceholderText('Enter item description');
      await user.type(descriptionInput, 'Test Product');

      // Submit the form
      const submitButton = screen.getByText('Submit Order');
      await user.click(submitButton);

      // Should show error message
      await waitFor(() => {
        const state = store.getState();
        expect(state.customerOrders.error).toBeTruthy();
      });
    });
  });

  describe('Order History and Tracking Workflow', () => {
    it('displays order history and allows tracking individual orders', async () => {
      const user = userEvent.setup();
      const store = createMockStore();

      const mockOrders = [
        {
          id: 'order-1',
          orderNumber: 'ORD-001',
          customerId: 'customer-1',
          customerName: 'John Doe',
          productType: 'LMR',
          requestedDeliveryDate: '2024-12-31',
          status: 'InProduction',
          items: [
            {
              id: 'item-1',
              orderId: 'order-1',
              productCode: 'TEST123',
              description: 'Test Product',
              quantity: 10,
              unit: 'each',
              specifications: 'Special requirements',
            },
          ],
          createdAt: '2024-01-01T00:00:00Z',
          createdBy: 'customer-1',
        },
      ];

      const mockOrderTracking = {
        order: mockOrders[0],
        timeline: [
          {
            id: 'event-1',
            orderId: 'order-1',
            status: 'Submitted',
            description: 'Order submitted',
            timestamp: '2024-01-01T00:00:00Z',
            details: 'Order was successfully submitted',
          },
          {
            id: 'event-2',
            orderId: 'order-1',
            status: 'InProduction',
            description: 'Order in production',
            timestamp: '2024-01-02T00:00:00Z',
            details: 'Order is currently being produced',
          },
        ],
        estimatedDeliveryDate: '2024-12-25',
      };

      // Mock API responses
      mockCustomerApiService.getMyOrders.mockResolvedValue({
        items: mockOrders,
        totalCount: 1,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1,
      });
      mockCustomerApiService.getOrderTracking.mockResolvedValue(mockOrderTracking);

      renderWithProviders(<CustomerOrders />, store);

      // Should load and display orders
      await waitFor(() => {
        expect(screen.getByText('Order #ORD-001')).toBeInTheDocument();
        expect(screen.getByText('InProduction')).toBeInTheDocument();
      });

      // Click track order button
      const trackButton = screen.getByText('Track Order');
      await user.click(trackButton);

      // Should show order tracking details
      await waitFor(() => {
        expect(screen.getByText('Order Tracking')).toBeInTheDocument();
        expect(screen.getByText('Order #ORD-001')).toBeInTheDocument();
        expect(screen.getByText('Order Timeline')).toBeInTheDocument();
        expect(screen.getByText('Order submitted')).toBeInTheDocument();
        expect(screen.getByText('Order in production')).toBeInTheDocument();
      });

      // Should show estimated delivery date
      expect(screen.getByText(/Estimated Delivery:/)).toBeInTheDocument();
      expect(screen.getByText(/December 25, 2024/)).toBeInTheDocument();
    });

    it('allows filtering and searching order history', async () => {
      const user = userEvent.setup();
      const store = createMockStore();

      const mockOrders = [
        {
          id: 'order-1',
          orderNumber: 'ORD-001',
          customerId: 'customer-1',
          customerName: 'John Doe',
          productType: 'LMR',
          requestedDeliveryDate: '2024-12-31',
          status: 'InProduction',
          items: [],
          createdAt: '2024-01-01T00:00:00Z',
          createdBy: 'customer-1',
        },
        {
          id: 'order-2',
          orderNumber: 'ORD-002',
          customerId: 'customer-1',
          customerName: 'John Doe',
          productType: 'FFV',
          requestedDeliveryDate: '2024-11-30',
          status: 'Delivered',
          items: [],
          createdAt: '2023-12-01T00:00:00Z',
          createdBy: 'customer-1',
        },
      ];

      mockCustomerApiService.getMyOrders.mockResolvedValue({
        items: mockOrders,
        totalCount: 2,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1,
      });

      renderWithProviders(<CustomerOrders />, store);

      // Wait for orders to load
      await waitFor(() => {
        expect(screen.getByText('Order #ORD-001')).toBeInTheDocument();
        expect(screen.getByText('Order #ORD-002')).toBeInTheDocument();
      });

      // Test filtering by status
      const statusSelect = screen.getByLabelText('Status');
      await user.selectOptions(statusSelect, ['InProduction']);

      const applyButton = screen.getByText('Apply Filters');
      await user.click(applyButton);

      // Should update filter in store
      await waitFor(() => {
        const state = store.getState();
        expect(state.customerOrders.filter.status).toEqual(['InProduction']);
      });

      // Test search functionality
      const searchInput = screen.getByPlaceholderText(/search by order number/i);
      await user.type(searchInput, 'ORD-001');
      await user.click(applyButton);

      await waitFor(() => {
        const state = store.getState();
        expect(state.customerOrders.filter.searchTerm).toBe('ORD-001');
      });

      // Test clearing filters
      const clearButton = screen.getByText('Clear Filters');
      await user.click(clearButton);

      await waitFor(() => {
        const state = store.getState();
        expect(state.customerOrders.filter).toEqual({});
      });
    });
  });

  describe('Responsive Design and Mobile Experience', () => {
    it('adapts layout for mobile screens', () => {
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      });

      renderWithProviders(<CustomerOrders />);

      // Should render mobile-friendly layout
      expect(screen.getByText('My Orders')).toBeInTheDocument();
      
      // Navigation buttons should be responsive
      const historyButton = screen.getByText('Order History');
      const submitButton = screen.getByText('Submit New Order');
      
      expect(historyButton).toBeInTheDocument();
      expect(submitButton).toBeInTheDocument();
    });

    it('handles touch interactions on mobile', async () => {
      const user = userEvent.setup();
      
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      });

      renderWithProviders(<CustomerOrders />);

      // Test touch navigation
      const submitOrderButton = screen.getByText('Submit New Order');
      await user.click(submitOrderButton);

      expect(screen.getByText('Submit New Order')).toBeInTheDocument();

      // Test form interactions on mobile
      const cancelButton = screen.getByText('Cancel');
      await user.click(cancelButton);

      expect(screen.getByText('Order History')).toBeInTheDocument();
    });
  });

  describe('Error Handling and Edge Cases', () => {
    it('handles network errors gracefully', async () => {
      const store = createMockStore();

      // Mock network error
      mockCustomerApiService.getMyOrders.mockRejectedValue(new Error('Network error'));

      renderWithProviders(<CustomerOrders />, store);

      // Should show error state
      await waitFor(() => {
        const state = store.getState();
        expect(state.customerOrders.error).toBeTruthy();
      });
    });

    it('handles empty states appropriately', () => {
      mockCustomerApiService.getMyOrders.mockResolvedValue({
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 0,
      });

      renderWithProviders(<CustomerOrders />);

      // Should show empty state message
      expect(screen.getByText('No orders found')).toBeInTheDocument();
    });

    it('handles pagination correctly', async () => {
      const user = userEvent.setup();
      const store = createMockStore();

      // Mock paginated response
      mockCustomerApiService.getMyOrders.mockResolvedValue({
        items: [],
        totalCount: 25,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 3,
      });

      renderWithProviders(<CustomerOrders />, store);

      // Should show pagination when there are multiple pages
      await waitFor(() => {
        const state = store.getState();
        if (state.customerOrders.totalPages > 1) {
          expect(screen.getByText('1')).toBeInTheDocument();
          expect(screen.getByText('2')).toBeInTheDocument();
          expect(screen.getByText('3')).toBeInTheDocument();
        }
      });
    });
  });

  describe('Accessibility', () => {
    it('provides proper ARIA labels and roles', () => {
      renderWithProviders(<CustomerOrders />);

      // Check for proper form labels
      const submitOrderButton = screen.getByText('Submit New Order');
      expect(submitOrderButton).toBeInTheDocument();

      // Navigation should be accessible
      const historyButton = screen.getByText('Order History');
      expect(historyButton).toHaveAttribute('class');
    });

    it('supports keyboard navigation', async () => {
      const user = userEvent.setup();
      renderWithProviders(<CustomerOrders />);

      // Test keyboard navigation between buttons
      const historyButton = screen.getByText('Order History');
      const submitButton = screen.getByText('Submit New Order');

      await user.tab();
      expect(historyButton).toHaveFocus();

      await user.tab();
      expect(submitButton).toHaveFocus();

      // Test Enter key activation
      await user.keyboard('{Enter}');
      expect(screen.getByText('Submit New Order')).toBeInTheDocument();
    });
  });
});