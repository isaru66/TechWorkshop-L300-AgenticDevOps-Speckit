import { expect, test } from '@playwright/test';

test('browses fictional customer accounts and history through the private proxy', async ({ page, request }) => {
  const customers = await (await request.get('/api/customers?page=1&pageSize=50')).json();
  let customer;
  let account;
  for (const candidate of customers.items) {
    const accounts = await (await request.get(`/api/customers/${candidate.id}/accounts?page=1&pageSize=50`)).json();
    if (accounts.items.length) {
      customer = candidate;
      account = accounts.items[0];
      break;
    }
  }
  expect(customer).toBeTruthy();
  const history = await (await request.get(`/api/accounts/${account.id}/transactions`)).json();
  await page.goto('/');
  await expect(page.getByText('Fictional data only')).toBeVisible();
  const customersPanel = page.getByRole('region', { name: 'Customers' });
  await customersPanel.getByRole('button', { name: customer.englishName || customer.name }).first().click();
  await page.getByRole('button', { name: new RegExp(account.id) }).click();
  await expect(page.getByRole('heading', { name: /^THB / })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Transaction history' })).toContainText(`${history.totalCount} entries`);
});
