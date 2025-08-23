import React from 'react';

const CustomerOrders: React.FC = () => {
  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">My Orders</h1>
      <div className="bg-white p-6 rounded-lg shadow">
        <h2 className="text-lg font-semibold mb-4">Order History</h2>
        <p className="text-gray-600">No orders found.</p>
      </div>
    </div>
  );
};

export default CustomerOrders;