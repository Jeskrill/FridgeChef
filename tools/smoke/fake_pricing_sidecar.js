#!/usr/bin/env node

const http = require('http');

const port = Number(process.env.PORT || 3333);

function json(res, status, payload) {
  res.writeHead(status, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(payload));
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let data = '';
    req.on('data', chunk => {
      data += chunk;
    });
    req.on('end', () => resolve(data));
    req.on('error', reject);
  });
}

function buildProduct(query) {
  return {
    externalSku: `sku-${query}`,
    title: `${query} product`,
    brand: 'smoke-brand',
    regularPrice: 199,
    discountPrice: 149,
    productUrl: `https://example.test/${encodeURIComponent(query)}`
  };
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, `http://127.0.0.1:${port}`);

  if (req.method === 'GET' && url.pathname === '/health') {
    return json(res, 200, { status: 'ready', error: null });
  }

  if (req.method === 'GET' && url.pathname === '/search') {
    const query = url.searchParams.get('q');
    if (!query) {
      return json(res, 400, { error: 'Missing query' });
    }

    return json(res, 200, {
      query,
      source: 'fake-sidecar',
      error: null,
      products: [buildProduct(query)]
    });
  }

  if (req.method === 'POST' && url.pathname === '/search/batch') {
    const body = await readBody(req);
    const payload = body ? JSON.parse(body) : {};
    const queries = Array.isArray(payload.queries) ? payload.queries : [];

    return json(res, 200, {
      completed: queries.length,
      total: queries.length,
      results: queries.map(query => ({
        query,
        source: 'fake-sidecar',
        error: null,
        products: [buildProduct(query)]
      }))
    });
  }

  if (req.method === 'POST' && url.pathname === '/restart') {
    return json(res, 200, { message: 'Restart initiated' });
  }

  if (req.method === 'POST' && url.pathname === '/cookies') {
    return json(res, 200, { message: 'Cookies accepted' });
  }

  return json(res, 404, { error: 'Not found' });
});

server.listen(port, '127.0.0.1', () => {
  console.log(`[fake-sidecar] listening on http://127.0.0.1:${port}`);
});
