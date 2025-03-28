import http from 'k6/http';
import { sleep, check } from 'k6';
import { Rate } from 'k6/metrics';

// Add custom metrics
const failures = new Rate('failures');

// Add environment variables
const BASE_URL = __ENV.BASE_URL || 'http://localhost:3000';

// Enum definitions matching the API
const DocumentOrigin = { File: 0, Email: 1, Ws: 2 };
const DocumentType = { Cfe: 0, Cte: 1, Cteos: 2, Mdfe: 3, Nfce: 4, Nfe: 5, Nfse: 6 };
const DocumentStatus = { Ok: 0, Pending: 1, Error: 2, NonExisting: 3 };

// Helper to get a random value from an enum object
function getRandomEnum(enumObj) {
    const values = Object.values(enumObj);
    return values[Math.floor(Math.random() * values.length)];
}

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
    { duration: '45s', target: 100 },    // Stay at 100 users (Shortened)
    { duration: '30s', target: 200 },   // Ramp-up to 200
    { duration: '45s', target: 200 },    // Stay at 200 (Shortened)
    { duration: '30s', target: 0 },     // Ramp-down
  ], // Total duration: 30+45+30+45+30 = 180s (3 minutes)
  thresholds: {
    failures: ['rate<0.1'], // Less than 10% failures
  },
};

export default function () {
  const url = `${BASE_URL}/documents`;

  // POST request with randomized data matching API structure
  const payload = JSON.stringify({
    CompanyId: getRandomInt(1, 10), // Reduced range for better consumption testing
    AccessKey: generateRandomHash(),
    Origin: getRandomEnum(DocumentOrigin),
    DocumentType: getRandomEnum(DocumentType),
    Status: getRandomEnum(DocumentStatus)
    // request_date and updated_date are set by the API
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

  // The API does not support GET /api/documents/{id}, so we skip this part.
  /* 
  // GET request to retrieve the created document
  let documentId = null;
  if (postCheck && postRes.body) {
      try {
          // API returns the ID directly as a number
          documentId = postRes.json(); 
          if (typeof documentId !== 'number' || documentId <= 0) {
              throw new Error('Invalid document ID received');
          }
      } catch (e) {
          console.error(`Failed to parse POST response body or invalid ID: ${e}, Body: ${postRes.body}`);
          failures.add(1);
          documentId = null; // Ensure documentId is null if parsing fails
      }
  }
  */

  // Removed GET request logic as the endpoint is not available.
}
