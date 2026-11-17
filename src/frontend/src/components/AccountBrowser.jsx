import { formatMoney } from '../api/money.js';
import Pagination from './Pagination.jsx';

export default function AccountBrowser({ customer, data, loading, error, selectedId, onSelect, onPageChange }) {
  return (
    <section className="panel" aria-labelledby="accounts-heading">
      <div className="section-heading"><h2 id="accounts-heading">Accounts</h2><span>{customer && !loading && !error ? data.totalCount : ''}</span></div>
      {!customer && <p>Select a customer to view accounts.</p>}
      {loading && <p role="status">Loading accounts...</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {customer && !loading && !error && data.items.length === 0 && <p>This customer has no accounts.</p>}
      <div className="selection-list">
        {!loading && !error && data.items.map((account) => (
          <button key={account.id} type="button" className={`selection ${selectedId === account.id ? 'selected' : ''}`}
            onClick={() => onSelect(account)} aria-pressed={selectedId === account.id}>
            <strong>{formatMoney(account.balance)}</strong>
            <span className="identifier">{account.id}</span>
          </button>
        ))}
      </div>
      {customer && <Pagination {...data} onPageChange={onPageChange} label="Account" />}
    </section>
  );
}
