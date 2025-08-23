import React from 'react';
import { CustomerOrder } from '../../types';

interface OrderReviewModalProps {
  order: CustomerOrder;
  onClose: () => void;
  onStartDistribution: () => void;
}

const OrderReviewModal: React.FC<OrderReviewModalProps> = ({
  order,
  onClose,
  onStartDistribution
}) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getStatusColor = (status: string) => {
    const statusColors: { [key: string]: string } = {
      'Submitted': 'bg-blue-100 text-blue-800',
      'UnderReview': 'bg-yellow-100 text-yellow-800',
      'PlanningInProgress': 'bg-orange-100 text-orange-800',
      'PurchaseOrdersCreated': 'bg-purple-100 text-purple-800',
      'AwaitingSupplierConfirmation': 'bg-indigo-100 text-indigo-800',
      'InProduction': 'bg-cyan-100 text-cyan-800',
      'ReadyForDelivery': 'bg-green-100 text-green-800',
      'Delivered': 'bg-gray-100 text-gray-800',
      'Cancelled': 'bg-red-100 text-red-800'
    };
    return statusColors[status] || 'bg-gray-100 text-gray-800';
  };

  const canStartDistribution = ['Submitted', 'UnderReview'].includes(order.status);

  const totalQuantity = order.items.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-11/12 max-w-4xl shadow-lg rounded-md bg-white">
        <div className="flex justify-between items-center mb-6">
          <h3 className="text-2xl font-bold text-gray-900">Order Review</h3>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-2xl font-bold"
          >
            ×
          </button>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Order Information */}
          <div className="space-y-6">
            <div className="bg-gray-50 p-4 rounded-lg">
              <h4 className="text-lg font-semibold text-gray-900 mb-3">Order Information</h4>
              <div className="space-y-2">
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">Order Number:</span>
                  <span className="text-sm text-gray-900">{order.orderNumber}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">Product Type:</span>
                  <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                    order.productType === 'LMR' 
                      ? 'bg-blue-100 text-blue-800' 
                      : 'bg-green-100 text-green-800'
                  }`}>
                    {order.productType}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">Status:</span>
                  <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusColor(order.status)}`}>
                    {order.status.replace(/([A-Z])/g, ' $1').trim()}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">Created:</span>
                  <span className="text-sm text-gray-900">{formatDate(order.createdAt)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">Created By:</span>
                  <span className="text-sm text-gray-900">{order.createdBy}</span>
                </div>
              </div>
            </div>

            <div className="bg-gray-50 p-4 rounded-lg">
              <h4 className="text-lg font-semibold text-gray-900 mb-3">Customer Information</h4>
              <div className="space-y-2">
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">Customer Name:</span>
                  <span className="text-sm text-gray-900">{order.customerName}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">DODAAC:</span>
                  <span className="text-sm text-gray-900">{order.customerId}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm font-medium text-gray-600">Requested Delivery:</span>
                  <span className="text-sm text-gray-900">{formatDate(order.requestedDeliveryDate)}</span>
                </div>
              </div>
            </div>
          </div>

          {/* Order Items */}
          <div>
            <div className="bg-gray-50 p-4 rounded-lg">
              <div className="flex justify-between items-center mb-3">
                <h4 className="text-lg font-semibold text-gray-900">Order Items</h4>
                <span className="text-sm text-gray-600">
                  {order.items.length} item{order.items.length !== 1 ? 's' : ''} • Total: {totalQuantity} units
                </span>
              </div>
              
              <div className="max-h-96 overflow-y-auto">
                <div className="space-y-3">
                  {order.items.map((item, index) => (
                    <div key={item.id} className="bg-white p-3 rounded border">
                      <div className="flex justify-between items-start mb-2">
                        <div className="flex-1">
                          <h5 className="font-medium text-gray-900">{item.productCode}</h5>
                          <p className="text-sm text-gray-600">{item.description}</p>
                        </div>
                        <div className="text-right ml-4">
                          <div className="text-sm font-medium text-gray-900">
                            {item.quantity} {item.unit}
                          </div>
                        </div>
                      </div>
                      
                      {item.specifications && (
                        <div className="mt-2 p-2 bg-gray-50 rounded text-xs">
                          <span className="font-medium text-gray-700">Specifications:</span>
                          <p className="text-gray-600 mt-1">{item.specifications}</p>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex justify-end space-x-3 mt-6 pt-6 border-t border-gray-200">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Close
          </button>
          
          {canStartDistribution && (
            <button
              onClick={onStartDistribution}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              Start Distribution Planning
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default OrderReviewModal;