import React from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { Link, useNavigate } from 'react-router-dom';
import { RootState } from '../../store';
import { logout } from '../../store/slices/authSlice';

const Header: React.FC = () => {
  const { user, isAuthenticated } = useSelector((state: RootState) => state.auth);
  const dispatch = useDispatch();
  const navigate = useNavigate();

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
  };

  return (
    <header className="bg-blue-600 text-white shadow-lg">
      <div className="container mx-auto px-4 py-3 flex justify-between items-center">
        <Link to="/" className="text-xl font-bold">
          Procurement Planner
        </Link>
        
        {isAuthenticated && user && (
          <nav className="flex items-center space-x-4">
            {user.role === 'lmr_planner' && (
              <Link to="/dashboard" className="hover:text-blue-200">
                Dashboard
              </Link>
            )}
            {user.role === 'supplier' && (
              <Link to="/supplier" className="hover:text-blue-200">
                Supplier Portal
              </Link>
            )}
            {user.role === 'customer' && (
              <Link to="/orders" className="hover:text-blue-200">
                My Orders
              </Link>
            )}
            
            <div className="flex items-center space-x-2">
              <span className="text-sm">Welcome, {user.name}</span>
              <button
                onClick={handleLogout}
                className="bg-blue-700 hover:bg-blue-800 px-3 py-1 rounded text-sm"
              >
                Logout
              </button>
            </div>
          </nav>
        )}
      </div>
    </header>
  );
};

export default Header;