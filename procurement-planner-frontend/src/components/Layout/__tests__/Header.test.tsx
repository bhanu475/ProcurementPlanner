import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect } from 'vitest';
import Header from '../Header';
import { store } from '../../../store';

const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <Provider store={store}>
      <BrowserRouter>
        {component}
      </BrowserRouter>
    </Provider>
  );
};

describe('Header', () => {
  it('renders the application title', () => {
    renderWithProviders(<Header />);
    expect(screen.getByText('Procurement Planner')).toBeInTheDocument();
  });
});