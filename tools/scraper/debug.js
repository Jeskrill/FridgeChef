#!/usr/bin/env node
/**
 * Strategy: Use Puppeteer stealth, stay on main page, 
 * make in-page fetch() requests to the search URL.
 * The browser handles cookies/CORS natively.
 */
const puppeteer = require('puppeteer-extra');
const StealthPlugin = require('puppeteer-extra-plugin-stealth');
puppeteer.use(StealthPlugin());

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-blink-features=AutomationControlled']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Warmup on main page
    console.log('Warming up on main page...');
    await page.goto('https://5ka.ru', { waitUntil: 'networkidle2', timeout: 30000 });
    await new Promise(r => setTimeout(r, 3000));
    
    const title = await page.title();
    console.log('Title:', title || '(empty)');
    const cookies = await page.cookies();
    console.log(`Cookies: ${cookies.length}`);

    // Check if we actually got past the challenge
    const hasApp = await page.$('#__next') || await page.$('#app');
    console.log('Has app element:', !!hasApp);

    if (!hasApp) {
        // Still on challenge page — wait more
        console.log('Waiting longer for challenge to resolve...');
        await new Promise(r => setTimeout(r, 5000));
    }

    // Try in-page fetch of search URL
    console.log('\nTrying in-page fetch of search...');
    const result = await page.evaluate(async () => {
        try {
            const resp = await fetch('/search/?text=молоко', {
                credentials: 'include',
                headers: { 'Accept': 'text/html' }
            });
            const html = await resp.text();
            return { 
                status: resp.status, 
                length: html.length, 
                hasCaptcha: html.includes('captcha') || html.includes('servicepipe'),
                hasNextData: html.includes('__NEXT_DATA__'),
                snippet: html.substring(0, 300)
            };
        } catch(e) {
            return { error: e.message };
        }
    });
    console.log('In-page fetch result:', JSON.stringify(result, null, 2));

    // Also try the internal API directly
    console.log('\nTrying 5d.5ka.ru API from page...');
    const apiResult = await page.evaluate(async () => {
        try {
            const resp = await fetch('https://5d.5ka.ru/api/catalog/v3/stores/Y601/search?mode=store&include_restrict=false&q=молоко&limit=5', {
                credentials: 'include',
                headers: { 'Accept': 'application/json' }
            });
            if (!resp.ok) return { status: resp.status, error: 'not ok' };
            const data = await resp.json();
            return { status: resp.status, keys: Object.keys(data), data: JSON.stringify(data).substring(0, 500) };
        } catch(e) {
            return { error: e.message };
        }
    });
    console.log('API result:', JSON.stringify(apiResult, null, 2));

    await browser.close();
    console.log('\nDone');
})();
