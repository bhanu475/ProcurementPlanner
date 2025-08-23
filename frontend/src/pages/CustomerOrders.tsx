import React, { useState } from 'react';
import OrderSubmissionForm from '../components/Customer/OrderSubmissionForm';
import OrderHistory from '../components/Customer/OrderHistory';
import OrderTracking from '../components/Customer/OrderTracking';

type ViewMode = 'history' | 'submit' | 'tracking';

const CustomerOrders: React.FC = () => {
  const [currentView, setCurrentView] = useState<ViewMode>('history');
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);

  const handleSubmitSuccess = () => {
    setCurrentView('history');
  };

  const handleViewOrder = (orderId: string) => {
    setSelectedOrderId(orderId);
    setCurrentView('tracking');
  };

  const handleTrackOrder = (orderId: string) => {
    setSelectedOrderId(orderId);
    setCurrentView('tracking');
  };

  const renderContent = () => {
    switch (currentView) {
      case 'submit':
        return (
          <OrderSubmissionForm
            onSuccess={handleSubmitSuccess}
            onCancel={() => setCurrentView('history')}
          />
        );
      case 'tracking':
        return selectedOrderId ? (
          <OrderTracking
            orderId={selectedOrderId}
            onClose={() => setCurrentView('history')}
          />
        ) : null;
      case 'history':
      default:
        return (
          <OrderHistory
            onViewOrder={handleViewOrder}
            onTrackOrder={handleTrackOrder}
          />
        );
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 p-4 sm:p-6 lg:p-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 space-y-4 sm:space-y-0">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">My Orders</h1>
            <p className="text-gray-600 mt-1">Manage your orders and track their progress</p>
          </div>
          
          {/* Navigation Buttons */}
          <div className="flex flex-wrap gap-2">
            <button
              onClick={() => setCurrentView('history')}
              className={`px-4 py-2 rounded-md font-medium transition-colors ${
                currentView === 'history'
                  ? 'bg-blue-500 text-white'
                  : 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50'
              }`}
            >
              Order History
            </button>
            <button
              onClick={() => setCurrentView('submit')}
              className={`px-4 py-2 rounded-md font-medium transition-colors ${
                currentView === 'submit'
                  ? 'bg-blue-500 text-white'
                  : 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50'
              }`}
            >
              Submit New Order
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="transition-all duration-300 ease-in-out">
          {renderContent()}
        </div>
      </div>
    </div>
  );
};

export default CustomerOrders;