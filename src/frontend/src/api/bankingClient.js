async function request(path, options = {}) {
  const response = await fetch(`/api${path}`, options);
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    const error = new Error(problem.detail || 'The banking service is unavailable.');
    error.code = problem.code || 'operation_unavailable';
    error.status = response.status;
    throw error;
  }
  return response.json();
}

const pageQuery = (page) => `?page=${page}&pageSize=50`;

export const bankingClient = {
  customers: (page, signal) => request(`/customers${pageQuery(page)}`, { signal }),
  customer: (customerId, signal) => request(`/customers/${customerId}`, { signal }),
  customerAccounts: (customerId, page, signal) => request(`/customers/${customerId}/accounts${pageQuery(page)}`, { signal }),
  accounts: (page, signal) => request(`/accounts${pageQuery(page)}`, { signal }),
  account: (accountId, signal) => request(`/accounts/${accountId}`, { signal }),
  history: (accountId, page, signal) => request(`/accounts/${accountId}/transactions${pageQuery(page)}`, { signal }),
  money: (accountId, type, amount) => request(`/accounts/${accountId}/${type === 'deposit' ? 'deposits' : 'withdrawals'}`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ amount, currency: 'THB' }),
  }),
  transfer: (sourceAccountId, destinationAccountId, amount) => request('/transfers', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sourceAccountId, destinationAccountId, amount, currency: 'THB' }),
  }),
};
