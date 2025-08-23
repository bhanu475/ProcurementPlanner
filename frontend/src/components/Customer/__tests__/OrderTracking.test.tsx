import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import OrderTracking from '../OrderTracking';
import customerOrdersSlice from '../../../store/slices/customerOrdersSlice';
import { OrderTrackingResponse } from '../../../services/customerApi';

// Mock the API service
vi.mock('../../../services/customerApi', () => ({
  customerApiService: {
    getOrderTracking: vi.fn(),
  },
}));

const mockOrderTracking: OrderTrackingResponse = {
  order: {
    id: 'order-1',
    orderNumber: 'ORD-001',
    customerId: 'customer-1',
    customerName: 'Test Customer',
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
      status: 'UnderReview',
      description: 'Order under review',
      timestamp: '2024-01-02T00:00:00Z',
      details: 'Order is being reviewed by procurement team',
    },
    {
      id: 'event-3',
      orderId: 'order-1',
      status: 'InProduction',
      description: 'Order in production',
      timestamp: '2024-01-03T00:00:00Z',
      details: 'Order is currently being produced',
    },
  ],
  estimatedDeliveryDate: '2024-12-25',
};

const createMockStore = (initialState = {}) => {
  return configureStore({
    reducer: {
      customerOrders: customerOrdersSlice,
    },
    preloadedState: {
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

const renderWithProvider = (component: React.ReactElement, store = createMockStore()) => {
  return render(
    <Provider store={store}>
      {component}
    </Provider>
  );
};

describe('OrderTracking', () => {
  const mockOnClose = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    const storeWithLoading = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: null,
        orderTracking: null,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: true,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithLoading);

    // Should show loading skeleton
    expect(document.querySelector('.animate-pulse')).toBeInTheDocument();
  });

  it('displays error state when tracking fails', async () => {
    const user = userEvent.setup();
    const storeWithError = createMockStore({
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
        error: 'Failed to load order tracking',
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithError);

    expect(screen.getByText('Error Loading Order')).toBeInTheDocument();
    expect(screen.getByText('Failed to load order tracking')).toBeInTheDocument();
    
    const tryAgainButton = screen.getByText('Try Again');
    expect(tryAgainButton).toBeInTheDocument();
    
    await user.click(tryAgainButton);
    // Should attempt to retry loading
  });

  it('displays order tracking information when loaded', () => {
    const storeWithTracking = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: mockOrderTracking.order,
        orderTracking: mockOrderTracking,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithTracking);

    // Check header
    expect(screen.getByText('Order Tracking')).toBeInTheDocument();
    expect(screen.getByText('Order #ORD-001')).toBeInTheDocument();

    // Check order summary
    expect(screen.getByText('Test Customer')).toBeInTheDocument();
    expect(screen.getByText('LMR')).toBeInTheDocument();
    expect(screen.getByText('InProduction')).toBeInTheDocument();

    // Check estimated delivery date
    expect(screen.getByText(/Estimated Delivery:/)).toBeInTheDocument();
    expect(screen.getByText(/December 25, 2024/)).toBeInTheDocument();

    // Check order items table
    expect(screen.getByText('Order Items')).toBeInTheDocument();
    expect(screen.getByText('TEST123')).toBeInTheDocument();
    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('10')).toBeInTheDocument();
    expect(screen.getByText('each')).toBeInTheDocument();
    expect(screen.getByText('Special requirements')).toBeInTheDocument();

    // Check timeline
    expect(screen.getByText('Order Timeline')).toBeInTheDocument();
    expect(screen.getByText('Order submitted')).toBeInTheDocument();
    expect(screen.getByText('Order under review')).toBeInTheDocument();
    expect(screen.getByText('Order in production')).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', async () => {
    const user = userEvent.setup();
    const storeWithTracking = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: mockOrderTracking.order,
        orderTracking: mockOrderTracking,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithTracking);

    const closeButton = screen.getByRole('button', { name: /close/i });
    await user.click(closeButton);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it('displays timeline events in chronological order', () => {
    const storeWithTracking = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: mockOrderTracking.order,
        orderTracking: mockOrderTracking,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithTracking);

    const timelineEvents = screen.getAllByText(/January \d+, 2024/);
    expect(timelineEvents).toHaveLength(3);
    
    // Events should be displayed with timestamps
    expect(screen.getByText(/January 1, 2024/)).toBeInTheDocument();
    expect(screen.getByText(/January 2, 2024/)).toBeInTheDocument();
    expect(screen.getByText(/January 3, 2024/)).toBeInTheDocument();
  });

  it('shows appropriate status colors for different order statuses', () => {
    const storeWithTracking = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: mockOrderTracking.order,
        orderTracking: mockOrderTracking,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithTracking);

    // Check that status indicators have appropriate colors
    const statusIndicators = document.querySelectorAll('.rounded-full');
    expect(statusIndicators.length).toBeGreaterThan(0);
    
    // Should have different colored status indicators for different statuses
    const blueIndicator = document.querySelector('.bg-blue-500');
    const yellowIndicator = document.querySelector('.bg-yellow-500');
    const cyanIndicator = document.querySelector('.bg-cyan-500');
    
    expect(blueIndicator).toBeInTheDocument(); // Submitted
    expect(yellowIndicator).toBeInTheDocument(); // UnderReview
    expect(cyanIndicator).toBeInTheDocument(); // InProduction
  });

  it('handles orders without estimated delivery date', () => {
    const trackingWithoutEstimate = {
      ...mockOrderTracking,
      estimatedDeliveryDate: undefined,
    };

    const storeWithTracking = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: trackingWithoutEstimate.order,
        orderTracking: trackingWithoutEstimate,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithTracking);

    // Should not show estimated delivery section
    expect(screen.queryByText(/Estimated Delivery:/)).not.toBeInTheDocument();
  });

  it('handles orders with no specifications', () => {
    const orderWithoutSpecs = {
      ...mockOrderTracking,
      order: {
        ...mockOrderTracking.order,
        items: [
          {
            ...mockOrderTracking.order.items[0],
            specifications: '',
          },
        ],
      },
    };

    const storeWithTracking = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: orderWithoutSpecs.order,
        orderTracking: orderWithoutSpecs,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderTracking orderId="order-1" onClose={mockOnClose} />, storeWithTracking);

    // Should still display the item without specifications
    expect(screen.getByText('TEST123')).toBeInTheDocument();
    expect(screen.getByText('Test Product')).toBeInTheDocument();
  });
});