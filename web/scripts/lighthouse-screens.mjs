import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';
import { launch } from 'chrome-launcher';
import { desktopConfig, startFlow } from 'lighthouse';
import puppeteer from 'puppeteer-core';

if (existsSync('../.env')) {
  process.loadEnvFile('../.env');
}

const base = process.env['DEMO_URL'] ?? 'http://localhost:4301';
const minimumPerformance = 90;
const minimumAccessibility = 95;
const maximumBlockingMs = 200;
const maximumShift = 0.1;

const accounts = {
  admin: {
    email: process.env.DEV_ADMIN_EMAIL ?? 'admin@oficina.local',
    password: process.env.DEV_ADMIN_PASSWORD ?? '',
  },
  mechanic: { email: 'mecanico.e2e@oficina.local', password: 'Oficina@2026' },
};

const journeys = [
  {
    account: 'admin',
    screens: [
      { name: 'painel', link: null, heading: 'Painel' },
      { name: 'orçamentos', link: 'Orçamentos', heading: 'Orçamentos' },
      { name: 'ordens de serviço', link: 'Ordens de serviço', heading: 'Ordens de serviço' },
    ],
  },
  {
    account: 'mechanic',
    screens: [{ name: 'modo mecânico', link: null, heading: 'Modo mecânico' }],
  },
];

const mobile = {
  formFactor: 'mobile',
  screenEmulation: { mobile: true, width: 390, height: 844, deviceScaleFactor: 3, disabled: false },
};

function score(result, category) {
  return Math.round((result.categories[category]?.score ?? 0) * 100);
}

async function waitForHeading(page, text) {
  await page.waitForFunction(
    `document.querySelector('h1')?.textContent?.trim() === ${JSON.stringify(text)}`,
    { timeout: 30_000 },
  );
}

async function openScreen(page, link) {
  const toggle = await page.$('button[aria-controls="app-sidebar"]');
  if (toggle) {
    await toggle.click();
  }

  const [anchor] = await page.$$(`xpath/.//nav//a[normalize-space()="${link}"]`);
  await anchor?.click();
}

const failures = [];
const lines = [];
const chrome = await launch({
  chromePath: chromium.executablePath(),
  chromeFlags: ['--headless=new', '--no-sandbox', '--disable-gpu'],
});
const browser = await puppeteer.connect({ browserURL: `http://127.0.0.1:${chrome.port}` });

try {
  for (const formFactor of ['desktop', 'mobile']) {
    const config = formFactor === 'desktop' ? desktopConfig : undefined;
    const settings = formFactor === 'desktop' ? undefined : mobile;
    const portal = await browser.newPage();
    const portalFlow = await startFlow(portal, { config, flags: settings });
    await portalFlow.navigate(`${base}/o/portal-demo-3324`, { name: 'portal' });
    for (const journey of journeys) {
      const page = await browser.newPage();
      const flow = await startFlow(page, { config, flags: settings });
      await flow.navigate(`${base}/login`, { name: 'login' });
      const account = accounts[journey.account];
      await page.type('input[type="email"]', account.email);
      await page.type('input[autocomplete="current-password"]', account.password);
      await page.click('button[type="submit"]');
      for (const screen of journey.screens) {
        if (screen.link) {
          await flow.startTimespan({ name: screen.name });
          await openScreen(page, screen.link);
          await waitForHeading(page, screen.heading);
          await new Promise((resolve) => setTimeout(resolve, 1500));
          await flow.endTimespan();
        } else {
          await waitForHeading(page, screen.heading);
          await new Promise((resolve) => setTimeout(resolve, 1500));
        }

        await flow.snapshot({ name: screen.name });
      }

      const results = (await flow.createFlowResult()).steps;
      for (const step of results) {
        const result = step.lhr;
        const label = `${step.name} (${formFactor}, ${result.gatherMode})`;
        if (result.gatherMode === 'navigation') {
          const performance = score(result, 'performance');
          const accessibility = score(result, 'accessibility');
          lines.push(`${label}: desempenho ${performance}, acessibilidade ${accessibility}`);
          if (performance < minimumPerformance || accessibility < minimumAccessibility) {
            failures.push(label);
          }
        } else if (result.gatherMode === 'snapshot') {
          const accessibility = score(result, 'accessibility');
          lines.push(`${label}: acessibilidade ${accessibility}`);
          if (accessibility < minimumAccessibility) {
            failures.push(label);
          }
        } else {
          const blocking = result.audits['total-blocking-time']?.numericValue ?? 0;
          const shift = result.audits['cumulative-layout-shift']?.numericValue ?? 0;
          lines.push(
            `${label}: bloqueio ${Math.round(blocking)} ms, deslocamento ${shift.toFixed(3)}`,
          );
          if (blocking > maximumBlockingMs || shift > maximumShift) {
            failures.push(label);
          }
        }
      }

      await page.close();
    }

    const portalSteps = (await portalFlow.createFlowResult()).steps;
    for (const step of portalSteps) {
      const result = step.lhr;
      const performance = score(result, 'performance');
      const accessibility = score(result, 'accessibility');
      lines.push(
        `portal (${formFactor}, navigation): desempenho ${performance}, acessibilidade ${accessibility}`,
      );
      if (performance < minimumPerformance || accessibility < minimumAccessibility) {
        failures.push(`portal (${formFactor})`);
      }
    }

    await portal.close();
  }
} finally {
  await browser.disconnect();
  chrome.kill();
}

process.stdout.write(`${lines.join('\n')}\n`);
if (failures.length > 0) {
  process.stderr.write(`Below the minimum: ${failures.join(', ')}\n`);
  process.exit(1);
}
