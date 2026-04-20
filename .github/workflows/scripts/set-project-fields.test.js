/**
 * @file set-project-fields.js の単体テスト
 * @author 山内陽
 */
const assert = require('node:assert/strict');

const run = require('./set-project-fields');
const { helpers } = run;

/**
 * @returns {{ core: { setFailed(message: string): void, messages: string[] }, github: { graphql: (...args: unknown[]) => Promise<unknown> }, calls: Array<{ query: string, variables: Record<string, string> }> }}
 */
function createMocks() {
  const messages = [];
  const calls = [];

  return {
    core: {
      messages,
      setFailed(message) {
        messages.push(message);
      },
    },
    github: {
      async graphql(query, variables) {
        calls.push({ query, variables });
        return { projectV2Item: { id: variables.itemId } };
      },
    },
    calls,
  };
}

function testMapPriority() {
  assert.equal(helpers.mapPriority('2'), 'P2');
  assert.equal(helpers.mapPriority('p4'), 'P4');
  assert.equal(helpers.mapPriority('High'), 'HIGH');
}

function testSafeParseFailure() {
  const { core } = createMocks();

  const result = helpers.safeParse('FIELDS', '{invalid}', core);

  assert.equal(result, null);
  assert.equal(core.messages.length, 1);
  assert.match(core.messages[0], /FIELDS の JSON 解析に失敗しました/);
}

async function testRunUpdatesAllFields() {
  const { core, github, calls } = createMocks();
  const env = {
    PROJECT_ID: 'project-1',
    ITEM_ID: 'item-1',
    FIELDS: JSON.stringify({
      Status: 'field-status',
      Priority: 'field-priority',
      'Start date': 'field-start',
      'Target date': 'field-target',
    }),
    FIELD_OPTIONS: JSON.stringify({
      Status: {
        Ready: 'option-ready',
      },
      Priority: {
        P2: 'option-p2',
      },
    }),
    STATUS: 'Ready',
    PRIORITY: '2',
    START_DATE: '2026-04-20',
    TARGET_DATE: '2026-04-30',
  };

  await run({
    core,
    github,
    process: { env },
  });

  assert.equal(core.messages.length, 0);
  assert.equal(calls.length, 4);
  assert.deepEqual(
    calls.map((call) => call.variables),
    [
      {
        projectId: 'project-1',
        itemId: 'item-1',
        fieldId: 'field-status',
        optionId: 'option-ready',
      },
      {
        projectId: 'project-1',
        itemId: 'item-1',
        fieldId: 'field-priority',
        optionId: 'option-p2',
      },
      {
        projectId: 'project-1',
        itemId: 'item-1',
        fieldId: 'field-start',
        date: '2026-04-20',
      },
      {
        projectId: 'project-1',
        itemId: 'item-1',
        fieldId: 'field-target',
        date: '2026-04-30',
      },
    ],
  );
}

async function testRunFailsOnInvalidDate() {
  const { core, github, calls } = createMocks();
  const env = {
    PROJECT_ID: 'project-1',
    ITEM_ID: 'item-1',
    FIELDS: JSON.stringify({
      Priority: 'field-priority',
      'Start date': 'field-start',
    }),
    FIELD_OPTIONS: JSON.stringify({
      Priority: {
        P1: 'option-p1',
      },
    }),
    PRIORITY: 'P1',
    START_DATE: '2026/04/20',
  };

  await run({
    core,
    github,
    process: { env },
  });

  assert.equal(calls.length, 1);
  assert.equal(core.messages.length, 1);
  assert.match(core.messages[0], /Start date は YYYY-MM-DD 形式で指定してください/);
}

/**
 * @returns {Promise<void>}
 */
async function main() {
  const tests = [
    ['mapPriority は数値を Project の優先度形式へ正規化する', testMapPriority],
    ['safeParse は不正な JSON で失敗を記録する', testSafeParseFailure],
    ['run は Status と Priority と日付フィールドを更新する', testRunUpdatesAllFields],
    ['run は不正な日付形式で失敗し後続更新を止める', testRunFailsOnInvalidDate],
  ];

  for (const [name, testCase] of tests) {
    await testCase();
    console.log(`PASS ${name}`);
  }
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
