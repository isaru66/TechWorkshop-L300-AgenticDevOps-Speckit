import { NavLink, Navigate, Route, Routes } from 'react-router-dom';
import CustomersPage from './pages/CustomersPage';
import CustomerDetailPage from './pages/CustomerDetailPage';
import NewCustomerPage from './pages/NewCustomerPage';
import AccountsPage from './pages/AccountsPage';
import AccountDetailPage from './pages/AccountDetailPage';
import TransferPage from './pages/TransferPage';

export default function App() {
  return (
    <>
      <nav className="nav" aria-label="Main">
        <span className="brand">Local Banking</span>
        <NavLink to="/customers">Customers</NavLink>
        <NavLink to="/accounts">Accounts</NavLink>
        <NavLink to="/transfer">Transfer</NavLink>
      </nav>
      <main className="container">
        <Routes>
          <Route path="/" element={<Navigate to="/customers" replace />} />
          <Route path="/customers" element={<CustomersPage />} />
          <Route path="/customers/new" element={<NewCustomerPage />} />
          <Route path="/customers/:customerId" element={<CustomerDetailPage />} />
          <Route path="/accounts" element={<AccountsPage />} />
          <Route path="/accounts/:accountId" element={<AccountDetailPage />} />
          <Route path="/transfer" element={<TransferPage />} />
          <Route path="*" element={<p>Page not found.</p>} />
        </Routes>
      </main>
    </>
  );
}
