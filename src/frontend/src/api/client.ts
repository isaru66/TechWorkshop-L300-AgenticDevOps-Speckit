import type {
  Account,
  ApiErrorBody,
  Balance,
  Customer,
  CustomerInput,
  FieldError,
  Money,
  Page,
  TransactionEntry,
  Transfer,
  TransferRequest,
} from './types';

export class ApiError extends Error {
  status: number;
  code: string;
  fieldErrors: FieldError[];

  constructor(status: number, body: ApiErrorBody) {
    super(body.message);
    this.name = 'ApiError';
    this.status = status;
    this.code = body.code;
    this.fieldErrors = body.fieldErrors ?? [];
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response;
  try {
    res = await fetch(`/api${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', Accept: 'application/json', ...init?.headers },
    });
  } catch {
    throw new ApiError(0, { code: 'NETWORK_ERROR', message: 'Cannot reach the banking API.' });
  }
  if (!res.ok) {
    let body: ApiErrorBody;
    try {
      body = (await res.json()) as ApiErrorBody;
    } catch {
      body = { code: 'UNKNOWN', message: `Request failed (${res.status}).` };
    }
    throw new ApiError(res.status, body);
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

const post = <T>(path: string, body?: unknown) =>
  request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) });

const paging = (page: number, size: number) => `?page=${page}&size=${size}`;

export const api = {
  listCustomers: (page = 0, size = 20) => request<Page<Customer>>(`/customers${paging(page, size)}`),
  getCustomer: (id: string) => request<Customer>(`/customers/${id}`),
  createCustomer: (input: CustomerInput) => post<Customer>('/customers', input),
  listCustomerAccounts: (customerId: string) => request<Account[]>(`/customers/${customerId}/accounts`),
  openAccount: (customerId: string) => post<Account>(`/customers/${customerId}/accounts`),
  listAccounts: (page = 0, size = 20) => request<Page<Account>>(`/accounts${paging(page, size)}`),
  getAccount: (id: string) => request<Account>(`/accounts/${id}`),
  getBalance: (id: string) => request<Balance>(`/accounts/${id}/balance`),
  deposit: (id: string, amount: Money) => post<TransactionEntry>(`/accounts/${id}/deposits`, { amount }),
  withdraw: (id: string, amount: Money) => post<TransactionEntry>(`/accounts/${id}/withdrawals`, { amount }),
  transfer: (req: TransferRequest) => post<Transfer>('/transfers', req),
  listTransactions: (id: string, page = 0, size = 20) =>
    request<Page<TransactionEntry>>(`/accounts/${id}/transactions${paging(page, size)}`),
};

export function errorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    switch (err.code) {
      case 'INSUFFICIENT_FUNDS':
        return 'Insufficient funds: the source account balance is too low for this amount.';
      case 'SAME_ACCOUNT':
        return 'Source and destination must be different accounts.';
      case 'CURRENCY_MISMATCH':
        return 'Currency mismatch: both accounts must use the same currency.';
      case 'NOT_FOUND':
        return 'Not found: ' + err.message;
      case 'VALIDATION_ERROR': {
        const details = err.fieldErrors.map((f) => `${f.field ?? ''} ${f.message ?? ''}`.trim()).join('; ');
        return 'Invalid input: ' + (details || err.message);
      }
      default:
        return err.message;
    }
  }
  return err instanceof Error ? err.message : 'Unexpected error';
}

const MONEY_RE = /^\d+(\.\d{1,2})?$/;
export const isValidMoney = (v: string) => MONEY_RE.test(v) && /[1-9]/.test(v);
