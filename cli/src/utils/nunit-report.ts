// The NUnit 3 report Unity's test runner writes (`-testResults`), parsed without an XML dependency (Unity's report is
// machine-written: elements, attributes and CDATA), plus JUnit output and GitHub Actions annotations built from it.

import * as path from 'path';

export type TestOutcome = 'passed' | 'failed' | 'skipped' | 'inconclusive';

export interface TestCaseResult {
  /** `Namespace.Fixture.Method` — what `--filter` matches. */
  fullName: string;
  name: string;
  /** The fixture: `fullName` without the method. */
  className: string;
  outcome: TestOutcome;
  durationSeconds: number;
  message?: string;
  stackTrace?: string;
}

export interface TestRunSummary {
  total: number;
  passed: number;
  failed: number;
  skipped: number;
  inconclusive: number;
  durationSeconds: number;
  /** The run's own verdict attribute (`Passed`, `Failed`, `Failed(Child)`, ...). */
  result: string;
}

export interface TestReport {
  summary: TestRunSummary;
  tests: TestCaseResult[];
}

const ENTITIES: Record<string, string> = { amp: '&', lt: '<', gt: '>', quot: '"', apos: "'" };

export function decodeXml(text: string): string {
  return text.replace(/&(#x[0-9a-f]+|#\d+|amp|lt|gt|quot|apos);/gi, (_, entity: string) => {
    if (entity[0] === '#') {
      const code = entity[1] === 'x' || entity[1] === 'X' ? parseInt(entity.slice(2), 16) : parseInt(entity.slice(1), 10);
      return Number.isFinite(code) ? String.fromCodePoint(code) : '';
    }
    return ENTITIES[entity.toLowerCase()] ?? '';
  });
}

export function encodeXml(text: string): string {
  // Characters XML 1.0 cannot carry at all are dropped (a test message may hold any byte).
  // eslint-disable-next-line no-control-regex
  return text.replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F]/g, '')
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&apos;');
}

function attributes(tag: string): Record<string, string> {
  const attrs: Record<string, string> = {};
  for (const m of tag.matchAll(/([\w:-]+)\s*=\s*("([^"]*)"|'([^']*)')/g)) {
    attrs[m[1]] = decodeXml(m[3] ?? m[4] ?? '');
  }
  return attrs;
}

/** The text of the first `<name>` element in `xml` (CDATA or escaped text), or undefined. */
function elementText(xml: string, name: string): string | undefined {
  const m = new RegExp(`<${name}(?:\\s[^>]*)?>([\\s\\S]*?)</${name}>`).exec(xml);
  if (!m) return undefined;
  const inner = m[1];
  const parts = [...inner.matchAll(/<!\[CDATA\[([\s\S]*?)\]\]>/g)];
  const text = parts.length > 0 ? parts.map(p => p[1]).join('') : decodeXml(inner);
  const trimmed = text.trim();
  return trimmed.length > 0 ? trimmed : undefined;
}

function toNumber(value: string | undefined): number {
  const n = value === undefined ? NaN : Number(value);
  return Number.isFinite(n) ? n : 0;
}

function outcomeOf(result: string | undefined): TestOutcome {
  switch ((result ?? '').toLowerCase()) {
    case 'passed': return 'passed';
    case 'failed': return 'failed';
    case 'skipped': return 'skipped';
    default: return 'inconclusive';
  }
}

/**
 * Parse an NUnit 3 report. Returns undefined when `xml` holds no `<test-run>` (not a report, or a truncated file).
 */
export function parseNUnitReport(xml: string): TestReport | undefined {
  const runTag = /<test-run\b[^>]*>/.exec(xml);
  if (!runTag) return undefined;
  const run = attributes(runTag[0]);

  const tests: TestCaseResult[] = [];
  for (const m of xml.matchAll(/<test-case\b([^>]*?)(\/>|>([\s\S]*?)<\/test-case>)/g)) {
    const attrs = attributes(m[1]);
    const body = m[3] ?? '';
    const fullName = attrs.fullname ?? attrs.name ?? '';
    const name = attrs.name ?? fullName;
    const className = attrs.classname
      ?? (fullName.endsWith(`.${name}`) ? fullName.slice(0, fullName.length - name.length - 1) : fullName);
    const failure = /<failure\b[\s\S]*?<\/failure>/.exec(body)?.[0];
    const reason = /<reason\b[\s\S]*?<\/reason>/.exec(body)?.[0];
    const detail = failure ?? reason;
    const message = detail ? elementText(detail, 'message') : undefined;
    const stackTrace = failure ? elementText(failure, 'stack-trace') : undefined;
    tests.push({
      fullName,
      name,
      className,
      outcome: outcomeOf(attrs.result),
      durationSeconds: toNumber(attrs.duration),
      ...(message !== undefined ? { message } : {}),
      ...(stackTrace !== undefined ? { stackTrace } : {}),
    });
  }

  const count = (outcome: TestOutcome) => tests.filter(t => t.outcome === outcome).length;
  // The run's own counters win; the per-case count covers a writer that leaves them out.
  const summary: TestRunSummary = {
    total: run.total !== undefined ? toNumber(run.total) : tests.length,
    passed: run.passed !== undefined ? toNumber(run.passed) : count('passed'),
    failed: run.failed !== undefined ? toNumber(run.failed) : count('failed'),
    skipped: run.skipped !== undefined ? toNumber(run.skipped) : count('skipped'),
    inconclusive: run.inconclusive !== undefined ? toNumber(run.inconclusive) : count('inconclusive'),
    durationSeconds: toNumber(run.duration),
    result: run.result ?? '',
  };
  return { summary, tests };
}

