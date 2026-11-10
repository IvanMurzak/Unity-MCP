import { describe, it, expect } from 'vitest';
import * as path from 'path';
import {
  decodeXml, encodeXml, escapeAnnotationData, escapeAnnotationProperty, parseNUnitReport, sourceLocation, toGitHubAnnotations, toJUnitXml,
} from '../src/utils/nunit-report.js';

const PROJECT = path.resolve('/work/MyGame');
const STACK = `at Game.Tests.MathTests.Adds () [0x00001] in ${path.join(PROJECT, 'Assets', 'Tests', 'MathTests.cs')}:17`;

// The shape Unity's test runner writes (trimmed).
const REPORT = `<?xml version="1.0" encoding="utf-8"?>
<test-run id="2" testcasecount="3" result="Failed(Child)" total="3" passed="1" failed="1" inconclusive="0" skipped="1" asserts="0" duration="1.5">
  <test-suite type="TestSuite" name="MyGame" fullname="MyGame">
    <test-suite type="TestFixture" name="MathTests" fullname="Game.Tests.MathTests" classname="Game.Tests.MathTests">
      <test-case id="1" name="Adds" fullname="Game.Tests.MathTests.Adds" classname="Game.Tests.MathTests" result="Failed" duration="0.25">
        <failure>
          <message><![CDATA[  Expected: 4
  But was:  5 & more <tags>
]]></message>
          <stack-trace><![CDATA[${STACK}]]></stack-trace>
        </failure>
      </test-case>
      <test-case id="2" name="Subtracts" fullname="Game.Tests.MathTests.Subtracts" classname="Game.Tests.MathTests" result="Passed" duration="0.01" />
      <test-case id="3" name="Later(&quot;x&quot;)" fullname="Game.Tests.MathTests.Later(&quot;x&quot;)" classname="Game.Tests.MathTests" result="Skipped" duration="0">
        <reason><message><![CDATA[Not ready]]></message></reason>
      </test-case>
    </test-suite>
  </test-suite>
</test-run>`;

describe('parseNUnitReport', () => {
  it('reads the run counters and every test case', () => {
    const report = parseNUnitReport(REPORT)!;
    expect(report.summary).toEqual({ total: 3, passed: 1, failed: 1, skipped: 1, inconclusive: 0, durationSeconds: 1.5, result: 'Failed(Child)' });
    expect(report.tests.map(t => [t.fullName, t.outcome])).toEqual([
      ['Game.Tests.MathTests.Adds', 'failed'],
      ['Game.Tests.MathTests.Subtracts', 'passed'],
      ['Game.Tests.MathTests.Later("x")', 'skipped'],
    ]);
  });

  it('keeps CDATA messages verbatim (trimmed) and the stack trace', () => {
    const failed = parseNUnitReport(REPORT)!.tests[0];
    expect(failed.message).toBe('Expected: 4\n  But was:  5 & more <tags>');
    expect(failed.stackTrace).toBe(STACK);
    expect(failed.className).toBe('Game.Tests.MathTests');
  });

  it('reads a skip reason as the message', () => {
    expect(parseNUnitReport(REPORT)!.tests[2].message).toBe('Not ready');
  });

  it('returns undefined for text that is not a report (a truncated file)', () => {
    expect(parseNUnitReport('<?xml version="1.0"?>')).toBeUndefined();
    expect(parseNUnitReport('')).toBeUndefined();
  });

  it('counts the cases when the run element has no counters', () => {
    const report = parseNUnitReport('<test-run><test-case fullname="A.B" name="B" result="Passed"/><test-case fullname="A.C" name="C" result="Failed"/></test-run>')!;
    expect(report.summary).toMatchObject({ total: 2, passed: 1, failed: 1 });
    expect(report.tests[0].className).toBe('A');
  });
});

describe('XML escaping', () => {
  it('round-trips entities', () => {
    const text = `a & b < c > "d" 'e'`;
    expect(decodeXml(encodeXml(text))).toBe(text);
    expect(decodeXml('&#65;&#x42;')).toBe('AB');
  });

  it('drops characters XML cannot carry', () => {
    expect(encodeXml('a\u0001b')).toBe('ab');
  });
});

describe('toJUnitXml', () => {
  it('writes one suite per fixture with failure and skipped elements', () => {
    const xml = toJUnitXml(parseNUnitReport(REPORT)!, 'MyGame EditMode');
    expect(xml).toContain('<testsuites name="MyGame EditMode" tests="3" failures="1" skipped="1"');
    expect(xml).toContain('<testsuite name="Game.Tests.MathTests" tests="3" failures="1" skipped="1"');
    expect(xml).toContain('<failure message="Expected: 4&#10;'.replace('&#10;', '\n'));
    expect(xml).toContain('5 &amp; more &lt;tags&gt;');
    expect(xml).toContain('name="Later(&quot;x&quot;)"');
    expect(xml).toContain('<skipped message="Not ready"/>');
    expect(xml).toContain('<testcase classname="Game.Tests.MathTests" name="Subtracts" time="0.01"/>');
  });
});

describe('sourceLocation', () => {
  it('finds the first frame inside the project, relative with / separators', () => {
    expect(sourceLocation(STACK, PROJECT)).toEqual({ file: 'Assets/Tests/MathTests.cs', line: 17 });
  });

  it('reads Unity\'s (at Assets/...:line) frames', () => {
    expect(sourceLocation('UnityEngine.Debug:Log (object)\nFoo:Bar () (at Assets/Scripts/Foo.cs:42)', PROJECT))
      .toEqual({ file: 'Assets/Scripts/Foo.cs', line: 42 });
  });

  it('skips frames outside the project', () => {
    const outside = `at NUnit.Framework.Assert.That () [0x0] in ${path.resolve('/other/Assert.cs')}:5`;
    expect(sourceLocation(outside, PROJECT)).toBeUndefined();
    expect(sourceLocation(undefined, PROJECT)).toBeUndefined();
  });
});

describe('GitHub annotations', () => {
  it('escapes data and property values', () => {
    expect(escapeAnnotationData('50% done\nnext')).toBe('50%25 done%0Anext');
    expect(escapeAnnotationProperty('A.B(x, y): z')).toBe('A.B(x%2C y)%3A z');
  });

  it('writes one ::error per failed test, anchored at its source line', () => {
    expect(toGitHubAnnotations(parseNUnitReport(REPORT)!, PROJECT)).toEqual([
      '::error file=Assets/Tests/MathTests.cs,line=17,title=Game.Tests.MathTests.Adds::Expected: 4%0A  But was:  5 & more <tags>',
    ]);
  });
});
