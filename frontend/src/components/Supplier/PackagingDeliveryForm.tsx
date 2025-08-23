import React, { useState } from 'react';
import { PurchaseOrderItem } from '../../types';

interface PackagingDeliveryFormProps {
  items: PurchaseOrderItem[];
  onSubmit: (packagingData: { [itemId: string]: { packagingDetails: string; deliveryMethod: string; estimatedDeliveryDate: string } }) => void;
  onCancel: () => void;
  loading: boolean;
}

interface PackagingFormData {
  packagingDetails: string;
  deliveryMethod: string;
  estimatedDeliveryDate: string;
}

const PackagingDeliveryForm: React.FC<PackagingDeliveryFormProps> = ({
  items,
  onSubmit,
  onCancel,
  loading
}) => {
  const [formData, setFormData] = useState<{ [itemId: string]: PackagingFormData }>(() => {
    const initialData: { [itemId: string]: PackagingFormData } = {};
    items.forEach(item => {
      initialData[item.id] = {
        packagingDetails: item.packagingDetails || '',
        deliveryMethod: item.deliveryMethod || '',
        estimatedDeliveryDate: item.estimatedDeliveryDate || ''
      };
    });
    return initialData;
  });

  const [errors, setErrors] = useState<{ [itemId: string]: { [field: string]: string } }>({});

  const handleInputChange = (itemId: string, field: keyof PackagingFormData, value: string) => {
    setFormData(prev => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        [field]: value
      }
    }));

    // Clear error when user starts typing
    if (errors[itemId]?.[field]) {
      setErrors(prev => ({
        ...prev,
        [itemId]: {
          ...prev[itemId],
          [field]: ''
        }
      }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: { [itemId: string]: { [field: string]: string } } = {};
    let hasErrors = false;

    items.forEach(item => {
      const itemData = formData[item.id];
      const itemErrors: { [field: string]: string } = {};

      if (!itemData.packagingDetails.trim()) {
        itemErrors.packagingDetails = 'Packaging details are required';
        hasErrors = true;
      }

      if (!itemData.deliveryMethod.trim()) {
        itemErrors.deliveryMethod = 'Delivery method is required';
        hasErrors = true;
      }

      if (!itemData.estimatedDeliveryDate) {
        itemErrors.estimatedDeliveryDate = 'Estimated delivery date is required';
        hasErrors = true;
      } else {
        const deliveryDate = new Date(itemData.estimatedDeliveryDate);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        
        if (deliveryDate < today) {
          itemErrors.estimatedDeliveryDate = 'Delivery date cannot be in the past';
          hasErrors = true;
        }
      }

      if (Object.keys(itemErrors).length > 0) {
        newErrors[item.id] = itemErrors;
      }
    });

    setErrors(newErrors);
    return !hasErrors;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  const deliveryMethods = [
    'Standard Shipping',
    'Express Shipping',
    'Overnight Delivery',
    'Local Pickup',
    'Direct Delivery',
    'Freight'
  ];

  return (
    <div className="mt-6 p-4 border border-blue-200 rounded-lg bg-blue-50">
      <h5 className="font-semibold mb-4">Packaging and Delivery Details</h5>
      
      <form onSubmit={handleSubmit} className="space-y-6">
        {items.map((item, index) => (
          <div key={item.id} className="bg-white p-4 rounded-lg border">
            <h6 className="font-medium mb-3">
              Item {index + 1} - Quantity: {item.allocatedQuantity}
            </h6>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {/* Packaging Details */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Packaging Details *
                </label>
                <textarea
                  value={formData[item.id]?.packagingDetails || ''}
                  onChange={(e) => handleInputChange(item.id, 'packagingDetails', e.target.value)}
                  className={`w-full p-2 border rounded-md text-sm ${
                    errors[item.id]?.packagingDetails ? 'border-red-300' : 'border-gray-300'
                  } focus:outline-none focus:ring-2 focus:ring-blue-500`}
                  rows={3}
                  placeholder="e.g., Cardboard boxes, 10 units per box, bubble wrap protection"
                />
                {errors[item.id]?.packagingDetails && (
                  <p className="text-red-600 text-xs mt-1">{errors[item.id].packagingDetails}</p>
                )}
              </div>

              {/* Delivery Method */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Delivery Method *
                </label>
                <select
                  value={formData[item.id]?.deliveryMethod || ''}
                  onChange={(e) => handleInputChange(item.id, 'deliveryMethod', e.target.value)}
                  className={`w-full p-2 border rounded-md text-sm ${
                    errors[item.id]?.deliveryMethod ? 'border-red-300' : 'border-gray-300'
                  } focus:outline-none focus:ring-2 focus:ring-blue-500`}
                >
                  <option value="">Select delivery method</option>
                  {deliveryMethods.map(method => (
                    <option key={method} value={method}>{method}</option>
                  ))}
                </select>
                {errors[item.id]?.deliveryMethod && (
                  <p className="text-red-600 text-xs mt-1">{errors[item.id].deliveryMethod}</p>
                )}
              </div>

              {/* Estimated Delivery Date */}
              <div>
                <label htmlFor={`delivery-date-${item.id}`} className="block text-sm font-medium text-gray-700 mb-1">
                  Estimated Delivery Date *
                </label>
                <input
                  id={`delivery-date-${item.id}`}
                  type="date"
                  value={formData[item.id]?.estimatedDeliveryDate || ''}
                  onChange={(e) => handleInputChange(item.id, 'estimatedDeliveryDate', e.target.value)}
                  className={`w-full p-2 border rounded-md text-sm ${
                    errors[item.id]?.estimatedDeliveryDate ? 'border-red-300' : 'border-gray-300'
                  } focus:outline-none focus:ring-2 focus:ring-blue-500`}
                  min={new Date().toISOString().split('T')[0]}
                />
                {errors[item.id]?.estimatedDeliveryDate && (
                  <p className="text-red-600 text-xs mt-1">{errors[item.id].estimatedDeliveryDate}</p>
                )}
              </div>
            </div>
          </div>
        ))}

        {/* Form Actions */}
        <div className="flex justify-end space-x-4 pt-4 border-t">
          <button
            type="button"
            onClick={onCancel}
            disabled={loading}
            className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={loading}
            className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
          >
            {loading ? 'Confirming...' : 'Confirm Order'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default PackagingDeliveryForm;