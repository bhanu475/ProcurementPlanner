import React, { useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState, AppDispatch } from '../../store';
import { submitOrder } from '../../store/slices/customerOrdersSlice';
import { CreateOrderRequest, CreateOrderItemRequest } from '../../services/customerApi';

interface OrderSubmissionFormProps {
  onSuccess?: () => void;
  onCancel?: () => void;
}

const OrderSubmissionForm: React.FC<OrderSubmissionFormProps> = ({ onSuccess, onCancel }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { user } = useSelector((state: RootState) => state.auth);
  const { submitting, error } = useSelector((state: RootState) => state.customerOrders);

  const [formData, setFormData] = useState<CreateOrderRequest>({
    customerId: user?.id || '',
    customerName: user?.name || '',
    productType: 'LMR',
    requestedDeliveryDate: '',
    items: [
      {
        productCode: '',
        description: '',
        quantity: 1,
        unit: 'each',
        specifications: '',
      },
    ],
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.customerName.trim()) {
      newErrors.customerName = 'Customer name is required';
    }

    if (!formData.requestedDeliveryDate) {
      newErrors.requestedDeliveryDate = 'Delivery date is required';
    } else {
      const deliveryDate = new Date(formData.requestedDeliveryDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      
      if (deliveryDate <= today) {
        newErrors.requestedDeliveryDate = 'Delivery date must be in the future';
      }
    }

    formData.items.forEach((item, index) => {
      if (!item.productCode.trim()) {
        newErrors[`items.${index}.productCode`] = 'Product code is required';
      }
      if (!item.description.trim()) {
        newErrors[`items.${index}.description`] = 'Description is required';
      }
      if (item.quantity <= 0) {
        newErrors[`items.${index}.quantity`] = 'Quantity must be greater than 0';
      }
      if (!item.unit.trim()) {
        newErrors[`items.${index}.unit`] = 'Unit is required';
      }
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    try {
      await dispatch(submitOrder(formData)).unwrap();
      onSuccess?.();
    } catch (error) {
      // Error is handled by the slice
    }
  };

  const handleInputChange = (field: keyof CreateOrderRequest, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const handleItemChange = (index: number, field: keyof CreateOrderItemRequest, value: unknown) => {
    const newItems = [...formData.items];
    newItems[index] = { ...newItems[index], [field]: value };
    setFormData(prev => ({ ...prev, items: newItems }));
    
    // Clear error when user starts typing
    const errorKey = `items.${index}.${field}`;
    if (errors[errorKey]) {
      setErrors(prev => ({ ...prev, [errorKey]: '' }));
    }
  };

  const addItem = () => {
    setFormData(prev => ({
      ...prev,
      items: [
        ...prev.items,
        {
          productCode: '',
          description: '',
          quantity: 1,
          unit: 'each',
          specifications: '',
        },
      ],
    }));
  };

  const removeItem = (index: number) => {
    if (formData.items.length > 1) {
      setFormData(prev => ({
        ...prev,
        items: prev.items.filter((_, i) => i !== index),
      }));
    }
  };

  return (
    <div className="bg-white p-6 rounded-lg shadow-md">
      <h2 className="text-2xl font-bold mb-6">Submit New Order</h2>
      
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Customer Information */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="customerName" className="block text-sm font-medium text-gray-700 mb-2">
              Customer Name *
            </label>
            <input
              id="customerName"
              type="text"
              value={formData.customerName}
              onChange={(e) => handleInputChange('customerName', e.target.value)}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                errors.customerName ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="Enter customer name"
            />
            {errors.customerName && (
              <p className="text-red-500 text-sm mt-1">{errors.customerName}</p>
            )}
          </div>

          <div>
            <label htmlFor="productType" className="block text-sm font-medium text-gray-700 mb-2">
              Product Type *
            </label>
            <select
              id="productType"
              value={formData.productType}
              onChange={(e) => handleInputChange('productType', e.target.value as 'LMR' | 'FFV')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="LMR">LMR</option>
              <option value="FFV">FFV</option>
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="requestedDeliveryDate" className="block text-sm font-medium text-gray-700 mb-2">
            Requested Delivery Date *
          </label>
          <input
            id="requestedDeliveryDate"
            type="date"
            value={formData.requestedDeliveryDate}
            onChange={(e) => handleInputChange('requestedDeliveryDate', e.target.value)}
            className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
              errors.requestedDeliveryDate ? 'border-red-500' : 'border-gray-300'
            }`}
          />
          {errors.requestedDeliveryDate && (
            <p className="text-red-500 text-sm mt-1">{errors.requestedDeliveryDate}</p>
          )}
        </div>

        {/* Order Items */}
        <div>
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-medium text-gray-900">Order Items</h3>
            <button
              type="button"
              onClick={addItem}
              className="bg-blue-500 text-white px-4 py-2 rounded-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              Add Item
            </button>
          </div>

          {formData.items.map((item, index) => (
            <div key={index} className="border border-gray-200 rounded-md p-4 mb-4">
              <div className="flex justify-between items-center mb-3">
                <h4 className="font-medium text-gray-900">Item {index + 1}</h4>
                {formData.items.length > 1 && (
                  <button
                    type="button"
                    onClick={() => removeItem(index)}
                    className="text-red-500 hover:text-red-700 focus:outline-none"
                  >
                    Remove
                  </button>
                )}
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <div>
                  <label htmlFor={`productCode-${index}`} className="block text-sm font-medium text-gray-700 mb-1">
                    Product Code *
                  </label>
                  <input
                    id={`productCode-${index}`}
                    type="text"
                    value={item.productCode}
                    onChange={(e) => handleItemChange(index, 'productCode', e.target.value)}
                    className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                      errors[`items.${index}.productCode`] ? 'border-red-500' : 'border-gray-300'
                    }`}
                    placeholder="e.g., ABC123"
                  />
                  {errors[`items.${index}.productCode`] && (
                    <p className="text-red-500 text-sm mt-1">{errors[`items.${index}.productCode`]}</p>
                  )}
                </div>

                <div>
                  <label htmlFor={`quantity-${index}`} className="block text-sm font-medium text-gray-700 mb-1">
                    Quantity *
                  </label>
                  <input
                    id={`quantity-${index}`}
                    type="number"
                    min="1"
                    value={item.quantity}
                    onChange={(e) => handleItemChange(index, 'quantity', parseInt(e.target.value) || 0)}
                    className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                      errors[`items.${index}.quantity`] ? 'border-red-500' : 'border-gray-300'
                    }`}
                  />
                  {errors[`items.${index}.quantity`] && (
                    <p className="text-red-500 text-sm mt-1">{errors[`items.${index}.quantity`]}</p>
                  )}
                </div>

                <div>
                  <label htmlFor={`unit-${index}`} className="block text-sm font-medium text-gray-700 mb-1">
                    Unit *
                  </label>
                  <select
                    id={`unit-${index}`}
                    value={item.unit}
                    onChange={(e) => handleItemChange(index, 'unit', e.target.value)}
                    className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                      errors[`items.${index}.unit`] ? 'border-red-500' : 'border-gray-300'
                    }`}
                  >
                    <option value="each">Each</option>
                    <option value="kg">Kilogram</option>
                    <option value="lb">Pound</option>
                    <option value="box">Box</option>
                    <option value="case">Case</option>
                    <option value="pallet">Pallet</option>
                  </select>
                  {errors[`items.${index}.unit`] && (
                    <p className="text-red-500 text-sm mt-1">{errors[`items.${index}.unit`]}</p>
                  )}
                </div>
              </div>

              <div className="mt-4">
                <label htmlFor={`description-${index}`} className="block text-sm font-medium text-gray-700 mb-1">
                  Description *
                </label>
                <input
                  id={`description-${index}`}
                  type="text"
                  value={item.description}
                  onChange={(e) => handleItemChange(index, 'description', e.target.value)}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                    errors[`items.${index}.description`] ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="Enter item description"
                />
                {errors[`items.${index}.description`] && (
                  <p className="text-red-500 text-sm mt-1">{errors[`items.${index}.description`]}</p>
                )}
              </div>

              <div className="mt-4">
                <label htmlFor={`specifications-${index}`} className="block text-sm font-medium text-gray-700 mb-1">
                  Specifications
                </label>
                <textarea
                  id={`specifications-${index}`}
                  value={item.specifications || ''}
                  onChange={(e) => handleItemChange(index, 'specifications', e.target.value)}
                  rows={2}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Enter any special specifications or requirements"
                />
              </div>
            </div>
          ))}
        </div>

        {/* Form Actions */}
        <div className="flex justify-end space-x-4 pt-6 border-t">
          {onCancel && (
            <button
              type="button"
              onClick={onCancel}
              className="px-6 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-500"
              disabled={submitting}
            >
              Cancel
            </button>
          )}
          <button
            type="submit"
            disabled={submitting}
            className="px-6 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {submitting ? 'Submitting...' : 'Submit Order'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default OrderSubmissionForm;