#!/usr/bin/env node
/**
 * 5ka.ru Product Scraper using Puppeteer + Stealth Plugin
 * 
 * Usage: node scrape.js "query1" "query2" ...
 * Output: JSON array of results to stdout
 * 
 * Called by the .NET HttpPyaterochkaScraper as a subprocess.
 */
const puppeteer = require('puppeteer-extra');
const StealthPlugin = require('puppeteer-extra-plugin-stealth');

puppeteer.use(StealthPlugin());

const SEARCH_URL = 'https://5ka.ru/search/?text=';
const DELAY_MS = 2000;

async function scrapeQuery(page, query) {
    const url = SEARCH_URL + encodeURIComponent(query);
    
    try {
        // Navigate with interception of API responses
        let apiData = null;
        
        page.on('response', async (response) => {
            const reqUrl = response.url();
            if (reqUrl.includes('5d.5ka.ru/api/catalog') && reqUrl.includes('search')) {
                try {
                    if (response.ok()) {
                        apiData = await response.json();
                    }
                } catch (e) { /* ignore */ }
            }
        });

        await page.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });

        // Wait a bit for API response
        await new Promise(r => setTimeout(r, 3000));

        // Strategy 1: API response intercepted
        if (apiData) {
            const products = findProducts(apiData);
            if (products.length > 0) {
                return { query, products: products.slice(0, 5) };
            }
        }

        // Strategy 2: __NEXT_DATA__
        const nextData = await page.evaluate(() => {
            const el = document.getElementById('__NEXT_DATA__');
            return el ? el.textContent : null;
        });

        if (nextData) {
            try {
                const data = JSON.parse(nextData);
                const products = findProducts(data);
                if (products.length > 0) {
                    return { query, products: products.slice(0, 5) };
                }
            } catch (e) { /* ignore */ }
        }

        // Check for captcha
        const bodyText = await page.evaluate(() => document.body.innerText || '');
        if (bodyText.includes('captcha') || bodyText.includes('Разверните картинку')) {
            return { query, products: [], error: 'captcha' };
        }

        return { query, products: [], error: 'no_data' };
    } catch (e) {
        return { query, products: [], error: e.message };
    }
}

function findProducts(obj, depth = 0) {
    if (depth > 10 || !obj || typeof obj !== 'object') return [];
    
    if (Array.isArray(obj.products) && obj.products.length > 0) {
        const first = obj.products[0];
        if (first.id || first.plu || first.name) {
            return obj.products.map(p => parseProduct(p)).filter(Boolean);
        }
    }

    for (const key of Object.keys(obj)) {
        if (typeof obj[key] === 'object') {
            const result = findProducts(obj[key], depth + 1);
            if (result.length > 0) return result;
        }
    }
    return [];
}

function parseProduct(p) {
    const id = String(p.id || p.plu || '');
    const name = p.name || '';
    if (!id || !name) return null;

    let regularPrice = 0;
    let discountPrice = null;

    // Format 1: { price, oldPrice }
    if (p.price != null) {
        const current = Number(p.price) || 0;
        const old = p.oldPrice != null ? Number(p.oldPrice) : 0;
        if (old > current && old > 0) {
            regularPrice = old;
            discountPrice = current;
        } else {
            regularPrice = current;
        }
    }

    // Format 2: { prices: { regular, discount } }
    if (regularPrice === 0 && p.prices) {
        regularPrice = Number(p.prices.regular) || 0;
        const disc = Number(p.prices.discount) || 0;
        if (disc > 0 && disc < regularPrice) discountPrice = disc;
    }

    if (regularPrice <= 0) return null;

    const slug = p.slug || '';
    const url = slug ? `https://5ka.ru/product/${slug}/` : `https://5ka.ru/product/${id}`;

    return {
        externalSku: id,
        title: name,
        brand: p.brand || null,
        regularPrice,
        discountPrice,
        productUrl: url
    };
}

async function main() {
    const queries = process.argv.slice(2);
    if (queries.length === 0) {
        console.error('Usage: node scrape.js "query1" "query2" ...');
        process.exit(1);
    }

    const browser = await puppeteer.launch({
        headless: 'new',
        args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--disable-blink-features=AutomationControlled'
        ]
    });

    const results = [];

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });

        // Warmup: visit main page first
        await page.goto('https://5ka.ru', { waitUntil: 'networkidle2', timeout: 30000 });
        await new Promise(r => setTimeout(r, 2000));

        for (const query of queries) {
            const result = await scrapeQuery(page, query);
            results.push(result);
            if (queries.indexOf(query) < queries.length - 1) {
                await new Promise(r => setTimeout(r, DELAY_MS));
            }
        }
    } finally {
        await browser.close();
    }

    // Output JSON to stdout
    console.log(JSON.stringify(results));
}

main().catch(e => {
    console.error(e.message);
    process.exit(1);
});
