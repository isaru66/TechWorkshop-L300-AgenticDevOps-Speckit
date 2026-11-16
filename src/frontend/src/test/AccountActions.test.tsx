import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError } from '../api/client';
import AccountDetailPage from '../pages/AccountDetailPage';
import { renderRoute } from './utils';

vi.mock('../api/client', async (orig) => {
  const actual = await orig<typeof import('../api/client')>();
  return { ...actual, api: { getAccount: vi.fn(), deposit: vi.fn(), withdraw: vi.fn(), listTransactions: vi.fn() } };
});

const acct = (balance: string) => ({
  id: 'acc-1', customerId: 'c1', currency: 'THB' as const, balance, createdAt: '2025-01-01T00:00:00Z',
});
const entry = {
  id: 'e1', sequence: 1, accountId: 'acc-1', type: 'DEPOSIT' as const, amount: '50.00', currency: 'THB' as const,
  completedAt: '2025-01-01T00:00:00Z', resultingBalance: '150.00',
};

function render() {
  renderRoute(<AccountDetailPage />, '/accounts/:accountId', '/accounts/acc-1');
}

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.listTransactions).mockResolvedValue({ page: 0, size: 20, totalElements: 0, totalPages: 0, items: [] });
});

describe('Account actions', () => {
  it('deposit submits and updates the balance', async () => {
    vi.mocked(api.getAccount).mockResolvedValueOnce(acct('100.00')).mockResolvedValue(acct('150.00'));
    vi.mocked(api.deposit).mockResolvedValue(entry);
    render();
    await screen.findByText('100.00 THB');
    await userEvent.type(screen.getByLabelText('Deposit amount (THB)'), '50.00');
    await userEvent.click(screen.getByRole('button', { name: 'Deposit' }));
    expect(await screen.findByText('150.00 THB')).toBeInTheDocument();
    expect(api.deposit).toHaveBeenCalledWith('acc-1', '50.00');
  });

  it('withdraw surfaces an insufficient-funds error', async () => {
    vi.mocked(api.getAccount).mockResolvedValue(acct('100.00'));
    vi.mocked(api.withdraw).mockRejectedValue(new ApiError(422, { code: 'INSUFFICIENT_FUNDS', message: 'no' }));
    render();
    await screen.findByText('100.00 THB');
    await userEvent.type(screen.getByLabelText('Withdraw amount (THB)'), '500.00');
    await userEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
    expect(await screen.findByText(/Insufficient funds/)).toBeInTheDocument();
  });

  it('rejects an invalid amount client-side', async () => {
    vi.mocked(api.getAccount).mockResolvedValue(acct('100.00'));
    render();
    await screen.findByText('100.00 THB');
    await userEvent.type(screen.getByLabelText('Deposit amount (THB)'), '1.234');
    await userEvent.click(screen.getByRole('button', { name: 'Deposit' }));
    expect(await screen.findByText(/at most 2 decimals/)).toBeInTheDocument();
    expect(api.deposit).not.toHaveBeenCalled();
  });
});
