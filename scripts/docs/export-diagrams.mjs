import { mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const sources = join(root, 'docs', 'arquitetura');
const target = join(root, 'docs', 'ddd');
const images = join(target, 'img');
const viewer = 'https://viewer.diagrams.net/js/viewer-static.min.js';
const border = 24;
const maxPdfWidth = 3200;
const require = createRequire(join(root, 'web', 'package.json'));
const { chromium } = require('@playwright/test');
const { PDFDocument } = require('pdf-lib');

const documents = {
  eventStorming: 'EventStorming_mecanicaOS',
  ddd: 'DDD_mecanicaOS',
  storytelling: 'DomainStorytelling_mecanicaOS',
  c4: 'C4_Oficina',
};

const slug = (name) =>
  name
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');

function pagesOf(xml) {
  return [...xml.matchAll(/<diagram\b[^>]*\bname="([^"]*)"/g)].map((match) =>
    match[1].replaceAll('&amp;', '&').replaceAll('&quot;', '"').replaceAll('&lt;', '<').replaceAll('&gt;', '>'),
  );
}

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1200 } });
await page.setContent('<!doctype html><html><head><meta charset="utf-8"><style>html,body{margin:0;background:#fff}</style></head><body></body></html>');
await page.addScriptTag({ url: viewer });

async function render(xml, index) {
  await page.evaluate(
    ({ xml, index, border }) => {
      document.body.innerHTML = '';
      const element = document.createElement('div');
      element.className = 'mxgraph';
      element.setAttribute('data-mxgraph', JSON.stringify({ xml, page: index, nav: false, resize: false, toolbar: '', lightbox: false, border, 'check-visible-state': false }));
      document.body.appendChild(element);
      window.GraphViewer.processElements();
    },
    { xml, index, border },
  );
  const svg = page.locator('.mxgraph svg').first();
  await svg.waitFor();
  await page.waitForTimeout(300);
  return page.evaluate(() => {
    const element = document.querySelector('.mxgraph svg');
    const width = Math.ceil(parseFloat(element.style.minWidth));
    const height = Math.ceil(parseFloat(element.style.minHeight));
    const clone = element.cloneNode(true);
    clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
    clone.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
    clone.setAttribute('width', String(width));
    clone.setAttribute('height', String(height));
    clone.setAttribute('viewBox', `0 0 ${width} ${height}`);
    clone.style.cssText = 'background-color:#ffffff';
    return { svg: `<?xml version="1.0" encoding="UTF-8"?>\n${new XMLSerializer().serializeToString(clone)}`, width, height };
  });
}

async function pdfOf(rendered) {
  await page.setContent(`<!doctype html><html><head><meta charset="utf-8"><style>@page{margin:0}html,body{margin:0;background:#fff}</style></head><body>${rendered.svg.replace(/^<\?xml[^>]*>\s*/, '')}</body></html>`);
  await page.evaluate(() => document.fonts.ready);
  const scale = Math.min(1, maxPdfWidth / rendered.width);
  const pdf = await page.pdf({ width: `${Math.ceil(rendered.width * scale)}px`, height: `${Math.ceil(rendered.height * scale)}px`, scale, printBackground: true, pageRanges: '1' });
  await page.setContent('<!doctype html><html><head><meta charset="utf-8"><style>html,body{margin:0;background:#fff}</style></head><body></body></html>');
  await page.addScriptTag({ url: viewer });
  return pdf;
}

rmSync(images, { recursive: true, force: true });
mkdirSync(images, { recursive: true });

const exported = {};
for (const [key, name] of Object.entries(documents)) {
  const xml = readFileSync(join(sources, `${name}.drawio`), 'utf8');
  exported[key] = [];
  for (const [index, title] of pagesOf(xml).entries()) {
    const rendered = await render(xml, index);
    const file = `${name}-${index + 1}-${slug(title.replace(/^(\d+\.|Event Storming \d+ -)\s*/, ''))}.svg`;
    writeFileSync(join(images, file), rendered.svg);
    exported[key].push({ title, file, pdf: await pdfOf(rendered) });
    process.stdout.write(`${file}\n`);
  }
}

await browser.close();

async function merge(output, parts) {
  const merged = await PDFDocument.create();
  merged.setTitle(output.replace('.pdf', ''));
  for (const part of parts) {
    const document = await PDFDocument.load(part.pdf);
    for (const copied of await merged.copyPages(document, document.getPageIndices())) {
      merged.addPage(copied);
    }
  }

  writeFileSync(join(target, output), await merged.save());
  process.stdout.write(`${output}: ${merged.getPageCount()} páginas\n`);
}

await merge('event-storming.pdf', exported.eventStorming);
await merge('diagramas.pdf', [...exported.ddd, ...exported.storytelling, ...exported.c4]);