/** A JUnit XML document of `report` (one `<testsuite>` per fixture), the format CI dashboards read. */
export function toJUnitXml(report: TestReport, suiteName = 'Unity'): string {
  const byClass = new Map<string, TestCaseResult[]>();
  for (const t of report.tests) {
    const list = byClass.get(t.className) ?? [];
    list.push(t);
    byClass.set(t.className, list);
  }
  const lines = ['<?xml version="1.0" encoding="UTF-8"?>'];
  const { summary } = report;
  lines.push(`<testsuites name="${encodeXml(suiteName)}" tests="${summary.total}" failures="${summary.failed}" skipped="${summary.skipped + summary.inconclusive}" time="${summary.durationSeconds}">`);
  for (const [className, cases] of byClass) {
    const failures = cases.filter(c => c.outcome === 'failed').length;
    const skipped = cases.filter(c => c.outcome === 'skipped' || c.outcome === 'inconclusive').length;
    const time = cases.reduce((sum, c) => sum + c.durationSeconds, 0);
    lines.push(`  <testsuite name="${encodeXml(className)}" tests="${cases.length}" failures="${failures}" skipped="${skipped}" time="${round(time)}">`);
    for (const c of cases) {
      const open = `    <testcase classname="${encodeXml(c.className)}" name="${encodeXml(c.name)}" time="${round(c.durationSeconds)}"`;
      if (c.outcome === 'failed') {
        lines.push(`${open}>`);
        lines.push(`      <failure message="${encodeXml(c.message ?? 'Test failed')}">${encodeXml(c.stackTrace ?? c.message ?? '')}</failure>`);
        lines.push('    </testcase>');
      } else if (c.outcome === 'skipped' || c.outcome === 'inconclusive') {
        lines.push(`${open}>`);
        lines.push(`      <skipped${c.message ? ` message="${encodeXml(c.message)}"` : ''}/>`);
        lines.push('    </testcase>');
      } else {
        lines.push(`${open}/>`);
      }
    }
    lines.push('  </testsuite>');
  }
  lines.push('</testsuites>');
  return lines.join('\n') + '\n';
}

function round(n: number): number {
  return Math.round(n * 1000) / 1000;
}

/**
 * The first `file:line` of `stackTrace` inside `projectPath`, relative to it with `/` separators. Unity prints frames
 * as `at X.Y () [0x00001] in C:\proj\Assets\Tests\FooTests.cs:12` (Mono) or `(at Assets/Tests/FooTests.cs:12)`.
 */
export function sourceLocation(stackTrace: string | undefined, projectPath: string): { file: string; line: number } | undefined {
  if (!stackTrace) return undefined;
  const root = path.resolve(projectPath);
  for (const m of stackTrace.matchAll(/(?:\bin |\(at )((?:[A-Za-z]:)?[^:\r\n()]+\.cs):(\d+)/g)) {
    const file = m[1].trim();
    const absolute = path.isAbsolute(file) ? path.resolve(file) : path.resolve(root, file);
    const relative = path.relative(root, absolute);
    if (relative.startsWith('..') || path.isAbsolute(relative)) continue;
    return { file: relative.split(path.sep).join('/'), line: parseInt(m[2], 10) };
  }
  return undefined;
}

/** Escape a GitHub workflow command's message (data) part. */
export function escapeAnnotationData(text: string): string {
  return text.replace(/%/g, '%25').replace(/\r/g, '%0D').replace(/\n/g, '%0A');
}

/** Escape a GitHub workflow command's property value. */
export function escapeAnnotationProperty(text: string): string {
  return escapeAnnotationData(text).replace(/:/g, '%3A').replace(/,/g, '%2C');
}

/** One `::error` line per failed test, anchored at its source line when the stack trace has one inside the project. */
export function toGitHubAnnotations(report: TestReport, projectPath: string): string[] {
  return report.tests.filter(t => t.outcome === 'failed').map((t) => {
    const location = sourceLocation(t.stackTrace, projectPath);
    const props = [
      ...(location ? [`file=${escapeAnnotationProperty(location.file)}`, `line=${location.line}`] : []),
      `title=${escapeAnnotationProperty(t.fullName)}`,
    ].join(',');
    return `::error ${props}::${escapeAnnotationData(t.message ?? 'Test failed')}`;
  });
}
