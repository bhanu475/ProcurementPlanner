import { Supplier } from '../models/Supplier';

const mockSuppliers: Supplier[] = [
  { id: '1', name: 'Supplier A', email: 'suppliera@example.com', phoneNumber: '123-456-7890', address: '123 Main St' },
  { id: '2', name: 'Supplier B', email: 'supplierb@example.com', phoneNumber: '234-567-8901', address: '456 Oak Ave' },
  { id: '3', name: 'Supplier C', email: 'supplierc@example.com', phoneNumber: '345-678-9012', address: '789 Pine Ln' },
];

export const getSuppliers = (): Promise<Supplier[]> => {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(mockSuppliers);
    }, 500); // Simulate network delay
  });
};
