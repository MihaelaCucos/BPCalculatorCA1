import { sleep } from 'k6';
import { submitReading } from './helpers/calculator.js';

export const options = {
  scenarios: {
    short_spike: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 1 },
        { duration: '5s', target: 20 },
        { duration: '10s', target: 20 },
        { duration: '5s', target: 1 },
        { duration: '10s', target: 1 },
      ],
      gracefulRampDown: '5s',
      gracefulStop: '5s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
  },
};

export default function () {
  submitReading(120, 79, 'Pre-High Blood Pressure');
  sleep(1);
}
