import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState, AppDispatch } from '../../store';
import { fetchOrderTracking, clearOrderTracking } from '../../store/slices/customerOrdersSlice';
import { OrderTimelineEvent } from '../../services/customerApi';
import { OrderStatus } from '../../types';

interface OrderTrackingProps {
  orderId: string;
  onClose?: () => void;
}

const OrderTracking: React.FC<OrderTrackingProps> = ({ orderId, onClose }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { orderTracking, loading, error } = useSelector((state: RootState) => state.customerOrders);

  useEffect(() => {
    if (orderId) {
      dispatch(fetchOrderTracking(orderId));
    }
    
    return () => {
      dispatch(clearOrderTracking());
    };
  }, [dispatch, orderId]);

  const getStatusColor = (status: OrderStatus): string => {
    switch (status) {
      case 'Submitted':
        return 'bg-blue-500';
      case 'UnderReview':
        return 'bg-yellow-500';
      case 'PlanningInProgress':
        return 'bg-orange-500';
      case 'PurchaseOrdersCreated':
        return 'bg-purple-500';
      case 'AwaitingSupplierConfirmation':
        return 'bg-indigo-500';
      case 'InProduction':
        return 'bg-cyan-500';
      case 'ReadyForDelivery':
        return 'bg-green-400';
      case 'Delivered':
        return 'bg-green-600';
      case 'Cancelled':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  };

  const getStatusDescription = (status: OrderStatus): string => {
    switch (status) {
      case 'Submitted':
        return 'Your order has been submitted and is awaiting review.';
      case 'UnderReview':
        return 'Your order is being reviewed by our procurement team.';
      case 'PlanningInProgress':
        return 'We are planning the procurement and supplier allocation.';
      case 'PurchaseOrdersCreated':
        return 'Purchase orders have been created and sent to suppliers.';
      case 'AwaitingSupplierConfirmation':
        return 'Waiting for supplier confirmation and delivery details.';
      case 'InProduction':
        return 'Your order is currently being produced by suppliers.';
      case 'ReadyForDelivery':
        return 'Your order is ready and will be delivered soon.';
      case 'Delivered':
        return 'Your order has been successfully delivered.';
      case 'Cancelled':
        return 'This order has been cancelled.';
      default:
        return 'Order status unknown.';
    }
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  if (loading) {
    return (
      <div className="bg-white p-6 rounded-lg shadow-md">
        <div className="animate-pulse">
          <div className="h-6 bg-gray-200 rounded mb-4"></div>
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex items-center space-x-4">
                <div className="w-4 h-4 bg-gray-200 rounded-full"></div>
                <div className="flex-1">
                  <div className="h-4 bg-gray-200 rounded mb-2"></div>
                  <div className="h-3 bg-gray-200 rounded w-3/4"></div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white p-6 rounded-lg shadow-md">
        <div className="text-center">
          <div className="text-red-500 mb-4">
            <svg className="w-12 h-12 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-lg font-medium text-gray-900 mb-2">Error Loading Order</h3>
          <p className="text-gray-600 mb-4">{error}</p>
          <button
            onClick={() => dispatch(fetchOrderTracking(orderId))}
            className="bg-blue-500 text-white px-4 py-2 rounded-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  if (!orderTracking) {
    return null;
  }

  const { order, timeline, estimatedDeliveryDate } = orderTracking;

  return (
    <div className="bg-white p-6 rounded-lg shadow-md">
      <div className="flex justify-between items-start mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Order Tracking</h2>
          <p className="text-gray-600">Order #{order.orderNumber}</p>
        </div>
        {onClose && (
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 focus:outline-none"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>

      {/* Order Summary */}
      <div className="bg-gray-50 p-4 rounded-lg mb-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div>
            <p className="text-sm font-medium text-gray-500">Customer</p>
            <p className="text-lg font-semibold text-gray-900">{order.customerName}</p>
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Product Type</p>
            <p className="text-lg font-semibold text-gray-900">{order.productType}</p>
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Requested Delivery</p>
            <p className="text-lg font-semibold text-gray-900">
              {new Date(order.requestedDeliveryDate).toLocaleDateString()}
            </p>
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Current Status</p>
            <div className="flex items-center space-x-2">
              <div className={`w-3 h-3 rounded-full ${getStatusColor(order.status)}`}></div>
              <p className="text-lg font-semibold text-gray-900">{order.status}</p>
            </div>
          </div>
        </div>
        
        {estimatedDeliveryDate && (
          <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-md">
            <p className="text-sm font-medium text-blue-800">
              Estimated Delivery: {new Date(estimatedDeliveryDate).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
              })}
            </p>
          </div>
        )}
      </div>

      {/* Order Items */}
      <div className="mb-6">
        <h3 className="text-lg font-medium text-gray-900 mb-3">Order Items</h3>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Product Code
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Description
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Quantity
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Unit
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {order.items.map((item) => (
                <tr key={item.id}>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {item.productCode}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-900">
                    {item.description}
                    {item.specifications && (
                      <p className="text-xs text-gray-500 mt-1">{item.specifications}</p>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {item.quantity}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {item.unit}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Timeline */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Order Timeline</h3>
        <div className="flow-root">
          <ul className="-mb-8">
            {timeline.map((event, eventIdx) => (
              <li key={event.id}>
                <div className="relative pb-8">
                  {eventIdx !== timeline.length - 1 ? (
                    <span
                      className="absolute top-4 left-4 -ml-px h-full w-0.5 bg-gray-200"
                      aria-hidden="true"
                    />
                  ) : null}
                  <div className="relative flex space-x-3">
                    <div>
                      <span className={`h-8 w-8 rounded-full flex items-center justify-center ring-8 ring-white ${getStatusColor(event.status as OrderStatus)}`}>
                        <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                        </svg>
                      </span>
                    </div>
                    <div className="min-w-0 flex-1 pt-1.5 flex justify-between space-x-4">
                      <div>
                        <p className="text-sm font-medium text-gray-900">{event.description}</p>
                        <p className="text-sm text-gray-500">{getStatusDescription(event.status as OrderStatus)}</p>
                        {event.details && (
                          <p className="text-sm text-gray-600 mt-1">{event.details}</p>
                        )}
                      </div>
                      <div className="text-right text-sm whitespace-nowrap text-gray-500">
                        <time dateTime={event.timestamp}>{formatDate(event.timestamp)}</time>
                      </div>
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
};

export default OrderTracking;