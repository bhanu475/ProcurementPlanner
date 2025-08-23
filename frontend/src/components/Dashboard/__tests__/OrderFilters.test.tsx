import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import OrderFilters from '../OrderFilters';
import { OrderFilter } from '../../../types';

const mockOnFilterChange = vi.fn();
const mockOnClearFilters = vi.fn();

const defaultProps = {
  filter: {} as OrderFilter,
  onFilterChange: mockOnFilterChange,
  onClearFilters: mockOnClearFilters
};

describe('OrderFilters', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders filter components correctly', () => {
    render(<OrderFilters {...defaultProps} />);
    
    expect(screen.getByText('Filters')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Order number, customer...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter DODAAC')).toBeInTheDocument();
    expect(screen.getByLabelText('Start Date')).toBeInTheDocument();
    expect(screen.getByLabelText('End Date')).toBeInTheDocument();
  });

  it('shows expand/collapse functionality', () => {
    render(<OrderFilters {...defaultProps} />);
    
    const expandButton = screen.getByText('Expand');
    expect(expandButton).toBeInTheDocument();
    
    fireEvent.click(expandButton);
    
    expect(screen.getByText('Collapse')).toBeInTheDocument();
    expect(screen.getByText('Product Types')).toBeInTheDocument();
    expect(screen.getByText('Order Status')).toBeInTheDocument();
  });

  it('handles search term input correctly', () => {
    render(<OrderFilters {...defaultProps} />);
    
    const searchInput = screen.getByPlaceholderText('Order number, customer...');
    fireEvent.change(searchInput, { target: { value: 'test search' } });
    
    expect(mockOnFilterChange).toHaveBeenCalledWith({ searchTerm: 'test search' });
  });

  it('handles customer ID input correctly', () => {
    render(<OrderFilters {...defaultProps} />);
    
    const customerInput = screen.getByPlaceholderText('Enter DODAAC');
    fireEvent.change(customerInput, { target: { value: 'DODAAC123' } });
    
    expect(mockOnFilterChange).toHaveBeenCalledWith({ customerId: 'DODAAC123' });
  });

  it('handles date inputs correctly', () => {
    render(<OrderFilters {...defaultProps} />);
    
    const startDateInput = screen.getByLabelText('Start Date');
    const endDateInput = screen.getByLabelText('End Date');
    
    fireEvent.change(startDateInput, { target: { value: '2024-01-01' } });
    fireEvent.change(endDateInput, { target: { value: '2024-12-31' } });
    
    expect(mockOnFilterChange).toHaveBeenCalledWith({ startDate: '2024-01-01' });
    expect(mockOnFilterChange).toHaveBeenCalledWith({ endDate: '2024-12-31' });
  });

  it('handles product type selection correctly', () => {
    render(<OrderFilters {...defaultProps} />);
    
    // Expand to show product types
    fireEvent.click(screen.getByText('Expand'));
    
    const lmrCheckbox = screen.getByLabelText('LMR');
    fireEvent.click(lmrCheckbox);
    
    expect(mockOnFilterChange).toHaveBeenCalledWith({ productType: ['LMR'] });
  });

  it('handles order status selection correctly', () => {
    render(<OrderFilters {...defaultProps} />);
    
    // Expand to show order statuses
    fireEvent.click(screen.getByText('Expand'));
    
    const submittedCheckbox = screen.getByLabelText('Submitted');
    fireEvent.click(submittedCheckbox);
    
    expect(mockOnFilterChange).toHaveBeenCalledWith({ status: ['Submitted'] });
  });

  it('shows clear all button when filters are active', () => {
    const filterWithData: OrderFilter = {
      searchTerm: 'test',
      customerId: 'DODAAC123'
    };
    
    render(<OrderFilters {...defaultProps} filter={filterWithData} />);
    
    expect(screen.getByText('Clear All')).toBeInTheDocument();
  });

  it('calls clear filters when clear all is clicked', () => {
    const filterWithData: OrderFilter = {
      searchTerm: 'test'
    };
    
    render(<OrderFilters {...defaultProps} filter={filterWithData} />);
    
    const clearButton = screen.getByText('Clear All');
    fireEvent.click(clearButton);
    
    expect(mockOnClearFilters).toHaveBeenCalled();
  });

  it('handles multiple product type selections correctly', () => {
    render(<OrderFilters {...defaultProps} />);
    
    // Expand to show product types
    fireEvent.click(screen.getByText('Expand'));
    
    const lmrCheckbox = screen.getByLabelText('LMR');
    const ffvCheckbox = screen.getByLabelText('FFV');
    
    fireEvent.click(lmrCheckbox);
    fireEvent.click(ffvCheckbox);
    
    expect(mockOnFilterChange).toHaveBeenCalledWith({ productType: ['LMR'] });
    expect(mockOnFilterChange).toHaveBeenCalledWith({ productType: ['FFV'] });
  });

  it('handles deselecting filters correctly', () => {
    const filterWithProductType: OrderFilter = {
      productType: ['LMR']
    };
    
    render(<OrderFilters {...defaultProps} filter={filterWithProductType} />);
    
    // Expand to show product types
    fireEvent.click(screen.getByText('Expand'));
    
    const lmrCheckbox = screen.getByLabelText('LMR');
    expect(lmrCheckbox).toBeChecked();
    
    fireEvent.click(lmrCheckbox);
    
    expect(mockOnFilterChange).toHaveBeenCalledWith({ productType: undefined });
  });
});