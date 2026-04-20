/**
 * @file GitHub Project item の可視性を担保しつつ item を解決する補助スクリプト
 * @author 山内陽
 */
const DEFAULT_REPAIR_ATTEMPTS = 3;
const DEFAULT_VISIBILITY_WAIT_MS = 1500;

const PROJECT_QUERY = `
  query ResolveProjectData($login: String!, $number: Int!) {
    user(login: $login) {
      projectV2(number: $number) {
        id
        fields(first: 50) {
          nodes {
            ... on ProjectV2Field { id name dataType }
            ... on ProjectV2SingleSelectField { id name dataType options { id name } }
          }
        }
      }
    }
  }
`;

const VISIBLE_ITEM_QUERY = `
  query FindVisibleProjectItem($login: String!, $number: Int!, $first: Int!, $after: String) {
    user(login: $login) {
      projectV2(number: $number) {
        items(first: $first, after: $after) {
          nodes {
            id
            content {
              ... on Issue { id number }
            }
          }
          pageInfo {
            hasNextPage
            endCursor
          }
        }
      }
    }
  }
`;

const ISSUE_PROJECT_ITEMS_QUERY = `
  query FindIssueProjectItems($issueId: ID!, $first: Int!) {
    node(id: $issueId) {
      ... on Issue {
        projectItems(first: $first) {
          nodes {
            id
            isArchived
            project {
              id
              number
              title
            }
          }
        }
      }
    }
  }
`;

const ADD_ITEM_MUTATION = `
  mutation AddProjectItem($projectId: ID!, $contentId: ID!) {
    addProjectV2ItemById(input: { projectId: $projectId, contentId: $contentId }) {
      item {
        id
      }
    }
  }
`;

const DELETE_ITEM_MUTATION = `
  mutation DeleteProjectItem($projectId: ID!, $itemId: ID!) {
    deleteProjectV2Item(input: { projectId: $projectId, itemId: $itemId }) {
      deletedItemId
    }
  }
`;

/**
 * @param {number} waitMs 待機時間
 * @returns {Promise<void>}
 */
function sleep(waitMs) {
  return new Promise((resolve) => {
    setTimeout(resolve, waitMs);
  });
}

/**
 * @param {Array<{ id: string, name: string, options?: Array<{ id: string, name: string }> }>} nodes Project field nodes
 * @returns {{ fields: Record<string, string>, fieldOptions: Record<string, Record<string, string>> }}
 */
function buildProjectFieldData(nodes) {
  const fields = {};
  const fieldOptions = {};

  for (const field of nodes || []) {
    fields[field.name] = field.id;
    if (field.options) {
      const options = {};
      for (const option of field.options) {
        options[option.name] = option.id;
      }

      fieldOptions[field.name] = options;
    }
  }

  return { fields, fieldOptions };
}

/**
 * @param {{ github: { graphql: Function }, login: string, projectNumber: number, issueNumber: number }} params 入力値
 * @returns {Promise<string | null>}
 */
async function findVisibleItemId({ github, login, projectNumber, issueNumber }) {
  let after = null;

  while (true) {
    const response = await github.graphql(VISIBLE_ITEM_QUERY, {
      after,
      first: 100,
      login,
      number: projectNumber,
    });

    const items = response.user.projectV2.items;
    const hit = items.nodes.find((node) => node.content && node.content.number === issueNumber);
    if (hit) {
      return hit.id;
    }

    if (!items.pageInfo.hasNextPage) {
      return null;
    }

    after = items.pageInfo.endCursor;
  }
}

/**
 * @param {{ github: { graphql: Function }, issueId: string, projectId: string }} params 入力値
 * @returns {Promise<string[]>}
 */
async function findInvisibleLinkedItemIds({ github, issueId, projectId }) {
  const response = await github.graphql(ISSUE_PROJECT_ITEMS_QUERY, {
    first: 20,
    issueId,
  });

  return (response.node?.projectItems?.nodes || [])
    .filter((node) => node.project && node.project.id === projectId && !node.isArchived)
    .map((node) => node.id);
}

/**
 * @param {{ github: { graphql: Function }, projectId: string, itemId: string }} params 入力値
 * @returns {Promise<void>}
 */
