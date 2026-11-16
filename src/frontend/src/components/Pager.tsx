interface Props {
  page: number;
  totalPages: number;
  onPage: (page: number) => void;
}

export default function Pager({ page, totalPages, onPage }: Props) {
  return (
    <div className="pager">
      <button type="button" className="btn secondary" disabled={page <= 0} onClick={() => onPage(page - 1)}>
        Previous
      </button>
      <span>
        Page {page + 1} of {Math.max(totalPages, 1)}
      </span>
      <button
        type="button"
        className="btn secondary"
        disabled={page + 1 >= totalPages}
        onClick={() => onPage(page + 1)}
      >
        Next
      </button>
    </div>
  );
}
