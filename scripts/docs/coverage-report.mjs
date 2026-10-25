import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const backendSummary = join(root, 'coveragereport', 'Summary.json');
const frontendLcov = join(root, 'web', 'coverage', 'oficina-web', 'lcov.info');
const target = join(root, 'docs', 'qualidade');
const images = join(target, 'img');
const minimum = 85;
const criticalMinimum = 80;
const critical = [
  'Oficina.Domain.ServiceOrders',
  'Oficina.Domain.Quotes',
  'Oficina.Domain.Customers',
  'Oficina.Domain.Inventory',
  'Oficina.Domain.Catalog',
  'Oficina.Application.ServiceOrders',
  'Oficina.Application.Quotes',
  'Oficina.Application.Customers',
  'Oficina.Application.Inventory',
  'Oficina.Application.Catalog',
];

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}

if (!existsSync(backendSummary)) {
  fail('Rode os testes com cobertura e o ReportGenerator (README, seção 9) antes deste script.');
}

const backend = JSON.parse(readFileSync(backendSummary, 'utf8'));
const percent = (covered, total) => (total === 0 ? 100 : Math.round((covered / total) * 1000) / 10);
const format = (value) => `${value.toFixed(1).replace('.', ',')}%`;
const status = (value, limit) => (value >= limit ? '✅' : '❌');

const assemblies = backend.coverage.assemblies.map((assembly) => ({
  name: assembly.name,
  lines: percent(assembly.coveredlines, assembly.coverablelines),
  branches: percent(assembly.coveredbranches ?? 0, assembly.totalbranches ?? 0),
  covered: assembly.coveredlines,
  coverable: assembly.coverablelines,
}));

const classes = backend.coverage.assemblies.flatMap((assembly) => assembly.classesinassembly);
const namespaces = critical.map((prefix) => {
  const matching = classes.filter((item) => item.name.startsWith(`${prefix}.`));
  const covered = matching.reduce((sum, item) => sum + item.coveredlines, 0);
  const coverable = matching.reduce((sum, item) => sum + item.coverablelines, 0);
  return { name: prefix, lines: percent(covered, coverable), covered, coverable, classes: matching.length };
});

const totalLines = percent(backend.summary.coveredlines, backend.summary.coverablelines);
const totalBranches = percent(backend.summary.coveredbranches, backend.summary.totalbranches);

function lcovTotals(path) {
  const totals = { LF: 0, LH: 0, BRF: 0, BRH: 0, FNF: 0, FNH: 0 };
  for (const line of readFileSync(path, 'utf8').split(/\r?\n/)) {
    const [key, value] = line.split(':');
    if (key in totals) {
      totals[key] += Number(value);
    }
  }

  return {
    lines: { pct: percent(totals.LH, totals.LF), covered: totals.LH, total: totals.LF },
    branches: { pct: percent(totals.BRH, totals.BRF) },
    functions: { pct: percent(totals.FNH, totals.FNF) },
  };
}

const frontend = existsSync(frontendLcov) ? lcovTotals(frontendLcov) : null;

const lines = [
  '# Cobertura dos testes',
  '',
  `Gerado em ${backend.summary.generatedon} por \`node scripts/docs/coverage-report.mjs\`, a partir do ReportGenerator (\`coveragereport/Summary.json\`) e do relatório do Vitest (\`web/coverage/oficina-web/lcov.info\`).`,
  '',
  'O enunciado pede 80% nos domínios críticos. O CI exige 85% de linhas sobre todo o `src/` do back-end (só `Migrations/` e `Program.cs` ficam fora) e 85% de linhas no front-end.',
  '',
  '## Back-end (testes unitários, de contrato e funcionais somados)',
  '',
  '| Indicador | Valor | Mínimo | Situação |',
  '|---|---|---|---|',
  `| Linhas | ${format(totalLines)} (${backend.summary.coveredlines} de ${backend.summary.coverablelines}) | ${minimum}% | ${status(totalLines, minimum)} |`,
  `| Branches | ${format(totalBranches)} (${backend.summary.coveredbranches} de ${backend.summary.totalbranches}) | — | — |`,
  `| Métodos | ${format(backend.summary.methodcoverage)} | — | — |`,
  '',
  '### Por projeto',
  '',
  '| Projeto | Linhas | Branches | Linhas cobertas |',
  '|---|---|---|---|',
  ...assemblies.map((item) => `| \`${item.name}\` | ${format(item.lines)} | ${format(item.branches)} | ${item.covered} de ${item.coverable} |`),
  '',
  '### Domínios críticos',
  '',
  '| Namespace | Classes | Linhas | Mínimo | Situação |',
  '|---|---|---|---|---|',
  ...namespaces.map((item) => `| \`${item.name}\` | ${item.classes} | ${format(item.lines)} (${item.covered} de ${item.coverable}) | ${criticalMinimum}% | ${status(item.lines, criticalMinimum)} |`),
  '',
];

if (frontend) {
  lines.push(
    '## Front-end (Vitest)',
    '',
    '| Indicador | Valor | Mínimo | Situação |',
    '|---|---|---|---|',
    `| Linhas | ${format(frontend.lines.pct)} (${frontend.lines.covered} de ${frontend.lines.total}) | ${minimum}% | ${status(frontend.lines.pct, minimum)} |`,
    `| Branches | ${format(frontend.branches.pct)} | — | — |`,
    `| Funções | ${format(frontend.functions.pct)} | — | — |`,
    '',
  );
}

lines.push(
  '## Captura do relatório',
  '',
  '![Resumo do ReportGenerator](img/reportgenerator-resumo.png)',
  '',
  '## Como gerar de novo',
  '',
  '1. Rode os testes do back-end com cobertura e o ReportGenerator (comandos no README, seção 9), incluindo `-reporttypes:Html;TextSummary;JsonSummary`.',
  '2. Rode `npm test` dentro de `web/`.',
  '3. Rode `node scripts/docs/coverage-report.mjs` na raiz.',
  '',
  'O relatório HTML completo fica em `coveragereport/index.html` (não versionado; o CI publica como artefato).',
  '',
);

mkdirSync(images, { recursive: true });
writeFileSync(join(target, 'cobertura.md'), lines.join('\n'));

const { chromium } = createRequire(join(root, 'web', 'package.json'))('@playwright/test');
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
await page.goto(pathToFileURL(join(root, 'coveragereport', 'index.html')).href);
await page.screenshot({ path: join(images, 'reportgenerator-resumo.png'), fullPage: false });
await browser.close();

const failures = [
  ...(totalLines < minimum ? ['back-end'] : []),
  ...(frontend && frontend.lines.pct < minimum ? ['front-end'] : []),
  ...namespaces.filter((item) => item.lines < criticalMinimum).map((item) => item.name),
];
if (failures.length > 0) {
  fail(`Cobertura abaixo do mínimo: ${failures.join(', ')}`);
}

process.stdout.write(`docs/qualidade/cobertura.md: back-end ${format(totalLines)}${frontend ? `, front-end ${format(frontend.lines.pct)}` : ''}\n`);
