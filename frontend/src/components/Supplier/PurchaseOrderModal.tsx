import React, { useState } from 'react';
import { PurchaseOrder, PurchaseOrderItem } from '../../types';
import { supplierApi } from '../../services/supplierApi';
import PackagingDeliveryForm from './PackagingDeliveryForm';

interface PurchaseOrderModalProps {
  purchaseOrder: PurchaseOrder;
  onClose: () => void;
  onUpdate: (updatedOrder: PurchaseOrder) => void;
}

const PurchaseOrderModal: React.FC<PurchaseOrderModalProps> = ({
  purchaseOrder,
  onClose,
  onUpdate
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPackagingForm, setShowPackagingForm] = useState(false);
  const [rejectionReason, setRejectionReason] = useState('');
  const [showRejectionForm, setShowRejectionForm] = useState(false);

  const handleConfirmOrder = async (packagingData?: { [itemId: string]: { packagingDetails: string; deliveryMethod: string; estimatedDeliveryDate: string } }) => {
    try {
      setLoading(true);
      setError(null);

      const confirmationData = {
        status: 'Confirmed' as const,
        supplierNotes: 'Order confirmed by supplier',
        items: packagingData ? purchaseOrder.items.map(item => ({
          ...item,
          packagingDetails: packagingData[item.id]?.packagingDetails || '',
          deliveryMethod: packagingData[item.id]?.deliveryMethod || '',
          estimatedDeliveryDate: packagingData[item.id]?.estimatedDeliveryDate || ''
        })) : purchaseOrder.items
      };

      const response = await supplierApi.confirmPurchaseOrder(purchaseOrder.id, confirmationData);
      onUpdate(response.data);
    } catch (err) {
      setError('Failed to confirm purchase order');
      console.error('Confirmation error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleRejectOrder = async () => {
    if (!rejectionReason.trim()) {
      setError('Please provide a reason for rejection');
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const rejectionData = {
        status: 'Rejected' as const,
        supplierNotes: rejectionReason
      };

      const response = await supplierApi.confirmPurchaseOrder(purchaseOrder.id, rejectionData);
      onUpdate(response.data);
    } catch (err) {
      setError('Failed to reject purchase order');
      console.error('Rejection error:', err);
    } finally {
      setLoading(false);
    }
  };

  const canConfirmOrReject = purchaseOrder.status === 'SentToSupplier';
  const totalQuantity = purchaseOrder.items.reduce((sum, item) => sum + item.allocatedQuantity, 0);

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-11/12 max-w-4xl shadow-lg rounded-md bg-white">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <h3 className="text-lg font-semibold text-gray-900">
            Purchase Order Details - {purchaseOrder.purchaseOrderNumber}
          </h3>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
            aria-label="Close modal"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {error && (
          <div className="mb-4 bg-red-50 border border-red-200 rounded-md p-4">
            <p className="text-red-800">{error}</p>
          </div>
        )}

        {/* Order Information */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
          <div className="bg-gray-50 p-4 rounded-lg">
            <h4 className="font-semibold mb-2">Order Information</h4>
            <div className="space-y-2 text-sm">
              <div><span className="font-medium">Status:</span> 
                <span className={`ml-2 px-2 py-1 rounded-full text-xs ${
                  purchaseOrder.status === 'SentToSupplier' ? 'bg-orange-100 text-orange-800' :
                  purchaseOrder.status === 'Confirmed' ? 'bg-green-100 text-green-800' :
                  purchaseOrder.status === 'Rejected' ? 'bg-red-100 text-red-800' :
                  'bg-gray-100 text-gray-800'
                }`}>
                  {purchaseOrder.status}
                </span>
              </div>
              <div><span className="font-medium">Required Delivery:</span> {new Date(purchaseOrder.requiredDeliveryDate).toLocaleDateString()}</div>
              <div><span className="font-medium">Created:</span> {new Date(purchaseOrder.createdAt).toLocaleDateString()}</div>
              <div><span className="font-medium">Total Items:</span> {totalQuantity}</div>
            </div>
          </div>

          {purchaseOrder.confirmedAt && (
            <div className="bg-green-50 p-4 rounded-lg">
              <h4 className="font-semibold mb-2">Confirmation Details</h4>
              <div className="space-y-2 text-sm">
                <div><span className="font-medium">Confirmed:</span> {new Date(purchaseOrder.confirmedAt).toLocaleDateString()}</div>
                {purchaseOrder.supplierNotes && (
                  <div><span className="font-medium">Notes:</span> {purchaseOrder.supplierNotes}</div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Order Items */}
        <div className="mb-6">
          <h4 className="font-semibold mb-4">Order Items</h4>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Product Code
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Quantity
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Packaging
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Delivery Method
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Est. Delivery
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {purchaseOrder.items.map((item) => (
                  <tr key={item.id}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {item.orderItemId}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {item.allocatedQuantity}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {item.packagingDetails || '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {item.deliveryMethod || '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {item.estimatedDeliveryDate ? new Date(item.estimatedDeliveryDate).toLocaleDateString() : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Action Buttons */}
        {canConfirmOrReject && (
          <div className="flex justify-end space-x-4">
            <button
              onClick={() => setShowRejectionForm(true)}
              disabled={loading}
              className="px-4 py-2 border border-red-300 text-red-700 rounded-md hover:bg-red-50 disabled:opacity-50"
            >
              Reject Order
            </button>
            <button
              onClick={() => setShowPackagingForm(true)}
              disabled={loading}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
            >
              {loading ? 'Processing...' : 'Confirm Order'}
            </button>
          </div>
        )}

        {/* Rejection Form */}
        {showRejectionForm && (
          <div className="mt-6 p-4 border border-red-200 rounded-lg bg-red-50">
            <h5 className="font-semibold mb-2">Reason for Rejection</h5>
            <textarea
              value={rejectionReason}
              onChange={(e) => setRejectionReason(e.target.value)}
              className="w-full p-2 border border-gray-300 rounded-md"
              rows={3}
              placeholder="Please provide a reason for rejecting this order..."
            />
            <div className="flex justify-end space-x-2 mt-4">
              <button
                onClick={() => setShowRejectionForm(false)}
                className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleRejectOrder}
                disabled={loading}
                className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {loading ? 'Rejecting...' : 'Confirm Rejection'}
              </button>
            </div>
          </div>
        )}

        {/* Packaging and Delivery Form */}
        {showPackagingForm && (
          <PackagingDeliveryForm
            items={purchaseOrder.items}
            onSubmit={handleConfirmOrder}
            onCancel={() => setShowPackagingForm(false)}
            loading={loading}
          />
        )}
      </div>
    </div>
  );
};

export default PurchaseOrderModal;