async function deleteItem({ github, projectId, itemId }) {
  await github.graphql(DELETE_ITEM_MUTATION, {
    itemId,
    projectId,
  });
}

/**
 * @param {{ github: { graphql: Function }, projectId: string, contentId: string }} params 入力値
 * @returns {Promise<string>}
 */
async function addItem({ github, projectId, contentId }) {
  const response = await github.graphql(ADD_ITEM_MUTATION, {
    contentId,
    projectId,
  });

  return response.addProjectV2ItemById.item.id;
}

/**
 * @param {{
 *   core: { setFailed(message: string): void },
 *   github: { graphql: Function },
 *   issue: { node_id: string, number: number },
 *   itemIdHint?: string,
 *   projectId: string,
 *   projectNumber: number,
 *   repairAttempts?: number,
 *   user: string,
 *   visibilityWaitMs?: number,
 * }} params 入力値
 * @returns {Promise<string | null>}
 */
async function resolveVisibleItemId({
  core,
  github,
  issue,
  itemIdHint,
  projectId,
  projectNumber,
  repairAttempts = DEFAULT_REPAIR_ATTEMPTS,
  user,
  visibilityWaitMs = DEFAULT_VISIBILITY_WAIT_MS,
}) {
  const hintedId = itemIdHint || null;
  const visibleId = await findVisibleItemId({
    github,
    issueNumber: issue.number,
    login: user,
    projectNumber,
  });
  if (visibleId) {
    return visibleId;
  }

  for (let attempt = 1; attempt <= repairAttempts; attempt += 1) {
    const invisibleItemIds = await findInvisibleLinkedItemIds({
      github,
      issueId: issue.node_id,
      projectId,
    });

    for (const itemId of invisibleItemIds) {
      console.log(`Delete invisible project item: ${itemId}`);
      await deleteItem({ github, itemId, projectId });
    }

    const createdItemId = await addItem({
      contentId: issue.node_id,
      github,
      projectId,
    });

    console.log(`Repair attempt ${attempt}: created item ${createdItemId}`);
    await sleep(visibilityWaitMs);

    const repairedItemId = await findVisibleItemId({
      github,
      issueNumber: issue.number,
      login: user,
      projectNumber,
    });
    if (repairedItemId) {
      return repairedItemId;
    }
  }

  core.setFailed(
    `Project item for issue #${issue.number} は作成できましたが、一覧に可視化されませんでした。GitHub Projects 側の不整合の可能性があります。`,
  );
  return hintedId;
}

/**
 * @param {{
 *   core: { setFailed(message: string): void },
 *   github: { graphql: Function },
 *   issue: { node_id: string, number: number },
 *   itemIdHint?: string,
 *   projectNumber: number,
 *   user: string,
 * }} params 入力値
 * @returns {Promise<{ projectId: string, itemId: string, fields: Record<string, string>, fieldOptions: Record<string, Record<string, string>> } | null>}
 */
async function resolveProjectItem(params) {
  const {
    core,
    github,
    issue,
    itemIdHint,
    projectNumber,
    user,
  } = params;

  const projectResponse = await github.graphql(PROJECT_QUERY, {
    login: user,
    number: projectNumber,
  });

  const project = projectResponse.user.projectV2;
  if (!project) {
    core.setFailed(`Project ${projectNumber} was not found for user ${user}.`);
    return null;
  }

  const itemId = await resolveVisibleItemId({
    core,
    github,
    issue,
    itemIdHint,
    projectId: project.id,
    projectNumber,
    user,
  });
  if (!itemId) {
    return null;
  }

  const { fields, fieldOptions } = buildProjectFieldData(project.fields.nodes);
  return {
    fieldOptions,
    fields,
    itemId,
    projectId: project.id,
  };
}

module.exports = resolveProjectItem;
module.exports.helpers = {
  ADD_ITEM_MUTATION,
  buildProjectFieldData,
  DEFAULT_REPAIR_ATTEMPTS,
  DEFAULT_VISIBILITY_WAIT_MS,
  DELETE_ITEM_MUTATION,
  findInvisibleLinkedItemIds,
  findVisibleItemId,
  ISSUE_PROJECT_ITEMS_QUERY,
  PROJECT_QUERY,
  resolveVisibleItemId,
  sleep,
  VISIBLE_ITEM_QUERY,
};
