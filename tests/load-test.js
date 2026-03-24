import http from 'k6/http';
import { check, group, sleep } from 'k6';

const API_BASE_URL = 'http://localhost:5211';
const CONTRACT_FILE = 'contract.json';

// Ler arquivo no escopo global (init stage)
const contractData = JSON.parse(open(CONTRACT_FILE));

function randomString(length) {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}

export const options = {
  vus: 100, // Virtual Users
  duration: '30s', // Duração total do teste
  thresholds: {
    http_req_duration: ['p(95)<5000'], // 95% das requests devem ter tempo de resposta menor que 5s
    http_req_failed: ['rate<0.1'], // Taxa de falhas deve ser menor que 10%
  },
};

export default function () {
  group('Contract Processing Load Test', () => {
    const idempotencyKey = `k6-test-${randomString(8)}-${Date.now()}`;
    const payload = {
      idempotencyKey: idempotencyKey,
      contracts: contractData.contracts
    };

    let response = http.post(
      `${API_BASE_URL}/api/contracts`,
      JSON.stringify(payload),
      {
        headers: {
          'Content-Type': 'application/json',
        },
        timeout: '30s',
      }
    );

    check(response, {
      'status is 202': (r) => r.status === 202,
      'response time < 5s': (r) => r.timings.duration < 5000,
      'has processingDetails': (r) => r.json('processingDetails') !== undefined,
    });

    sleep(0.1);
  });
}
