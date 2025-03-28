import http from 'k6/http';
import { sleep } from 'k6';

// Get PORT from environment variable with fallback to 3000
const PORT = __ENV.PORT || '3000';
const BASE_URL = `http://localhost:${PORT}`;

function getRandomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function generateRandomHash() {
  let hash = '';
  for (let i = 0; i < 44; i++) {
    hash += Math.floor(Math.random() * 10).toString();
  }
  return hash;
}

export const options = {
  stages: [
    { duration: '30s', target: 1000 }, // Ramp-up to 10 users over 30 seconds
    { duration: '1m', target: 500 },  // Stay at 10 users for 1 minute
    { duration: '30s', target: 0 },  // Ramp-down to 0 users over 30 seconds
  ],
};

export default function () {
  const url = `${BASE_URL}/documents`; // Replace with your application's URL

  // POST request with randomized data
  const payload = JSON.stringify({
    id_company: getRandomInt(1, 100),
    access_key: generateRandomHash(),
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

  const postRes = http.post(`${url}`, payload, params);
  if (postRes.status !== 201) {
    console.error(`POST request failed. Status: ${postRes.status}`);
  }

  // sleep(1); // Simulate user think time
  
  // GET request to retrieve the created document
  // Assuming the POST response contains the document ID in the response body
  let documentId;
  try {
    const responseBody = JSON.parse(postRes.body);
    documentId = responseBody.id || responseBody._id;
  } catch (e) {
    console.error('Failed to parse POST response body:', e);
    documentId = getRandomInt(1, 1000); // Fallback to random ID if parsing fails
  }
  
  const getRes = http.get(`${url}/${documentId}`, params);
  if (getRes.status !== 200) {
    console.error(`GET request failed. Status: ${getRes.status}`);
  }

  // sleep(1); // Simulate user think time
}
