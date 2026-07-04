import { AccountServiceClient } from '../src/AccountServiceClient';

describe('AccountServiceClient', () => {
  test('creates client instance', () => {
    const client = new AccountServiceClient('http://localhost:8080');
    expect(client).toBeDefined();
  });
});
