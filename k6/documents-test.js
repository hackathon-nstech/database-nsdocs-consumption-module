import http from 'k6/http';
import { sleep, check } from 'k6';
import { Rate } from 'k6/metrics';

// Add custom metrics
const failures = new Rate('failures');

// Add environment variables
const BASE_URL = __ENV.BASE_URL || 'http://localhost:3000';

function getRandomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

// Improved random hash generation
function generateRandomHash() {
  const chars = '0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ';
  let hash = '';
  for (let i = 0; i < 44; i++) {
    hash += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return hash;
}

// More realistic stages
export const options = {
  stages: [
    { duration: '30s', target: 100 },   // Ramp-up to 100 users
    { duration: '1m', target: 100 },    // Stay at 100 users
    { duration: '30s', target: 200 },   // Ramp-up to 200
    { duration: '1m', target: 200 },    // Stay at 200
    { duration: '30s', target: 0 },     // Ramp-down
  ],
  thresholds: {
    failures: ['rate<0.1'], // Less than 10% failures
  },
};

export default function () {
  const url = `${BASE_URL}/documents`;

  // POST request with randomized data
  const payload = JSON.stringify({
    CompanyId: getRandomInt(1, 100),
    AccessKey: generateRandomHash(),
    request_date: new Date().toISOString(),
    updated_date: new Date().toISOString(),
    origin_id: getRandomInt(1, 3),
    document_type_id: getRandomInt(4, 10),
    status_id: getRandomInt(11, 14),
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const postRes = http.post(url, payload, params);
  const postCheck = check(postRes, {
    'POST status is 201': (r) => r.status === 201,
  });
  if (!postCheck) {
    failures.add(1);
    console.error(`POST request failed. Status: ${postRes.status}, Body: ${postRes.body}`);
  }

  sleep(1); // Simulate user think time

  // GET request to retrieve the created document
  let documentId = null;
  if (postCheck && postRes.body) {
      try {
          const responseBody = JSON.parse(postRes.body);
          // Adapt this based on your actual API response structure
          documentId = responseBody.id || responseBody._id || responseBody.documentId; 
      } catch (e) {
          console.error(`Failed to parse POST response body: ${e}, Body: ${postRes.body}`);
          failures.add(1);
      }
  }

  // Only proceed with GET if POST was successful and we have an ID
  if (documentId) {
      const getRes = http.get(`${url}/${documentId}`, params);
      const getCheck = check(getRes, {
          'GET status is 200': (r) => r.status === 200,
      });
      if (!getCheck) {
          failures.add(1);
          console.error(`GET request failed for ID ${documentId}. Status: ${getRes.status}, Body: ${getRes.body}`);
      }
  } else {
      // Optionally add a failure if we couldn't get an ID from a successful POST
      if (postCheck) {
          console.error('Could not extract document ID from successful POST response.');
          failures.add(1); 
      }
  }

  sleep(1); // Simulate user think time
}
