// Mirrors specs/001-local-banking-api/contracts/openapi.yaml. Money is a decimal string.
export type Money = string;

export type ErrorCode =
  | 'VALIDATION_ERROR'
  | 'NOT_FOUND'
  | 'INSUFFICIENT_FUNDS'
  | 'SAME_ACCOUNT'
  | 'CURRENCY_MISMATCH';

export interface FieldError {
  field?: string;
  message?: string;
}

export interface ApiErrorBody {
  code: ErrorCode | string;
  message: string;
  fieldErrors?: FieldError[];
}

export interface CustomerInput {
  name: string;
  englishName?: string;
  email?: string;
  phone?: string;
  addressLine?: string;
  subdistrict?: string;
  district?: string;
  province?: string;
  postcode?: string;
  dateOfBirth?: string;
  occupation?: string;
  nationalId?: string;
}

export interface Customer extends CustomerInput {
  id: string;
  createdAt: string;
}

export interface Account {
  id: string;
  customerId: string;
  currency: 'THB';
  balance: Money;
  createdAt: string;
}

export interface Balance {
  accountId: string;
  currency: 'THB';
  balance: Money;
}

export interface TransferRequest {
  sourceAccountId: string;
  destinationAccountId: string;
  amount: Money;
}

export interface Transfer {
  id: string;
  sourceAccountId: string;
  destinationAccountId: string;
  amount: Money;
  currency: 'THB';
  completedAt: string;
}

export type TransactionType = 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER_IN' | 'TRANSFER_OUT';

export interface TransactionEntry {
  id: string;
  sequence: number;
  accountId: string;
  type: TransactionType;
  amount: Money;
  currency: 'THB';
  completedAt: string;
  resultingBalance: Money;
  transferId?: string | null;
  counterpartyAccountId?: string | null;
}

export interface Page<T> {
  page: number;
  size: number;
  totalElements: number;
  totalPages: number;
  items: T[];
}
