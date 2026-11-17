export default function Pagination({ page, pageSize, totalCount, onPageChange, label }) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize));
  return (
    <nav className="pagination" aria-label={`${label} pages`}>
      <button type="button" className="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>Previous</button>
      <span>Page {page} of {pages}</span>
      <button type="button" className="secondary" disabled={page >= pages} onClick={() => onPageChange(page + 1)}>Next</button>
    </nav>
  );
}
