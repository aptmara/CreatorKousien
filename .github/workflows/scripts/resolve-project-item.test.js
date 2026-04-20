/**
 * @file resolve-project-item.js の単体テスト
 * @author 山内陽
 */
const assert = require('node:assert/strict');

const resolveProjectItem = require('./resolve-project-item');
const { helpers } = resolveProjectItem;

/**
 * @param {Array<unknown>} values 返却値一覧
 * @returns {{ graphql(query: string, variables: Record<string, unknown>): Promise<unknown>, calls: Array<{ query: string, variables: Record<string, unknown> }> }}
 */
function createGithubMock(values) {
  const queue = [...values];
  const calls = [];

  return {
    calls,
    async graphql(query, variables) {
      calls.push({ query, variables });
      if (queue.length === 0) {
        throw new Error('Mock response is exhausted.');
      }

      return queue.shift();
    },
  };
}

/**
 * @returns {{ messages: string[], setFailed(message: string): void }}
 */
function createCoreMock() {
  const messages = [];
  return {
    messages,
    setFailed(message) {
      messages.push(message);
    },
  };
}

function testBuildProjectFieldData() {
  const result = helpers.buildProjectFieldData([
    {
      id: 'field-status',
      name: 'Status',
      options: [
        { id: 'ready', name: 'Ready' },
      ],
    },
    {
      id: 'field-start',
      name: 'Start date',
    },
  ]);

  assert.deepEqual(result, {
    fieldOptions: {
      Status: {
        Ready: 'ready',
      },
    },
    fields: {
      'Start date': 'field-start',
      Status: 'field-status',
    },
  });
}

async function testResolveProjectItemReturnsExistingVisibleItem() {
  const github = createGithubMock([
    {
      user: {
        projectV2: {
          fields: {
            nodes: [
              { id: 'field-status', name: 'Status', options: [] },
            ],
          },
          id: 'project-1',
        },
      },
    },
    {
      user: {
        projectV2: {
          items: {
            nodes: [
              { content: { number: 158 }, id: 'visible-item-1' },
            ],
            pageInfo: {
              endCursor: null,
              hasNextPage: false,
            },
          },
        },
      },
    },
  ]);
  const core = createCoreMock();

  const result = await resolveProjectItem({
    core,
    github,
    issue: {
      node_id: 'issue-node-1',
      number: 158,
    },
    itemIdHint: '',
    projectNumber: 4,
    user: 'aptmara',
  });

  assert.deepEqual(result, {
    fieldOptions: {
      Status: {},
    },
    fields: {
      Status: 'field-status',
    },
    itemId: 'visible-item-1',
    projectId: 'project-1',
  });
  assert.equal(core.messages.length, 0);
  assert.equal(github.calls.length, 2);
}

async function testResolveVisibleItemRepairsInvisibleItem() {
  const github = createGithubMock([
    {
      user: {
        projectV2: {
          items: {
            nodes: [],
            pageInfo: {
              endCursor: null,
              hasNextPage: false,
            },
          },
        },
      },
    },
    {
      node: {
        projectItems: {
          nodes: [
            {
              id: 'invisible-item-1',
              isArchived: false,
              project: {
                id: 'project-1',
              },
            },
          ],
        },
      },
    },
    {
      deleteProjectV2Item: {
        deletedItemId: 'invisible-item-1',
      },
    },
    {
      addProjectV2ItemById: {
        item: {
          id: 'new-item-1',
        },
      },
    },
    {
      user: {
        projectV2: {
          items: {
            nodes: [
              { content: { number: 158 }, id: 'new-item-1' },
            ],
            pageInfo: {
              endCursor: null,
              hasNextPage: false,
            },
          },
        },
      },
    },
  ]);
  const core = createCoreMock();

  const result = await helpers.resolveVisibleItemId({
    core,
    github,
    issue: {
      node_id: 'issue-node-1',
      number: 158,
    },
    itemIdHint: '',
    projectId: 'project-1',
    projectNumber: 4,
    user: 'aptmara',
    visibilityWaitMs: 0,
  });

  assert.equal(result, 'new-item-1');
  assert.equal(core.messages.length, 0);
  assert.equal(github.calls[2].variables.itemId, 'invisible-item-1');
}

async function testResolveVisibleItemFailsWhenStillInvisible() {
  const github = createGithubMock([
    {
      user: {
        projectV2: {
          items: {
            nodes: [],
            pageInfo: {
              endCursor: null,
              hasNextPage: false,
            },
          },
        },
      },
    },
    {
      node: {
        projectItems: {
          nodes: [],
        },
      },
    },
    {
      addProjectV2ItemById: {
        item: {
          id: 'new-item-1',
        },
      },
    },
    {
      user: {
        projectV2: {
          items: {
            nodes: [],
            pageInfo: {
              endCursor: null,
              hasNextPage: false,
            },
          },
        },
      },
    },
  ]);
  const core = createCoreMock();

  const result = await helpers.resolveVisibleItemId({
    core,
    github,
    issue: {
      node_id: 'issue-node-1',
      number: 158,
    },
    itemIdHint: '',
    projectId: 'project-1',
    projectNumber: 4,
    repairAttempts: 1,
    user: 'aptmara',
    visibilityWaitMs: 0,
  });

  assert.equal(result, null);
  assert.equal(core.messages.length, 1);
  assert.match(core.messages[0], /一覧に可視化されませんでした/);
}

/**
 * @returns {Promise<void>}
 */
async function main() {
  const tests = [
    ['buildProjectFieldData は field と option を展開する', testBuildProjectFieldData],
    ['resolveProjectItem は既に見えている item をそのまま返す', testResolveProjectItemReturnsExistingVisibleItem],
    ['resolveVisibleItemId は不可視 item を削除再登録して可視 item を返す', testResolveVisibleItemRepairsInvisibleItem],
    ['resolveVisibleItemId は再登録後も不可視なら失敗を記録する', testResolveVisibleItemFailsWhenStillInvisible],
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
