import { screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api/client';
import CustomersPage from '../pages/CustomersPage';
import CustomerDetailPage from '../pages/CustomerDetailPage';
import { renderRoute } from './utils';

vi.mock('../api/client', async (orig) => {
  const actual = await orig<typeof import('../api/client')>();
  return { ...actual, api: { listCustomers: vi.fn(), getCustomer: vi.fn(), listCustomerAccounts: vi.fn() } };
});

const customer = {
  id: 'c1', name: 'Somchai Testman', englishName: 'Somchai', email: 'a@example.test', phone: '0800000000',
  nationalId: '1234567890123', province: 'Bangkok', createdAt: '2025-01-01T00:00:00Z',
};

beforeEach(() => vi.resetAllMocks());

describe('Customers', () => {
  it('renders the customer list', async () => {
    vi.mocked(api.listCustomers).mockResolvedValue({ page: 0, size: 20, totalElements: 1, totalPages: 1, items: [customer] });
    renderRoute(<CustomersPage />, '/customers', '/customers');
    expect(await screen.findByRole('link', { name: 'Somchai Testman' })).toBeInTheDocument();
  });

  it('shows the full record and accounts with balances', async () => {
    vi.mocked(api.getCustomer).mockResolvedValue(customer);
    vi.mocked(api.listCustomerAccounts).mockResolvedValue([
      { id: 'acc-1', customerId: 'c1', currency: 'THB', balance: '1500.50', createdAt: '2025-01-01T00:00:00Z' },
    ]);
    renderRoute(<CustomerDetailPage />, '/customers/:customerId', '/customers/c1');
    expect(await screen.findByText('1234567890123')).toBeInTheDocument();
    expect(screen.getByText('a@example.test')).toBeInTheDocument();
    const row = (await screen.findByRole('link', { name: 'acc-1' })).closest('tr')!;
    expect(within(row).getByText('1500.50')).toBeInTheDocument();
  });
});
