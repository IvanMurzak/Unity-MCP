// NUnit reports from Unity, plus JUnit output and GitHub Actions annotations.

import * as path from 'path';
import { SaxesParser } from 'saxes';

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
  /** A fixture's setup or teardown failed (not a test case). */
  suiteFailure?: boolean;
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
      return Number.isFinite(code) && code >= 0 && code <= 0x10ffff ? String.fromCodePoint(code) : '';
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

interface ReportElement {
  name: string;
  attributes: Record<string, string>;
  children: ReportElement[];
  text: string;
}

function readDocument(xml: string): ReportElement | undefined {
  const parser = new SaxesParser({ xmlns: false });
  const stack: ReportElement[] = [];
  let root: ReportElement | undefined;
  parser.on('opentag', (tag) => {
    const element: ReportElement = { name: tag.name, attributes: tag.attributes, children: [], text: '' };
    const parent = stack[stack.length - 1];
    if (parent) parent.children.push(element);
    else root = element;
    stack.push(element);
  });
  const appendText = (text: string) => {
    const element = stack[stack.length - 1];
    if (element?.name === 'message' || element?.name === 'stack-trace') element.text += text;
  };
  parser.on('text', appendText);
  parser.on('cdata', appendText);
  parser.on('closetag', () => { stack.pop(); });
  parser.on('doctype', () => { throw new Error('NUnit reports do not require a DTD.'); });
  try {
    parser.write(xml).close();
    return root;
  } catch {
    return undefined;
  }
}

function child(element: ReportElement, name: string): ReportElement | undefined {
  return element.children.find(node => node.name === name);
}

function elementText(element: ReportElement | undefined, name: string): string | undefined {
  const text = element && child(element, name)?.text.replace(/\r\n?/g, '\n').trim();
  return text || undefined;
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
 * Parse a complete NUnit 3 document. Invalid XML or a root other than test-run returns undefined.
 */
export function parseNUnitReport(xml: string): TestReport | undefined {
  const root = readDocument(xml);
  if (root?.name !== 'test-run') return undefined;
  const run = root.attributes;
  const cases: ReportElement[] = [];
  const suites: ReportElement[] = [];
  const pending = [root];
  while (pending.length > 0) {
    const element = pending.pop()!;
    if (element.name === 'test-case') cases.push(element);
    else {
      if (element.name === 'test-suite') suites.push(element);
      for (let i = element.children.length - 1; i >= 0; i--) {
        const node = element.children[i];
        if (node.name === 'test-case' || node.name === 'test-suite') pending.push(node);
      }
    }
  }

  const tests: TestCaseResult[] = [];
  for (const element of cases) {
    const attrs = element.attributes;
    const fullName = attrs.fullname ?? attrs.name ?? '';
    const name = attrs.name ?? fullName;
    const className = attrs.classname
      ?? (fullName.endsWith(`.${name}`) ? fullName.slice(0, fullName.length - name.length - 1) : fullName);
    const failure = child(element, 'failure');
    const detail = failure ?? child(element, 'reason');
    const message = elementText(detail, 'message');
    const stackTrace = elementText(failure, 'stack-trace');
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

  // A fixture's OneTimeSetUp/TearDown failure is reported on its <test-suite> (site="SetUp"/"TearDown"), not as a
  // failed case (a teardown failure leaves every case passed): count it as a failure of its own.
  for (const element of suites) {
    const attrs = element.attributes;
    if ((attrs.result ?? '').toLowerCase() !== 'failed' || !['setup', 'teardown'].includes((attrs.site ?? '').toLowerCase())) continue;
    const failure = child(element, 'failure');
    const fullName = `${attrs.fullname ?? attrs.name ?? ''} [${attrs.site}]`;
    const message = elementText(failure, 'message');
    const stackTrace = elementText(failure, 'stack-trace');
    tests.push({
      fullName,
      name: `[${attrs.site}]`,
      className: attrs.fullname ?? attrs.name ?? '',
      outcome: 'failed',
      durationSeconds: toNumber(attrs.duration),
      suiteFailure: true,
      ...(message !== undefined ? { message } : {}),
      ...(stackTrace !== undefined ? { stackTrace } : {}),
    });
  }

  const count = (outcome: TestOutcome) => tests.filter(t => t.outcome === outcome).length;
  const suiteFailures = tests.filter(t => t.suiteFailure).length;
  // The run's own counters win; the per-case count covers a writer that leaves them out.
  const summary: TestRunSummary = {
    total: run.total !== undefined ? toNumber(run.total) : tests.length,
    passed: run.passed !== undefined ? toNumber(run.passed) : count('passed'),
    // Unity's counter counts cases only; a suite-level failure adds to it.
    failed: (run.failed !== undefined ? toNumber(run.failed) : count('failed') - suiteFailures) + suiteFailures,
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
