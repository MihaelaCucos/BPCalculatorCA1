import { sleep } from 'k6';
import { submitReading } from './helpers/calculator.js';

export const options = {
  scenarios: {
    normal_load: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 3 },
        { duration: '20s', target: 5 },
        { duration: '30s', target: 10 },
        { duration: '10s', target: 0 },
      ],
      gracefulRampDown: '5s',
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
  if (__ITER % 10 === 0) {
    submitReading(
      80,
      80,
      'Systolic must be greater than Diastolic',
      'Pre-High Blood Pressure',
    );
  } else {
    submitReading(120, 79, 'Pre-High Blood Pressure');
  }

  sleep(1);
}
