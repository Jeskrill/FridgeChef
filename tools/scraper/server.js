#!/usr/bin/env node
/**
 * 5ka.ru Puppeteer Stealth Sidecar Server
 * 
 * HTTP API consumed by the .NET PuppeteerPyaterochkaScraper:
 *   GET  /health          → { status: "ready"|"busy"|"error", error?: string }
 *   GET  /search?q=молоко → { query, products[], source, error? }
 *   POST /search/batch    → { results: [{ query, products[], source, error? }], completed, total }
 *   POST /restart         → restarts the browser
 *   POST /cookies         → { cookies: "name=val; ..." } sets cookies
 */
const http = require('http');
const path = require('path');
const puppeteer = require('puppeteer-extra');
const StealthPlugin = require('puppeteer-extra-plugin-stealth');
puppeteer.use(StealthPlugin());

const PORT = process.env.PORT || 3333;
const SEARCH_URL = 'https://5ka.ru/search/?text=';
const DELAY_MS = 2000;
const HEADLESS = (process.env.PUPPETEER_HEADLESS || 'true').toLowerCase() !== 'false';
const CHROME_EXECUTABLE_PATH = process.env.CHROME_EXECUTABLE_PATH || null;

let browser = null;
let page = null;
let status = 'starting';
let lastError = null;

// ────── Browser Management ──────

async function launchBrowser() {
    status = 'starting';
    try {
        if (browser) {
            try { await browser.close(); } catch(e) {}
        }

        const userDataDir = path.join(__dirname, '.chrome-profile');
        console.log(`[sidecar] User data dir: ${userDataDir}`);
        console.log(`[sidecar] Launching browser. Headless=${HEADLESS}, ExecutablePath=${CHROME_EXECUTABLE_PATH || 'auto'}`);

        browser = await puppeteer.launch({
            headless: HEADLESS,
            executablePath: CHROME_EXECUTABLE_PATH || undefined,
            userDataDir,
            args: [
                '--disable-blink-features=AutomationControlled',
                '--disable-dev-shm-usage',
                '--window-size=1440,900'
            ]
        });

        page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });

        // Navigate to main page
        console.log('[sidecar] Navigating to 5ka.ru...');
        await page.goto('https://5ka.ru', { waitUntil: 'networkidle2', timeout: 60000 });

        // Wait for page to stabilize (user may need to solve captcha)
        const checkReady = async () => {
            for (let i = 0; i < 30; i++) {
                await new Promise(r => setTimeout(r, 2000));
                const title = await page.title().catch(() => '');
                const url = page.url();
                const hasNext = await page.$('#__NEXT_DATA__').then(el => !!el).catch(() => false);
                console.log(`[sidecar] Check ${i+1}: title="${title}", hasNext=${hasNext}`);
                
                if (hasNext || title.includes('Пятёрочка') || title.includes('5ka')) {
                    console.log('[sidecar] ✓ Site loaded successfully!');
                    return true;
                }
                
                // If on captcha page, wait for user to solve
                const body = await page.evaluate(() => document.body?.innerText?.substring(0, 100) || '').catch(() => '');
                if (body.includes('Разверните') || body.includes('captcha')) {
                    console.log('[sidecar] ⚠ Captcha detected — please solve it in the browser window');
                }
            }
            return false;
        };

        const ready = await checkReady();
        
        const cookies = await page.cookies();
        console.log(`[sidecar] Warmup done. Cookies: ${cookies.length}, Ready: ${ready}`);

        status = ready ? 'ready' : 'error';
        lastError = ready
            ? null
            : HEADLESS
                ? 'Initial warmup failed in headless mode. Retry with PUPPETEER_HEADLESS=false or refresh cookies.'
                : 'Captcha not solved — solve it in the browser window, then POST /restart';
    } catch (e) {
        status = 'error';
        lastError = e.message;
        console.error('[sidecar] Browser launch failed:', e.message);
    }
}

// ────── Scraping Logic ──────

async function scrapeQuery(query) {
    if (!browser || status !== 'ready') {
        return { query, products: [], error: 'Browser not ready', source: 'none' };
    }

    let scratchPage = null;
    try {
        // Open a new tab for each search (inherits all cookies from the browser context)
        scratchPage = await browser.newPage();
        await scratchPage.setViewport({ width: 1440, height: 900 });

        const url = SEARCH_URL + encodeURIComponent(query);
        await scratchPage.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });

        // Check for captcha
        const bodyText = await scratchPage.evaluate(() => document.body?.innerText?.substring(0, 200) || '');
        if (bodyText.includes('Разверните') || bodyText.includes('captcha')) {
            return { query, products: [], error: 'captcha', source: 'navigation' };
        }

        // Extract __NEXT_DATA__
        const nextData = await scratchPage.$eval('#__NEXT_DATA__', el => el.textContent).catch(() => null);
        if (nextData) {
            try {
                const data = JSON.parse(nextData);
                const products = findProducts(data).slice(0, 5).map(formatProduct).filter(Boolean);
                return { query, products, source: 'next_data' };
            } catch(e) {
                return { query, products: [], error: 'JSON parse failed', source: 'next_data' };
            }
        }

        return { query, products: [], error: 'no __NEXT_DATA__', source: 'navigation' };
    } catch(e) {
        return { query, products: [], error: e.message, source: 'error' };
    } finally {
        if (scratchPage) await scratchPage.close().catch(() => {});
    }
}

