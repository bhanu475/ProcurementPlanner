import React, { useState } from 'react';
import { OrderFilter, ProductType, OrderStatus } from '../../types';

interface OrderFiltersProps {
  filter: OrderFilter;
  onFilterChange: (filter: Partial<OrderFilter>) => void;
  onClearFilters: () => void;
}

const OrderFilters: React.FC<OrderFiltersProps> = ({ filter, onFilterChange, onClearFilters }) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const productTypes: ProductType[] = ['LMR', 'FFV'];
  const orderStatuses: OrderStatus[] = [
    'Submitted',
    'UnderReview',
    'PlanningInProgress',
    'PurchaseOrdersCreated',
    'AwaitingSupplierConfirmation',
    'InProduction',
    'ReadyForDelivery',
    'Delivered',
    'Cancelled'
  ];

  const handleStatusChange = (status: OrderStatus, checked: boolean) => {
    const currentStatuses = filter.status || [];
    const newStatuses = checked
      ? [...currentStatuses, status]
      : currentStatuses.filter(s => s !== status);
    
    onFilterChange({ status: newStatuses.length > 0 ? newStatuses : undefined });
  };

  const handleProductTypeChange = (productType: ProductType, checked: boolean) => {
    const currentTypes = filter.productType || [];
    const newTypes = checked
      ? [...currentTypes, productType]
      : currentTypes.filter(t => t !== productType);
    
    onFilterChange({ productType: newTypes.length > 0 ? newTypes : undefined });
  };

  const hasActiveFilters = Object.keys(filter).some(key => {
    const value = filter[key as keyof OrderFilter];
    return value !== undefined && value !== '' && (Array.isArray(value) ? value.length > 0 : true);
  });

  return (
    <div className="bg-white p-6 rounded-lg shadow-sm border">
      <div className="flex justify-between items-center mb-4">
        <h3 className="text-lg font-semibold text-gray-900">Filters</h3>
        <div className="flex items-center space-x-2">
          {hasActiveFilters && (
            <button
              onClick={onClearFilters}
              className="text-sm text-red-600 hover:text-red-800 font-medium"
            >
              Clear All
            </button>
          )}
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="text-sm text-blue-600 hover:text-blue-800 font-medium"
          >
            {isExpanded ? 'Collapse' : 'Expand'}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
        {/* Search Term */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Search
          </label>
          <input
            type="text"
            value={filter.searchTerm || ''}
            onChange={(e) => onFilterChange({ searchTerm: e.target.value || undefined })}
            placeholder="Order number, customer..."
            className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Customer ID */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Customer ID (DODAAC)
          </label>
          <input
            type="text"
            value={filter.customerId || ''}
            onChange={(e) => onFilterChange({ customerId: e.target.value || undefined })}
            placeholder="Enter DODAAC"
            className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Start Date */}
        <div>
          <label htmlFor="start-date" className="block text-sm font-medium text-gray-700 mb-1">
            Start Date
          </label>
          <input
            id="start-date"
            type="date"
            value={filter.startDate || ''}
            onChange={(e) => onFilterChange({ startDate: e.target.value || undefined })}
            className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* End Date */}
        <div>
          <label htmlFor="end-date" className="block text-sm font-medium text-gray-700 mb-1">
            End Date
          </label>
          <input
            id="end-date"
            type="date"
            value={filter.endDate || ''}
            onChange={(e) => onFilterChange({ endDate: e.target.value || undefined })}
            className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      {isExpanded && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 pt-4 border-t border-gray-200">
          {/* Product Types */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Product Types
            </label>
            <div className="space-y-2">
              {productTypes.map(productType => (
                <label key={productType} className="flex items-center">
                  <input
                    type="checkbox"
                    checked={filter.productType?.includes(productType) || false}
                    onChange={(e) => handleProductTypeChange(productType, e.target.checked)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                  />
                  <span className="ml-2 text-sm text-gray-700">{productType}</span>
                </label>
              ))}
            </div>
          </div>

          {/* Order Statuses */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Order Status
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto">
              {orderStatuses.map(status => (
                <label key={status} className="flex items-center">
                  <input
                    type="checkbox"
                    checked={filter.status?.includes(status) || false}
                    onChange={(e) => handleStatusChange(status, e.target.checked)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                  />
                  <span className="ml-2 text-sm text-gray-700">
                    {status.replace(/([A-Z])/g, ' $1').trim()}
                  </span>
                </label>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default OrderFilters;