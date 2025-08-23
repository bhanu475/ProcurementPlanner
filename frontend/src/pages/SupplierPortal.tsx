import React from 'react';

const SupplierPortal: React.FC = () => {
  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Supplier Portal</h1>
      <div className="bg-white p-6 rounded-lg shadow">
        <h2 className="text-lg font-semibold mb-4">Purchase Orders</h2>
        <p className="text-gray-600">No purchase orders available at the moment.</p>
      </div>
    </div>
  );
};

export default SupplierPortal;