// ────── JSON Parsing ──────

function findProducts(obj, depth = 0) {
    if (depth > 10 || !obj || typeof obj !== 'object') return [];
    if (Array.isArray(obj.products) && obj.products.length > 0) {
        const first = obj.products[0];
        if (first.id || first.plu || first.name) return obj.products;
    }
    for (const key of Object.keys(obj)) {
        if (typeof obj[key] === 'object') {
            const r = findProducts(obj[key], depth + 1);
            if (r.length > 0) return r;
        }
    }
    return [];
}

function formatProduct(p) {
    const id = String(p.id || p.plu || '');
    const name = p.name || '';
    if (!id || !name) return null;

    let regularPrice = 0, discountPrice = null;

    if (p.price != null) {
        const current = Number(p.price) || 0;
        const old = p.oldPrice != null ? Number(p.oldPrice) : 0;
        if (old > current && old > 0) {
            regularPrice = old; discountPrice = current;
        } else {
            regularPrice = current;
        }
    }
    if (regularPrice === 0 && p.prices) {
        regularPrice = Number(p.prices.regular) || 0;
        const disc = Number(p.prices.discount) || 0;
        if (disc > 0 && disc < regularPrice) discountPrice = disc;
    }
    if (regularPrice <= 0) return null;

    const slug = p.slug || '';
    return {
        externalSku: id,
        title: name,
        brand: p.brand || null,
        regularPrice,
        discountPrice,
        productUrl: slug ? `https://5ka.ru/product/${slug}/` : `https://5ka.ru/product/${id}`
    };
}

// ────── HTTP Server ──────

const server = http.createServer(async (req, res) => {
    const url = new URL(req.url, `http://localhost:${PORT}`);
    const sendJson = (code, data) => {
        res.writeHead(code, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify(data));
    };

    try {
        // GET /health
        if (req.method === 'GET' && url.pathname === '/health') {
            return sendJson(200, { status, error: lastError });
        }

        // GET /search?q=...
        if (req.method === 'GET' && url.pathname === '/search') {
            const q = url.searchParams.get('q');
            if (!q) return sendJson(400, { error: 'Missing ?q= parameter' });

            const result = await scrapeQuery(q);
            return sendJson(200, result);
        }

        // POST /search/batch  body: { queries: ["a","b",...] }
        if (req.method === 'POST' && url.pathname === '/search/batch') {
            const body = await readBody(req);
            const { queries } = JSON.parse(body);
            if (!Array.isArray(queries)) return sendJson(400, { error: 'queries must be an array' });

            const results = [];
            for (const [index, q] of queries.entries()) {
                const r = await scrapeQuery(q);
                results.push(r);
                if (index < queries.length - 1) {
                    await new Promise(r => setTimeout(r, DELAY_MS));
                }
            }

            return sendJson(200, {
                results,
                completed: results.filter(r => !r.error && r.products.length > 0).length,
                total: queries.length
            });
        }

        // POST /restart
        if (req.method === 'POST' && url.pathname === '/restart') {
            launchBrowser().catch(console.error);
            return sendJson(200, { message: 'Restart initiated' });
        }

        // POST /cookies  body: { cookies: "name=val; ..." }
        if (req.method === 'POST' && url.pathname === '/cookies') {
            const body = await readBody(req);
            const { cookies: cookieStr } = JSON.parse(body);
            if (!cookieStr || !page) return sendJson(400, { error: 'No cookies or page' });

            const cookieArr = cookieStr.split(';').map(c => {
                const [name, ...rest] = c.trim().split('=');
                return { name: name?.trim(), value: rest.join('='), domain: '.5ka.ru', path: '/' };
            }).filter(c => c.name);

            await page.setCookie(...cookieArr);
            return sendJson(200, { message: `Set ${cookieArr.length} cookies` });
        }

        sendJson(404, { error: 'Not found' });
    } catch(e) {
        console.error('[sidecar] Error:', e.message);
        sendJson(500, { error: e.message });
    }
});

function readBody(req) {
    return new Promise((resolve, reject) => {
        let data = '';
        req.on('data', chunk => data += chunk);
        req.on('end', () => resolve(data));
        req.on('error', reject);
    });
}

// ────── Start ──────

server.listen(PORT, async () => {
    console.log(`[sidecar] Puppeteer 5ka.ru scraper listening on http://localhost:${PORT}`);
    await launchBrowser();
});

process.on('SIGINT', async () => {
    console.log('[sidecar] Shutting down...');
    if (browser) await browser.close();
    process.exit(0);
});
