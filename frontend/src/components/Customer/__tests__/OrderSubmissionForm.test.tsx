import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import OrderSubmissionForm from '../OrderSubmissionForm';
import customerOrdersSlice from '../../../store/slices/customerOrdersSlice';
import authSlice from '../../../store/slices/authSlice';

// Mock the API service
vi.mock('../../../services/customerApi', () => ({
  customerApiService: {
    submitOrder: vi.fn(),
  },
}));

const createMockStore = (initialState = {}) => {
  return configureStore({
    reducer: {
      customerOrders: customerOrdersSlice,
      auth: authSlice,
    },
    preloadedState: {
      auth: {
        user: {
          id: 'user-1',
          name: 'Test Customer',
          email: 'test@example.com',
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

const renderWithProvider = (component: React.ReactElement, store = createMockStore()) => {
  return render(
    <Provider store={store}>
      {component}
    </Provider>
  );
};

describe('OrderSubmissionForm', () => {
  const mockOnSuccess = vi.fn();
  const mockOnCancel = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the form with all required fields', () => {
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    expect(screen.getByText('Submit New Order')).toBeInTheDocument();
    expect(screen.getByLabelText(/customer name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/product type/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/requested delivery date/i)).toBeInTheDocument();
    expect(screen.getByText('Order Items')).toBeInTheDocument();
    expect(screen.getByText('Add Item')).toBeInTheDocument();
    expect(screen.getByText('Submit Order')).toBeInTheDocument();
  });

  it('pre-fills customer information from user data', () => {
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    const customerNameInput = screen.getByLabelText(/customer name/i) as HTMLInputElement;
    expect(customerNameInput.value).toBe('Test Customer');
  });

  it('validates required fields', async () => {
    const user = userEvent.setup();
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    // Clear customer name
    const customerNameInput = screen.getByLabelText(/customer name/i);
    await user.clear(customerNameInput);

    // Try to submit without filling required fields
    const submitButton = screen.getByText('Submit Order');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Customer name is required')).toBeInTheDocument();
      expect(screen.getByText('Delivery date is required')).toBeInTheDocument();
      expect(screen.getByText('Product code is required')).toBeInTheDocument();
      expect(screen.getByText('Description is required')).toBeInTheDocument();
    });
  });

  it('validates delivery date is in the future', async () => {
    const user = userEvent.setup();
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    const deliveryDateInput = screen.getByLabelText(/requested delivery date/i);
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    const yesterdayString = yesterday.toISOString().split('T')[0];

    await user.type(deliveryDateInput, yesterdayString);

    const submitButton = screen.getByText('Submit Order');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Delivery date must be in the future')).toBeInTheDocument();
    });
  });

  it('validates quantity is greater than 0', async () => {
    const user = userEvent.setup();
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    const quantityInput = screen.getByDisplayValue('1');
    await user.clear(quantityInput);
    await user.type(quantityInput, '0');

    const submitButton = screen.getByText('Submit Order');
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Quantity must be greater than 0')).toBeInTheDocument();
    });
  });

  it('allows adding and removing order items', async () => {
    const user = userEvent.setup();
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    // Initially should have one item
    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.queryByText('Item 2')).not.toBeInTheDocument();

    // Add an item
    const addItemButton = screen.getByText('Add Item');
    await user.click(addItemButton);

    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 2')).toBeInTheDocument();

    // Remove the second item
    const removeButtons = screen.getAllByText('Remove');
    await user.click(removeButtons[1]);

    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.queryByText('Item 2')).not.toBeInTheDocument();
  });

  it('does not allow removing the last item', async () => {
    const user = userEvent.setup();
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    // Should not show remove button when there's only one item
    expect(screen.queryByText('Remove')).not.toBeInTheDocument();

    // Add an item to get a remove button
    const addItemButton = screen.getByText('Add Item');
    await user.click(addItemButton);

    // Now should have remove buttons
    expect(screen.getAllByText('Remove')).toHaveLength(2);

    // Remove one item
    const removeButtons = screen.getAllByText('Remove');
    await user.click(removeButtons[0]);

    // Should be back to no remove buttons
    expect(screen.queryByText('Remove')).not.toBeInTheDocument();
  });

  it('submits form with valid data', async () => {
    const user = userEvent.setup();
    const store = createMockStore();
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />, store);

    // Fill in the form
    const deliveryDateInput = screen.getByLabelText(/requested delivery date/i);
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const tomorrowString = tomorrow.toISOString().split('T')[0];
    await user.type(deliveryDateInput, tomorrowString);

    const productCodeInput = screen.getByPlaceholderText('e.g., ABC123');
    await user.type(productCodeInput, 'TEST123');

    const descriptionInput = screen.getByPlaceholderText('Enter item description');
    await user.type(descriptionInput, 'Test Product Description');

    const specificationsInput = screen.getByPlaceholderText('Enter any special specifications or requirements');
    await user.type(specificationsInput, 'Special requirements');

    // Submit the form
    const submitButton = screen.getByText('Submit Order');
    await user.click(submitButton);

    // Should dispatch the submit action
    await waitFor(() => {
      const state = store.getState();
      expect(state.customerOrders.submitting).toBe(false);
    });
  });

  it('calls onCancel when cancel button is clicked', async () => {
    const user = userEvent.setup();
    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />);

    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    expect(mockOnCancel).toHaveBeenCalledTimes(1);
  });

  it('displays error message when submission fails', () => {
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
        error: 'Failed to submit order',
        filter: {},
      },
    });

    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />, storeWithError);

    expect(screen.getByText('Failed to submit order')).toBeInTheDocument();
  });

  it('disables submit button when submitting', () => {
    const storeWithSubmitting = createMockStore({
      customerOrders: {
        orders: [],
        currentOrder: null,
        orderTracking: null,
        totalCount: 0,
        currentPage: 1,
        pageSize: 10,
        totalPages: 0,
        loading: false,
        submitting: true,
        error: null,
        filter: {},
      },
    });

    renderWithProvider(<OrderSubmissionForm onSuccess={mockOnSuccess} onCancel={mockOnCancel} />, storeWithSubmitting);

    const submitButton = screen.getByText('Submitting...');
    expect(submitButton).toBeDisabled();
  });
});