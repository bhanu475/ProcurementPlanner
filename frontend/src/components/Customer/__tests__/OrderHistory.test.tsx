import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import OrderHistory from '../OrderHistory';
import customerOrdersSlice from '../../../store/slices/customerOrdersSlice';
import { CustomerOrder } from '../../../types';

// Mock the API service
vi.mock('../../../services/customerApi', () => ({
  customerApiService: {
    getMyOrders: vi.fn(),
  },
}));

const mockOrders: CustomerOrder[] = [
  {
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
  {
    id: 'order-2',
    orderNumber: 'ORD-002',
    customerId: 'customer-1',
    customerName: 'Test Customer',
    productType: 'FFV',
    requestedDeliveryDate: '2024-11-30',
    status: 'Delivered',
    items: [
      {
        id: 'item-2',
        orderId: 'order-2',
        productCode: 'TEST456',
        description: 'Another Test Product',
        quantity: 5,
        unit: 'kg',
        specifications: '',
      },
    ],
    createdAt: '2023-12-01T00:00:00Z',
    createdBy: 'customer-1',
  },
];

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

describe('OrderHistory', () => {
  const mockOnViewOrder = vi.fn();
  const mockOnTrackOrder = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the order history header', () => {
    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />
    );

    expect(screen.getByText('Order History')).toBeInTheDocument();
    expect(screen.getByText('0 total orders')).toBeInTheDocument();
  });

  it('shows loading state', () => {
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

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      storeWithLoading
    );

    expect(document.querySelector('.animate-pulse')).toBeInTheDocument();
  });

  it('displays error message', () => {
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
        error: 'Failed to load orders',
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      storeWithError
    );

    expect(screen.getByText('Failed to load orders')).toBeInTheDocument();
  });

  it('displays empty state when no orders', () => {
    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />
    );

    expect(screen.getByText('No orders found')).toBeInTheDocument();
    expect(screen.getByText("You haven't placed any orders yet or no orders match your current filters.")).toBeInTheDocument();
  });

  it('displays orders when available', () => {
    const storeWithOrders = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      storeWithOrders
    );

    expect(screen.getByText('2 total orders')).toBeInTheDocument();
    expect(screen.getByText('Order #ORD-001')).toBeInTheDocument();
    expect(screen.getByText('Order #ORD-002')).toBeInTheDocument();
    expect(screen.getByText('InProduction')).toBeInTheDocument();
    expect(screen.getByText('Delivered')).toBeInTheDocument();
  });

  it('calls onViewOrder when View Details button is clicked', async () => {
    const user = userEvent.setup();
    const storeWithOrders = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      storeWithOrders
    );

    const viewButtons = screen.getAllByText('View Details');
    await user.click(viewButtons[0]);

    expect(mockOnViewOrder).toHaveBeenCalledWith('order-1');
  });

  it('calls onTrackOrder when Track Order button is clicked', async () => {
    const user = userEvent.setup();
    const storeWithOrders = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      storeWithOrders
    );

    const trackButtons = screen.getAllByText('Track Order');
    await user.click(trackButtons[0]);

    expect(mockOnTrackOrder).toHaveBeenCalledWith('order-1');
  });

  it('allows filtering by status', async () => {
    const user = userEvent.setup();
    const store = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      store
    );

    const statusSelect = screen.getByLabelText('Status');
    await user.selectOptions(statusSelect, ['InProduction']);

    const applyButton = screen.getByText('Apply Filters');
    await user.click(applyButton);

    // Should dispatch filter action
    await waitFor(() => {
      const state = store.getState();
      expect(state.customerOrders.filter.status).toEqual(['InProduction']);
    });
  });

  it('allows filtering by product type', async () => {
    const user = userEvent.setup();
    const store = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      store
    );

    const productTypeSelect = screen.getByLabelText('Product Type');
    await user.selectOptions(productTypeSelect, ['LMR']);

    const applyButton = screen.getByText('Apply Filters');
    await user.click(applyButton);

    await waitFor(() => {
      const state = store.getState();
      expect(state.customerOrders.filter.productType).toEqual(['LMR']);
    });
  });

  it('allows filtering by date range', async () => {
    const user = userEvent.setup();
    const store = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      store
    );

    const startDateInput = screen.getByLabelText('Start Date');
    const endDateInput = screen.getByLabelText('End Date');

    await user.type(startDateInput, '2024-01-01');
    await user.type(endDateInput, '2024-12-31');

    const applyButton = screen.getByText('Apply Filters');
    await user.click(applyButton);

    await waitFor(() => {
      const state = store.getState();
      expect(state.customerOrders.filter.startDate).toBe('2024-01-01');
      expect(state.customerOrders.filter.endDate).toBe('2024-12-31');
    });
  });

  it('allows searching by text', async () => {
    const user = userEvent.setup();
    const store = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      store
    );

    const searchInput = screen.getByPlaceholderText(/search by order number/i);
    await user.type(searchInput, 'ORD-001');

    const applyButton = screen.getByText('Apply Filters');
    await user.click(applyButton);

    await waitFor(() => {
      const state = store.getState();
      expect(state.customerOrders.filter.searchTerm).toBe('ORD-001');
    });
  });

  it('clears filters when Clear Filters is clicked', async () => {
    const user = userEvent.setup();
    const store = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {
          status: ['InProduction'],
          productType: ['LMR'],
          searchTerm: 'test',
        },
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      store
    );

    const clearButton = screen.getByText('Clear Filters');
    await user.click(clearButton);

    await waitFor(() => {
      const state = store.getState();
      expect(state.customerOrders.filter).toEqual({});
    });
  });

  it('displays pagination when there are multiple pages', () => {
    const storeWithPagination = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 25,
        currentPage: 2,
        pageSize: 10,
        totalPages: 3,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      storeWithPagination
    );

    expect(screen.getByText('Showing 11 to 20 of 25 results')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('handles pagination navigation', async () => {
    const user = userEvent.setup();
    const store = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 25,
        currentPage: 2,
        pageSize: 10,
        totalPages: 3,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      store
    );

    const page3Button = screen.getByText('3');
    await user.click(page3Button);

    // Should dispatch action to fetch page 3
    // This would be tested by checking if the appropriate action was dispatched
  });

  it('displays correct status badge colors', () => {
    const storeWithOrders = createMockStore({
      customerOrders: {
        orders: mockOrders,
        currentOrder: null,
        orderTracking: null,
        totalCount: 2,
        currentPage: 1,
        pageSize: 10,
        totalPages: 1,
        loading: false,
        submitting: false,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(
      <OrderHistory onViewOrder={mockOnViewOrder} onTrackOrder={mockOnTrackOrder} />,
      storeWithOrders
    );

    // Check that status badges have appropriate styling
    const inProductionBadge = screen.getByText('InProduction');
    const deliveredBadge = screen.getByText('Delivered');

    expect(inProductionBadge).toHaveClass('bg-cyan-100', 'text-cyan-800');
    expect(deliveredBadge).toHaveClass('bg-green-200', 'text-green-900');
  });
});