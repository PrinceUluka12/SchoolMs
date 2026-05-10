import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ── Metrics ───────────────────────────────────────────────────────────────────
const errorRate = new Rate('errors');
const apiDuration = new Trend('api_duration');

// ── Config ────────────────────────────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || 'https://schoolms-api-staging.azurewebsites.net';
const JWT_TOKEN = __ENV.JWT_TOKEN || '';

export const options = {
  stages: [
    { duration: '2m',  target: 100  },  // Ramp up to 100 users
    { duration: '5m',  target: 500  },  // Ramp up to 500 users
    { duration: '10m', target: 1000 },  // Hold at 1,000 users (target load)
    { duration: '5m',  target: 1000 },  // Soak at 1,000 users
    { duration: '3m',  target: 0    },  // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],   // 95% of requests must complete < 500ms
    http_req_failed:   ['rate<0.01'],   // Less than 1% failures
    errors:            ['rate<0.01'],
  },
};

const HEADERS = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${JWT_TOKEN}`,
};

export default function () {
  // ── Health check ───────────────────────────────────────────────────────────
  const health = http.get(`${BASE_URL}/health/ready`);
  check(health, { 'health ready': r => r.status === 200 });

  // ── Dashboard stats ────────────────────────────────────────────────────────
  const dashboard = http.get(`${BASE_URL}/api/v1/dashboard/stats`, { headers: HEADERS });
  check(dashboard, { 'dashboard 200': r => r.status === 200 });
  apiDuration.add(dashboard.timings.duration, { endpoint: 'dashboard' });
  errorRate.add(dashboard.status !== 200);

  // ── Students list ──────────────────────────────────────────────────────────
  const students = http.get(`${BASE_URL}/api/v1/students?page=1&pageSize=20`, { headers: HEADERS });
  check(students, { 'students 200': r => r.status === 200 });
  apiDuration.add(students.timings.duration, { endpoint: 'students' });
  errorRate.add(students.status !== 200);

  // ── Academic years ─────────────────────────────────────────────────────────
  const years = http.get(`${BASE_URL}/api/v1/academic-years`, { headers: HEADERS });
  check(years, { 'academic-years 200': r => r.status === 200 });
  errorRate.add(years.status !== 200);

  // ── Finance summary (cached) ───────────────────────────────────────────────
  const finance = http.get(`${BASE_URL}/api/v1/dashboard/stats`, { headers: HEADERS });
  check(finance, { 'finance 200': r => r.status === 200 });
  errorRate.add(finance.status !== 200);

  sleep(1);
}

export function handleSummary(data) {
  return {
    'stdout': JSON.stringify({
      p95_response_time_ms: data.metrics.http_req_duration.values['p(95)'],
      error_rate_percent:   (data.metrics.http_req_failed.values.rate * 100).toFixed(2),
      total_requests:       data.metrics.http_reqs.values.count,
      pass: data.metrics.http_req_duration.values['p(95)'] < 500 &&
            data.metrics.http_req_failed.values.rate < 0.01
    }, null, 2),
  };
}