#!/usr/bin/env node
/**
 * Session Initializer — opens a VISIBLE Chrome browser to solve the
 * ServicePipe WAF challenge. Cookies are saved to the persistent
 * .chrome-profile directory and reused by the headless server.
 *
 * Usage:
 *   node init-session.js
 *
 * What happens:
 *   1. Opens Chrome (visible) with stealth plugin
 *   2. Navigates to 5ka.ru
 *   3. WAF JS challenge auto-resolves (or you solve captcha manually)
 *   4. Waits until the real site loads
 *   5. Does a test search to verify cookies work
 *   6. Saves session and closes
 *
 * After this, run `npm start` to launch the headless server —
 * it will reuse the saved cookies from the profile.
 */

const path = require('path');
const fs = require('fs');
const puppeteer = require('puppeteer-extra');
const StealthPlugin = require('puppeteer-extra-plugin-stealth');

puppeteer.use(StealthPlugin());

const USER_DATA_DIR = path.join(__dirname, '.chrome-profile');
const SEARCH_TEST_QUERY = 'молоко';

function log(msg) {
    console.log(`[${new Date().toISOString()}] ${msg}`);
}

function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

(async () => {
    if (!fs.existsSync(USER_DATA_DIR)) {
        fs.mkdirSync(USER_DATA_DIR, { recursive: true });
    }

    log('Opening VISIBLE Chrome browser...');
    log('If a captcha appears — solve it manually.');
    log('The script will wait for the real site to load.\n');

    const browser = await puppeteer.launch({
        headless: false,  // VISIBLE — you can see and interact
        userDataDir: USER_DATA_DIR,
        args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--disable-blink-features=AutomationControlled',
            '--window-size=1440,900',
            '--lang=ru-RU,ru',
            '--disable-background-timer-throttling',
            '--disable-backgrounding-occluded-windows',
            '--disable-renderer-backgrounding',
            '--enforce-webrtc-ip-permission-check',
        ],
        ignoreDefaultArgs: ['--enable-automation'],
        defaultViewport: null,  // Use actual window size
    });

    const pages = await browser.pages();
    const page = pages[0] || await browser.newPage();

    // Override navigator
    await page.evaluateOnNewDocument(() => {
        Object.defineProperty(navigator, 'webdriver', { get: () => false });
        window.chrome = {
            runtime: {},
            loadTimes: function() {},
            csi: function() {},
            app: { isInstalled: false },
        };
        Object.defineProperty(navigator, 'languages', {
            get: () => ['ru-RU', 'ru', 'en-US', 'en'],
        });
    });

    // Step 1: Navigate to 5ka.ru
    log('Navigating to https://5ka.ru ...');
    await page.goto('https://5ka.ru', { waitUntil: 'networkidle2', timeout: 60000 });

    // Step 2: Wait for WAF challenge to resolve
    log('Waiting for WAF challenge resolution...');
    const MAX_WAIT = 120; // 2 minutes
    let resolved = false;

    for (let i = 0; i < MAX_WAIT; i++) {
        try {
            const hasApp = !!(await page.$('input.searchPanel_input__CiSR3') || await page.$('.chakra-link') || await page.$('input[placeholder="Поиск"]'));
            const title = await page.title();

            if (hasApp && title) {
                log(`✅ Site loaded! Title: "${title}"`);
                resolved = true;
                break;
            }
        } catch (_) {
            // Execution context destroyed during navigation — that's fine,
            // WAF is redirecting us. Just wait and try again.
        }

        if (i % 10 === 0 && i > 0) {
            log(`Still waiting... (${i}s). If you see a captcha, please solve it.`);
        }
        await sleep(1000);
    }

    if (!resolved) {
        log('❌ Site did not load within 2 minutes.');
        log('Try again or check if 5ka.ru is reachable.');
        await browser.close();
        process.exit(1);
    }

    // Step 3: Verify cookies
    const cookies = await page.cookies();
    log(`Cookies saved: ${cookies.length}`);

    const importantCookies = cookies
        .filter(c => ['qrator_jsid', 'spid', 'spsn', 'sp_ssid'].includes(c.name))
        .map(c => `${c.name}=${c.value.substring(0, 10)}...`);

    if (importantCookies.length > 0) {
        log(`Key WAF cookies: ${importantCookies.join(', ')}`);
    } else {
        log('⚠️ No key WAF cookies found — session may not persist.');
    }

    // Step 4: Test search
    log(`\nTesting search for "${SEARCH_TEST_QUERY}"...`);
    await page.goto(
        `https://5ka.ru/search/?text=${encodeURIComponent(SEARCH_TEST_QUERY)}`,
        { waitUntil: 'networkidle2', timeout: 30000 }
    );
    await sleep(3000);

    // Check __NEXT_DATA__
    const nextData = await page.evaluate(() => {
        const el = document.getElementById('__NEXT_DATA__');
        return el ? el.textContent : null;
    });

    if (nextData) {
        try {
            const data = JSON.parse(nextData);
            const json = JSON.stringify(data);
            const hasProducts = json.includes('"products"') || json.includes('"items"');
            log(hasProducts
                ? '✅ Search works! Product data found in __NEXT_DATA__.'
                : '⚠️ __NEXT_DATA__ found but no products array.');
        } catch (e) {
            log('⚠️ Could not parse __NEXT_DATA__: ' + e.message);
        }
    } else {
        log('⚠️ No __NEXT_DATA__ found on search page.');
    }

    // Step 5: Close
    log('\n✅ Session initialized successfully!');
    log(`Profile saved to: ${USER_DATA_DIR}`);
    log('Now run: npm start');
    log('The headless server will reuse these cookies.\n');

    await browser.close();
    process.exit(0);
})();
