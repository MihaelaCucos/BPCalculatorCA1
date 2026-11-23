import { check } from 'k6';
import http from 'k6/http';

const baseUrl = (__ENV.BASE_URL || 'http://127.0.0.1:5050').replace(/\/$/, '');

export function openCalculator() {
  const response = http.get(baseUrl, { tags: { name: 'GET /' } });
  const body = response.body || '';
  const tokenMatch = body.match(
    /<input[^>]*name="__RequestVerificationToken"[^>]*value="([^"]+)"/,
  );

  check(response, {
    'calculator page returns HTTP 200': (res) => res.status === 200,
    'calculator heading is present': (res) =>
      (res.body || '').includes('BP Category Calculator'),
    'form contains an anti-forgery token': () => tokenMatch !== null,
  });

  return tokenMatch ? tokenMatch[1] : null;
}

export function submitReading(systolic, diastolic, expectedText, unexpectedText = null) {
  const token = openCalculator();
  if (!token) {
    return;
  }

  const response = http.post(
    baseUrl,
    {
      'BP.Systolic': String(systolic),
      'BP.Diastolic': String(diastolic),
      __RequestVerificationToken: token,
    },
    {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      tags: { name: 'POST /' },
    },
  );
  const body = response.body || '';

  const responseChecks = {
    'form submission returns HTTP 200': (res) => res.status === 200,
    [`response contains "${expectedText}"`]: () => body.includes(expectedText),
  };
  if (unexpectedText !== null) {
    responseChecks[`response omits "${unexpectedText}"`] = () => !body.includes(unexpectedText);
  }
  check(response, responseChecks);
}
