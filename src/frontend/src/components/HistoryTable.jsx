import { formatMoney } from '../api/money.js';
import Pagination from './Pagination.jsx';

export default function HistoryTable({ data, loading, error, onPageChange }) {
  return (
    <section className="panel" aria-labelledby="history-heading">
      <div className="section-heading"><h2 id="history-heading">Transaction history</h2>{!loading && !error && <span>{data.totalCount} entries</span>}</div>
      {loading && <p role="status">Loading history...</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {!loading && !error && data.items.length === 0 && <p>No transactions for this account.</p>}
      {!loading && !error && data.items.length > 0 && <div className="table-scroll"><table>
        <thead><tr><th>Sequence</th><th>Type</th><th>Amount</th><th>Completed</th><th>Resulting balance</th><th>Transfer / counterparty</th></tr></thead>
        <tbody>{data.items.map((entry) => <tr key={entry.id}>
          <td>{entry.sequence}</td><td>{entry.type.replaceAll('_', ' ')}</td><td>{formatMoney(entry.amount)}</td>
          <td>{entry.completedAt}</td><td>{formatMoney(entry.resultingBalance)}</td>
          <td className="identifier">{entry.transferId || 'None'}{entry.counterpartyAccountId && <><br />{entry.counterpartyAccountId}</>}</td>
        </tr>)}</tbody>
      </table></div>}
      <Pagination {...data} onPageChange={onPageChange} label="History" />
    </section>
  );
}
