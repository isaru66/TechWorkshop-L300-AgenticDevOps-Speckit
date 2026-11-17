import Pagination from './Pagination.jsx';

export default function CustomerBrowser({ data, loading, error, selectedId, onSelect, onPageChange }) {
  return (
    <section className="panel" aria-labelledby="customers-heading">
      <div className="section-heading"><h2 id="customers-heading">Customers</h2><span>{data.totalCount} total</span></div>
      {loading && <p role="status">Loading customers...</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {!loading && !error && data.items.length === 0 && <p>No customers found.</p>}
      <div className="selection-list">
        {data.items.map((customer) => (
          <button key={customer.id} type="button" className={`selection ${selectedId === customer.id ? 'selected' : ''}`}
            onClick={() => onSelect(customer)} aria-pressed={selectedId === customer.id}>
            <strong>{customer.englishName || customer.name}</strong>
            {customer.englishName && <span>{customer.name}</span>}
          </button>
        ))}
      </div>
      <Pagination {...data} onPageChange={onPageChange} label="Customer" />
    </section>
  );
}
