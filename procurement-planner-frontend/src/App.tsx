
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Provider } from 'react-redux';
import { store } from './store';
import Layout from './components/Layout/Layout';
import ProtectedRoute from './components/Auth/ProtectedRoute';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import SupplierPortal from './pages/SupplierPortal';
import CustomerOrders from './pages/CustomerOrders';
import Unauthorized from './pages/Unauthorized';

function App() {
  return (
    <Provider store={store}>
      <Router>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/unauthorized" element={<Unauthorized />} />
          
          <Route path="/" element={<Layout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            
            <Route
              path="/dashboard"
              element={
                <ProtectedRoute allowedRoles={['lmr_planner']}>
                  <Dashboard />
                </ProtectedRoute>
              }
            />
            
            <Route
              path="/supplier"
              element={
                <ProtectedRoute allowedRoles={['supplier']}>
                  <SupplierPortal />
                </ProtectedRoute>
              }
            />
            
            <Route
              path="/orders"
              element={
                <ProtectedRoute allowedRoles={['customer']}>
                  <CustomerOrders />
                </ProtectedRoute>
              }
            />
          </Route>
        </Routes>
      </Router>
    </Provider>
  );
}

export default App;
