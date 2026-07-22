import http from 'k6/http';
import exec from 'k6/execution';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

// Load test for AuthorizationInterceptor, driven through the SourceApi sample.
//
// Prerequisites (see ../README.md):
//   1. TargetApi   -> dotnet run --project ../TargetApi    (http://localhost:5121)
//   2. Redis       -> localhost:6379   (this endpoint uses the distributed cache interceptor)
//   3. SourceApi   -> dotnet run --project ../SourceApi    (http://localhost:5117)
//
// Run:
//   k6 run load-test.js
//   k6 run -e COLD_VUS=200 -e RPS=500 -e DURATION=5m load-test.js
//
// Timing that matters: TargetApi issues access tokens with expires_in = 30s and a 60s refresh
// token. AuthorizationHeaders caches for the refresh lifetime but treats the headers as expired
// once the access token dies, so the interceptor chain re-authenticates roughly every 30 seconds.
// The default 3 minute steady phase crosses about six of those boundaries on purpose: they are
// the moments where concurrent callers would otherwise stampede the auth endpoint.

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5117';
const ENDPOINT = `${BASE_URL}/test/TargetApiWithCustomInterceptors`;

const COLD_VUS = Number(__ENV.COLD_VUS || 100);
const RPS = Number(__ENV.RPS || 200);
const DURATION = __ENV.DURATION || '3m';

// Each /auth call on TargetApi mints a brand new access token, and /data echoes it back. So the
// number of distinct tokens a VU observes is a direct read on how often the chain actually
// authenticated, rather than served the value another caller had already cached.
const tokenRotations = new Counter('auth_token_rotations');

let lastToken = null;

export const options = {
  scenarios: {
    // Everyone arrives at once against an empty cache. Without deduplication every one of these
    // requests reaches the authentication handler; with it, one authenticates and the rest wait.
    cold_start: {
      executor: 'per-vu-iterations',
      vus: COLD_VUS,
      iterations: 1,
      maxDuration: '20s',
      exec: 'callEndpoint',
    },
    // Sustained traffic across several token expirations.
    steady_state: {
      executor: 'constant-arrival-rate',
      rate: RPS,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: Math.max(50, Math.ceil(RPS / 4)),
      maxVUs: Math.max(200, RPS),
      startTime: '20s',
      exec: 'callEndpoint',
    },
  },
  thresholds: {
    // Scoping every threshold to a scenario keeps the setup() probe out of the numbers.
    'http_req_failed{scenario:cold_start}': ['rate<0.01'],
    'http_req_failed{scenario:steady_state}': ['rate<0.01'],

    // Calibrated against measured runs on a local machine rather than guessed: deduplicated builds
    // land around p(95) 150-185ms and p(99) 500-605ms here, so these leave roughly 30% headroom and
    // catch a regression back towards the undeduplicated numbers (p(95) 257ms, p(99) 1.08s).
    'http_req_duration{scenario:steady_state}': ['p(95)<250', 'p(99)<700'],

    // Weak signal, kept only to catch gross regressions: at this scale the number is dominated by
    // JIT, handler chain construction and the Redis handshake, not by the interceptor. What is worth
    // reading here is the spread between min and max in the summary, not this threshold. Coalesced
    // callers share one result and finish together, so that spread should stay small.
    'http_req_duration{scenario:cold_start}': ['p(95)<3000'],

    checks: ['rate>0.99'],
  },
};

export function setup() {
  // Probe a different named HttpClient on purpose: hitting the endpoint under test here would
  // populate its cache and destroy the cold start measurement.
  const probe = http.get(`${BASE_URL}/test/TargetApiWithNoInterceptor`);

  if (probe.status !== 200) {
    exec.test.abort(
      `SourceApi probe returned ${probe.status}. Make sure TargetApi (:5121), Redis (:6379) ` +
      `and SourceApi (:5117) are all running before starting the test.`
    );
  }
}

export function callEndpoint() {
  const res = http.get(ENDPOINT, {
    headers: { 'x-mycustom-header': 'k6-load-test' },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'returned an access token': (r) => {
      try {
        return typeof r.json('access_token') === 'string';
      } catch (e) {
        return false;
      }
    },
  });

  if (res.status !== 200) {
    return;
  }

  let token;
  try {
    token = res.json('access_token');
  } catch (e) {
    return;
  }

  if (!token || token === lastToken) {
    return;
  }

  // The first token a VU sees is its baseline, not a rotation.
  if (lastToken !== null) {
    tokenRotations.add(1);
  }

  lastToken = token;
}

export function teardown() {
  console.log('');
  console.log('How to read the results');
  console.log('-----------------------');
  console.log(`  auth_token_rotations   how often a VU saw the token change. TargetApi tokens live`);
  console.log(`                         30s, so over the steady phase expect roughly`);
  console.log(`                         (duration / 30s) rotations per VU. Divide the total by the`);
  console.log(`                         VU count the steady phase settled on, otherwise runs with`);
  console.log(`                         different VU counts are not comparable. Well above that`);
  console.log(`                         means callers are authenticating past each other instead of`);
  console.log(`                         sharing one result.`);
  console.log(`  cold_start min..max    the spread matters more than any single percentile. Callers`);
  console.log(`                         sharing one authentication finish together, so the range`);
  console.log(`                         stays tight. A wide range means they queued behind each`);
  console.log(`                         other; a uniformly high one means they all authenticated.`);
  console.log(`                         The absolute value is mostly process warmup, not the library.`);
  console.log(`  steady_state p(95)     cache-hit path, dominated by the SourceApi -> TargetApi hop.`);
  console.log(`                         The refresh waves every 30s live in p(99) and max.`);
  console.log('');
  console.log('Note: the memory cache lives in the SourceApi process and survives between runs.');
  console.log('Restart SourceApi (or wait 60s) before re-running to measure a genuinely cold start.');
  console.log('');
  console.log('To keep the numbers for later: k6 run --summary-export=summary.json load-test.js');
}
