import { execFileSync, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const envFile = join(root, '.env');
const reportDir = join(root, 'docs', 'seguranca');
const imageDir = join(reportDir, 'img');
const host = 'http://localhost:9000';
const webScanner = '@sonar/scan@5.0.1';
const projects = [
  { key: 'mecanicaos-api', name: 'mecanicaOS · API', analyze: analyzeApi },
  { key: 'mecanicaos-web', name: 'mecanicaOS · Web', analyze: analyzeWeb },
];
const reviews = JSON.parse(readFileSync(join(root, 'scripts', 'sonar', 'reviews.json'), 'utf8'));
const args = new Set(process.argv.slice(2));

if (!existsSync(envFile)) {
  fail('crie o .env a partir do .env.example (cp .env.example .env).');
}

process.loadEnvFile(envFile);
const adminPassword = process.env.SONAR_ADMIN_PASSWORD ?? '';
if (adminPassword.length < 12) {
  fail('defina SONAR_ADMIN_PASSWORD no .env (mínimo 12 caracteres).');
}

function fail(message) {
  process.stderr.write(`[sonar] ${message}\n`);
  process.exit(1);
}

function log(message) {
  process.stdout.write(`[sonar] ${message}\n`);
}

function run(command, commandArgs, options = {}) {
  const { label, ...spawnOptions } = options;
  const result = spawnSync(command, commandArgs, { cwd: root, stdio: 'inherit', shell: process.platform === 'win32' && ['npm', 'npx'].includes(command), ...spawnOptions });
  if (result.status !== 0) {
    fail(`falhou: ${label ?? `${command} ${commandArgs.join(' ')}`}`);
  }
}

function compose(...composeArgs) {
  run('docker', ['compose', '--profile', 'quality', ...composeArgs]);
}

async function call(path, { method = 'GET', auth, form } = {}) {
  const headers = { Authorization: `Basic ${Buffer.from(auth).toString('base64')}` };
  const body = form ? new URLSearchParams(form) : undefined;
  const response = await fetch(`${host}${path}`, { method, headers, body });
  const text = await response.text();
  return { status: response.status, json: text ? safeJson(text) : null };
}

function safeJson(text) {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function waitUntilUp() {
  for (let attempt = 0; attempt < 90; attempt++) {
    try {
      const response = await fetch(`${host}/api/system/status`);
      if ((await response.json()).status === 'UP') {
        return;
      }
    } catch {
      await sleep(2000);
      continue;
    }

    await sleep(2000);
  }

  fail('o SonarQube não ficou disponível.');
}

async function adminAuth() {
  const current = `admin:${adminPassword}`;
  if ((await call('/api/authentication/validate', { auth: current })).json?.valid) {
    return current;
  }

  const changed = await call('/api/users/change_password', {
    method: 'POST',
    auth: 'admin:admin',
    form: { login: 'admin', previousPassword: 'admin', password: adminPassword },
  });
  if (changed.status !== 204) {
    fail('não foi possível entrar como admin. Confira SONAR_ADMIN_PASSWORD ou apague o volume oficina-sonarqube-data.');
  }

  log('senha padrão do admin trocada pela SONAR_ADMIN_PASSWORD.');
  return current;
}

async function analysisToken(auth) {
  const saved = process.env.SONAR_TOKEN ?? '';
  if (saved && (await call('/api/authentication/validate', { auth: `${saved}:` })).json?.valid) {
    return saved;
  }

  const generated = await call('/api/user_tokens/generate', {
    method: 'POST',
    auth,
    form: { name: `mecanicaos-${Date.now()}`, type: 'GLOBAL_ANALYSIS_TOKEN' },
  });
  const token = generated.json?.token;
  if (!token) {
    fail('não foi possível gerar o token de análise.');
  }

  const content = readFileSync(envFile, 'utf8');
  const line = `SONAR_TOKEN=${token}`;
  writeFileSync(envFile, /^SONAR_TOKEN=.*$/m.test(content) ? content.replace(/^SONAR_TOKEN=.*$/m, line) : `${content.trimEnd()}\n${line}\n`);
  log('token de análise gerado e gravado no .env (não é exibido).');
  return token;
}

async function ensureProjects(auth) {
  for (const project of projects) {
    const found = await call(`/api/projects/search?projects=${project.key}`, { auth });
    if ((found.json?.components ?? []).length === 0) {
      await call('/api/projects/create', { method: 'POST', auth, form: { project: project.key, name: project.name } });
      log(`projeto ${project.key} criado.`);
    }
  }
}

function prepareWebCoverage() {
  if (!args.has('--reuse-web-coverage')) {
    run('npm', ['test'], { cwd: join(root, 'web') });
  }

  const source = join(root, 'web', 'coverage', 'oficina-web', 'lcov.info');
  if (!existsSync(source)) {
    fail('lcov do frontend não encontrado (rode npm test em web/).');
  }

  const normalized = readFileSync(source, 'utf8').replace(/^SF:(.*)$/gm, (_, path) => `SF:${path.replaceAll('\\', '/')}`);
  writeFileSync(join(root, 'web', 'coverage', 'oficina-web', 'lcov.sonar.info'), normalized);
}

async function waitForTasks(auth, key) {
  for (let attempt = 0; attempt < 180; attempt++) {
    const component = (await call(`/api/ce/component?component=${key}`, { auth })).json;
    if ((component?.queue ?? []).length === 0 && component?.current) {
      if (component.current.status !== 'SUCCESS') {
        fail(`processamento de ${key} terminou com ${component.current.status}.`);
      }

      return;
    }

    await sleep(2000);
  }

  fail(`processamento de ${key} não terminou.`);
}

async function applyReviews(auth, key) {
  const rules = [...new Set(reviews.filter((item) => item.kind === 'issue' && item.project === key).map((item) => item.rule))];
  const issues = rules.length === 0 ? [] : (await call(`/api/issues/search?componentKeys=${key}&rules=${rules.join(',')}&resolved=false&ps=500`, { auth })).json?.issues ?? [];
  for (const issue of issues) {
    const review = reviews.find((item) => item.kind === 'issue' && item.project === key && item.rule === issue.rule && issue.component.includes(item.path));
    if (review) {
      await call('/api/issues/add_comment', { method: 'POST', auth, form: { issue: issue.key, text: review.justification } });
      await call('/api/issues/do_transition', { method: 'POST', auth, form: { issue: issue.key, transition: 'accept' } });
    }
  }

  const hotspots = (await call(`/api/hotspots/search?project=${key}&ps=500`, { auth })).json?.hotspots ?? [];
  for (const hotspot of hotspots.filter((item) => item.status === 'TO_REVIEW')) {
    const review = reviews.find((item) => item.kind === 'hotspot' && item.project === key && item.rule === hotspot.ruleKey && hotspot.component.includes(item.path));
    if (review) {
      await call('/api/hotspots/change_status', {
        method: 'POST',
        auth,
        form: { hotspot: hotspot.key, status: 'REVIEWED', resolution: review.resolution, comment: review.justification },
      });
    }
  }
}

async function collect(auth, project) {
  const metrics = ['alert_status', 'bugs', 'vulnerabilities', 'security_hotspots', 'security_hotspots_reviewed', 'code_smells', 'coverage', 'duplicated_lines_density', 'ncloc', 'reliability_rating', 'security_rating', 'sqale_rating'];
  const measures = (await call(`/api/measures/component?component=${project.key}&metricKeys=${metrics.join(',')}`, { auth })).json?.component?.measures ?? [];
  const value = (metric) => measures.find((item) => item.metric === metric)?.value ?? '0';
  const gate = (await call(`/api/qualitygates/project_status?projectKey=${project.key}`, { auth })).json?.projectStatus;
  const issues = (await call(`/api/issues/search?componentKeys=${project.key}&types=BUG,VULNERABILITY&resolved=false&ps=100`, { auth })).json?.issues ?? [];
  const smells = (await call(`/api/issues/search?componentKeys=${project.key}&types=CODE_SMELL&resolved=false&ps=1&facets=severities`, { auth })).json;
  const hotspots = (await call(`/api/hotspots/search?project=${project.key}&ps=500`, { auth })).json?.hotspots ?? [];
  const severities = (smells?.facets ?? []).find((facet) => facet.property === 'severities')?.values ?? [];
  const accepted = (await call(`/api/issues/search?componentKeys=${project.key}&issueStatuses=ACCEPTED&ps=100`, { auth })).json?.issues ?? [];
  return { project, value, gate, issues, severities, hotspots, accepted };
}

function rating(value) {
  return ['', 'A', 'B', 'C', 'D', 'E'][Math.round(Number(value))] ?? value;
}

function reviewOf(project, hotspot) {
  return reviews.find((item) => item.kind === 'hotspot' && item.project === project.key && item.rule === hotspot.ruleKey && hotspot.component.includes(item.path));
}

function writeReport(version, results) {
  const commit = execFileSync('git', ['rev-parse', '--short', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim();
  const dirty = execFileSync('git', ['status', '--porcelain'], { cwd: root, encoding: 'utf8' }).trim() !== '';
  const lines = [
    '# Relatório do SonarQube',
    '',
    'Gerado por `node scripts/sonar/run-analysis.mjs` a partir da API do SonarQube local (Docker, profile `quality`). Os scanners rodam na máquina e baixam o Java do próprio SonarQube. Não editar à mão.',
    '',
    `- Data: ${new Date().toISOString()}`,
    `- SonarQube: ${version}`,
    `- Commit analisado: \`${commit}\`${dirty ? ' (com alterações locais ainda não commitadas)' : ''}`,
    '',
    '| Projeto | Quality gate | Bugs | Vulnerabilidades | Security hotspots (revisados) | Code smells | Cobertura | Duplicação | Confiabilidade | Segurança | Manutenibilidade | Linhas |',
    '|---|---|---|---|---|---|---|---|---|---|---|---|',
  ];
  for (const { project, value, gate } of results) {
    lines.push(`| ${project.name} | ${gate?.status === 'OK' ? 'Passed' : (gate?.status ?? 'n/d')} | ${value('bugs')} | ${value('vulnerabilities')} | ${value('security_hotspots')} (${value('security_hotspots_reviewed')}%) | ${value('code_smells')} | ${value('coverage')}% | ${value('duplicated_lines_density')}% | ${rating(value('reliability_rating'))} | ${rating(value('security_rating'))} | ${rating(value('sqale_rating'))} | ${value('ncloc')} |`);
  }

  for (const { project, issues, severities, hotspots, accepted } of results) {
    lines.push('', `## ${project.name}`, '', `![Painel do ${project.name}](img/sonarqube-${project.key}.png)`, '', '### Bugs e vulnerabilidades abertos', '');
    lines.push(issues.length === 0 ? 'Nenhum.' : '| Tipo | Severidade | Regra | Arquivo | Mensagem |\n|---|---|---|---|---|');
    for (const issue of issues) {
      lines.push(`| ${issue.type} | ${issue.severity} | ${issue.rule} | ${issue.component.split(':').pop()}:${issue.line ?? ''} | ${issue.message.replaceAll('|', '/')} |`);
    }

    lines.push('', '### Itens aceitos com justificativa', '');
    lines.push(accepted.length === 0 ? 'Nenhum.' : '| Regra | Arquivo | Justificativa |\n|---|---|---|');
    for (const issue of accepted) {
      const review = reviews.find((item) => item.kind === 'issue' && item.project === project.key && item.rule === issue.rule && issue.component.includes(item.path));
      lines.push(`| ${issue.rule} | ${issue.component.split(':').pop()}:${issue.line ?? ''} | ${review?.justification ?? ''} |`);
    }

    lines.push('', '### Code smells por severidade', '', '| Severidade | Quantidade |', '|---|---|');
    for (const severity of severities) {
      lines.push(`| ${severity.val} | ${severity.count} |`);
    }

    lines.push('', '### Security hotspots', '');
    lines.push(hotspots.length === 0 ? 'Nenhum.' : '| Regra | Arquivo | Status | Justificativa |\n|---|---|---|---|');
    for (const hotspot of hotspots) {
      const review = reviewOf(project, hotspot);
      const status = hotspot.status === 'REVIEWED' ? `Revisado (${hotspot.resolution})` : 'A revisar';
      lines.push(`| ${hotspot.ruleKey} | ${hotspot.component.split(':').pop()}:${hotspot.line ?? ''} | ${status} | ${review?.justification ?? ''} |`);
    }
  }

  mkdirSync(reportDir, { recursive: true });
  writeFileSync(join(reportDir, 'sonarqube-relatorio.md'), `${lines.join('\n')}\n`);
  log('relatório gravado em docs/seguranca/sonarqube-relatorio.md.');
}

async function screenshots() {
  const require = createRequire(join(root, 'web', 'package.json'));
  const { chromium } = require('@playwright/test');
  mkdirSync(imageDir, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto(`${host}/sessions/new`);
  await page.fill('input[name="login"]', 'admin');
  await page.fill('input[name="password"]', adminPassword);
  await page.press('input[name="password"]', 'Enter');
  await page.waitForURL((url) => !url.pathname.startsWith('/sessions'));
  for (const project of projects) {
    await page.goto(`${host}/dashboard?id=${project.key}`);
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: join(imageDir, `sonarqube-${project.key}.png`), fullPage: false });
  }

  await browser.close();
  log('prints do painel gravados em docs/seguranca/img/.');
}

function analyzeApi(project, token) {
  const results = join(root, 'TestResults', 'Sonar');
  rmSync(results, { recursive: true, force: true });
  run('dotnet', ['tool', 'restore']);
  run('dotnet', [
    'sonarscanner', 'begin',
    `/k:${project.key}`,
    `/n:${project.name}`,
    `/d:sonar.host.url=${host}`,
    `/d:sonar.token=${token}`,
    '/d:sonar.scm.disabled=true',
    '/d:sonar.scanner.scanAll=false',
    '/d:sonar.exclusions=**/Migrations/**,**/bin/**,**/obj/**',
    '/d:sonar.coverage.exclusions=**/Migrations/**,**/Program.cs,tests/**',
    '/d:sonar.cpd.exclusions=**/Migrations/**,tests/**',
    '/d:sonar.cs.opencover.reportsPaths=TestResults/Sonar/**/*.xml',
  ], { label: 'dotnet sonarscanner begin' });
  run('dotnet', ['build', 'Oficina.slnx', '--configuration', 'Release', '--no-incremental']);
  for (const tests of ['UnitTests', 'ContractTests', 'FunctionalTests']) {
    run('dotnet', [
      'test', '--project', `tests/Oficina.${tests}`, '--configuration', 'Release', '--no-build',
      '--results-directory', join(results, tests), '--coverlet', '--coverlet-output-format', 'opencover',
      '--coverlet-include', '[Oficina.Api]*', '--coverlet-include', '[Oficina.Application]*',
      '--coverlet-include', '[Oficina.Domain]*', '--coverlet-include', '[Oficina.Infrastructure]*',
      '--coverlet-exclude-by-file', '**/Program.cs', '--coverlet-exclude-by-file', '**/Migrations/*.cs',
    ]);
  }

  run('dotnet', ['sonarscanner', 'end', `/d:sonar.token=${token}`], { label: 'dotnet sonarscanner end' });
}

function analyzeWeb() {
  run('npx', ['--yes', webScanner], { cwd: join(root, 'web'), env: { ...process.env, SONAR_HOST_URL: host } });
}

compose('up', '--detach', '--wait', 'sonarqube');
await waitUntilUp();
const auth = await adminAuth();
const token = await analysisToken(auth);
process.env.SONAR_TOKEN = token;
await ensureProjects(auth);
prepareWebCoverage();
for (const project of projects) {
  log(`analisando ${project.key}...`);
  project.analyze(project, token);
  await waitForTasks(auth, project.key);
  await applyReviews(auth, project.key);
}

const version = await (await fetch(`${host}/api/server/version`)).text();
const results = [];
for (const project of projects) {
  results.push(await collect(auth, project));
}

await screenshots();
writeReport(version, results);
const failed = results.filter(({ gate }) => gate?.status !== 'OK');
if (failed.length > 0) {
  fail(`quality gate reprovado: ${failed.map(({ project }) => project.key).join(', ')}`);
}

log('quality gate Passed nos dois projetos.');
if (args.has('--stop')) {
  compose('stop', 'sonarqube');
}
