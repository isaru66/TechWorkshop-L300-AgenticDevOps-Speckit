import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api/client';
import NewCustomerPage from '../pages/NewCustomerPage';
import CustomerDetailPage from '../pages/CustomerDetailPage';
import { renderRoute } from './utils';

vi.mock('../api/client', async (orig) => {
  const actual = await orig<typeof import('../api/client')>();
  return {
    ...actual,
    api: { createCustomer: vi.fn(), getCustomer: vi.fn(), listCustomerAccounts: vi.fn(), openAccount: vi.fn() },
  };
});

beforeEach(() => vi.resetAllMocks());

describe('Create forms', () => {
  it('validates the customer name', async () => {
    renderRoute(<NewCustomerPage />, '/customers/new', '/customers/new');
    await userEvent.click(screen.getByRole('button', { name: 'Create customer' }));
    expect(await screen.findByText('Name is required.')).toBeInTheDocument();
    expect(api.createCustomer).not.toHaveBeenCalled();
  });

  it('refreshes the account list after opening an account', async () => {
    const acct = { id: 'acc-new', customerId: 'c1', currency: 'THB' as const, balance: '0.00', createdAt: '2025-01-01T00:00:00Z' };
    vi.mocked(api.getCustomer).mockResolvedValue({ id: 'c1', name: 'Test Person', createdAt: '2025-01-01T00:00:00Z' });
    vi.mocked(api.listCustomerAccounts).mockResolvedValueOnce([]).mockResolvedValue([acct]);
    vi.mocked(api.openAccount).mockResolvedValue(acct);
    renderRoute(<CustomerDetailPage />, '/customers/:customerId', '/customers/c1');
    await userEvent.click(await screen.findByRole('button', { name: 'Open account' }));
    expect(await screen.findByRole('link', { name: 'acc-new' })).toBeInTheDocument();
    expect(api.openAccount).toHaveBeenCalledWith('c1');
  });
});
