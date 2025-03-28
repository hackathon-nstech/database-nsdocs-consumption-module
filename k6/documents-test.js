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

  // Generate data for POST
  const postData = {
    CompanyId: getRandomInt(1, 10), // Reduced range for better consumption testing
    AccessKey: generateRandomHash(),
    Origin: getRandomEnum(DocumentOrigin),
    DocumentType: getRandomEnum(DocumentType),
    Status: getRandomEnum(DocumentStatus)
    // request_date and updated_date are set by the API
  };
  const postPayload = JSON.stringify(postData);

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const postRes = http.post(url, postPayload, params);
  const postCheck = check(postRes, {
    'POST status is 201': (r) => r.status === 201,
  });
  if (!postCheck) {
    failures.add(1);
    console.error(`POST request failed. Status: ${postRes.status}, Body: ${postRes.body}`);
  }

  sleep(1); // Simulate user think time between POST and PUT

  // Attempt to Update the document if POST was successful
  let documentId = null;
  if (postCheck && postRes.body) {
      try {
          // API returns the ID directly as a number
          documentId = postRes.json(); 
          if (typeof documentId !== 'number' || documentId <= 0) {
              throw new Error('Invalid document ID received from POST');
          }
      } catch (e) {
          console.error(`Failed to parse POST response body or invalid ID: ${e}, Body: ${postRes.body}`);
          failures.add(1);
          documentId = null; // Ensure documentId is null if parsing fails
      }
  }

  // Only proceed with PUT if POST was successful and we have an ID
  if (documentId) {
      // Include original CompanyId and AccessKey in the PUT payload as required by validation
      const updatePayload = JSON.stringify({
          CompanyId: postData.CompanyId, // Required by validator
          AccessKey: postData.AccessKey, // Required by validator
          Origin: getRandomEnum(DocumentOrigin), // Pick a new random origin
          DocumentType: postData.DocumentType, // Keep original type (or randomize if needed)
          Status: getRandomEnum(DocumentStatus)  // Pick a new random status
      });

      const putRes = http.put(`${url}/${documentId}`, updatePayload, params);
      const putCheck = check(putRes, {
          'PUT status is 204': (r) => r.status === 204,
      });
      if (!putCheck) {
          failures.add(1);
          console.error(`PUT request failed for ID ${documentId}. Status: ${putRes.status}, Body: ${putRes.body}`);
      }
      
      sleep(1); // Simulate user think time after PUT before next iteration
      
  } else {
      // Optionally add a failure if we couldn't get an ID from a successful POST
      if (postCheck) {
          console.error('Could not extract document ID from successful POST response, skipping PUT.');
          // failures.add(1); // Decide if not getting an ID is a failure for the PUT step
      }
      // If POST failed, we already logged it and added to failures.
      // Add a sleep here too so iterations roughly take the same time even on POST failure.
      sleep(1); 
  }


  // The API does not support GET /api/documents/{id}, so we skip this part.
  /* 
  // GET request to retrieve the created document - Logic removed previously
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
          documentId = null; 
      }
  }
  */
}
