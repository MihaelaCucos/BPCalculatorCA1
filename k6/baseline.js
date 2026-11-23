import { sleep } from 'k6';
import { check } from 'k6';
import http from 'k6/http';

const baseUrl = (__ENV.BASE_URL || 'http://127.0.0.1:5050').replace(/\/$/, '');

export const options = {
  scenarios: {
    baseline: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      gracefulStop: '5s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
  },
};

export default function () {
  const response = http.get(baseUrl, { tags: { name: 'GET /' } });
  check(response, {
    'calculator page returns HTTP 200': (res) => res.status === 200,
    'calculator heading is present': (res) =>
      (res.body || '').includes('BP Category Calculator'),
  });
  sleep(1);
}
