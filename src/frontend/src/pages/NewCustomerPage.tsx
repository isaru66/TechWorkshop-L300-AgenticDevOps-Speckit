import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api, errorMessage } from '../api/client';
import type { CustomerInput } from '../api/types';

const OPTIONAL: [keyof CustomerInput, string, string?][] = [
  ['englishName', 'English name'],
  ['email', 'Email', 'email'],
  ['phone', 'Phone'],
  ['addressLine', 'Address'],
  ['subdistrict', 'Subdistrict'],
  ['district', 'District'],
  ['province', 'Province'],
  ['postcode', 'Postcode'],
  ['dateOfBirth', 'Date of birth', 'date'],
  ['occupation', 'Occupation'],
  ['nationalId', 'National ID'],
];

export default function NewCustomerPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [values, setValues] = useState<Record<string, string>>({ name: '' });
  const [nameError, setNameError] = useState('');
  const create = useMutation({
    mutationFn: (input: CustomerInput) => api.createCustomer(input),
    onSuccess: (c) => {
      qc.invalidateQueries({ queryKey: ['customers'] });
      navigate(`/customers/${c.id}`);
    },
  });

  const set = (k: string, v: string) => setValues((s) => ({ ...s, [k]: v }));

  function submit(e: FormEvent) {
    e.preventDefault();
    if (!values.name?.trim()) {
      setNameError('Name is required.');
      return;
    }
    setNameError('');
    const input: Record<string, string> = {};
    for (const [k, v] of Object.entries(values)) if (v.trim()) input[k] = v.trim();
    create.mutate(input as unknown as CustomerInput);
  }

  return (
    <section>
      <h1>New customer</h1>
      <form className="panel" onSubmit={submit} noValidate>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="name">Name *</label>
            <input id="name" value={values.name} onChange={(e) => set('name', e.target.value)} />
            {nameError && <div className="field-error" role="alert">{nameError}</div>}
          </div>
          {OPTIONAL.map(([k, label, type]) => (
            <div className="field" key={k}>
              <label htmlFor={k}>{label}</label>
              <input id={k} type={type ?? 'text'} value={values[k] ?? ''} onChange={(e) => set(k, e.target.value)} />
            </div>
          ))}
        </div>
        {create.error && <div className="alert error" role="alert">{errorMessage(create.error)}</div>}
        <button className="btn" type="submit" disabled={create.isPending}>Create customer</button>
      </form>
    </section>
  );
}
