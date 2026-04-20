/**
 * @file GitHub Project の single select フィールドを更新する補助スクリプト
 * @author 山内陽
 */
/**
 * @param {string} name JSON 名
 * @param {string} raw 生 JSON
 * @param {{ setFailed(message: string): void }} core GitHub Actions Core
 * @returns {Record<string, unknown> | null}
 */
function safeParse(name, raw, core) {
  try {
    return JSON.parse(raw);
  } catch (error) {
    core.setFailed(`${name} の JSON 解析に失敗しました: ${error.message}`);
    return null;
  }
}

/**
 * @param {string} value 正規化対象
 * @returns {string}
 */
function normalize(value) {
  return String(value || '').trim().toLowerCase();
}

/**
 * @param {string} rawValue 入力値
 * @returns {string}
 */
function mapPriority(rawValue) {
  const value = String(rawValue || '').trim().toUpperCase();
  if (/^P\d+$/.test(value)) {
    return value;
  }

  if (/^\d+$/.test(value)) {
    return `P${value}`;
  }

  return value;
}

/**
 * @param {string} rawValue 入力値
 * @returns {string | null}
 */
function mapDate(rawValue) {
  const value = String(rawValue || '').trim();
  const matched = value.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/);
  if (!matched) {
    return null;
  }

  const year = Number(matched[1]);
  const month = Number(matched[2]);
  const day = Number(matched[3]);
  const candidate = new Date(Date.UTC(year, month - 1, day));
  if (
    Number.isNaN(candidate.getTime()) ||
    candidate.getUTCFullYear() !== year ||
    candidate.getUTCMonth() !== month - 1 ||
    candidate.getUTCDate() !== day
  ) {
    return null;
  }

  return `${matched[1]}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

/**
 * @param {Record<string, string>} fields フィールド ID 一覧
 * @param {Record<string, Record<string, string>>} fieldOptions Option 一覧
 * @returns {{ fieldIdByName: Map<string, string>, optionIdByFieldName: Map<string, Map<string, string>> }}
 */
function buildFieldMaps(fields, fieldOptions) {
  return {
    fieldIdByName: new Map(
      Object.entries(fields).map(([name, id]) => [normalize(name), id]),
    ),
    optionIdByFieldName: new Map(
      Object.entries(fieldOptions).map(([fieldName, options]) => [
        normalize(fieldName),
        new Map(
          Object.entries(options || {}).map(([optionName, optionId]) => [
            normalize(optionName),
            optionId,
          ]),
        ),
      ]),
    ),
  };
}

async function run({ core, github, process }) {
  if (!process.env.PROJECT_ID || !process.env.ITEM_ID) {
    core.setFailed('PROJECT_ID または ITEM_ID が不足しています。');
    return;
  }

  if (!process.env.FIELDS || !process.env.FIELD_OPTIONS) {
    core.setFailed('FIELDS または FIELD_OPTIONS が不足しています。');
    return;
  }

  const fields = safeParse('FIELDS', process.env.FIELDS, core);
  const fieldOptions = safeParse('FIELD_OPTIONS', process.env.FIELD_OPTIONS, core);
  if (!fields || !fieldOptions) {
    return;
  }

  const { fieldIdByName, optionIdByFieldName } = buildFieldMaps(fields, fieldOptions);

  /**
   * @param {string} fieldName フィールド名
   * @param {string} rawValue 設定値
   * @returns {Promise<void>}
   */
  async function setSingleSelectField(fieldName, rawValue) {
    const fieldId = fieldIdByName.get(normalize(fieldName));
    if (!fieldId) {
      core.setFailed(`Project field '${fieldName}' が見つかりません。`);
      return;
    }

    const options = optionIdByFieldName.get(normalize(fieldName));
    if (!options) {
      core.setFailed(`Project field '${fieldName}' の option 定義が見つかりません。`);
      return;
    }

    const normalizedValue = normalize(rawValue);
    if (!normalizedValue || normalizedValue === '_no response_') {
      console.log(`Skip ${fieldName}: value is empty.`);
      return;
    }

    const optionId = options.get(normalizedValue);
    if (!optionId) {
      core.setFailed(
        `Project field '${fieldName}' に option '${rawValue}' が存在しません。利用可能: ${Array.from(options.keys()).join(', ')}`,
      );
      return;
    }

    const mutation = `
      mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
        updateProjectV2ItemFieldValue(
          input: {
            projectId: $projectId
            itemId: $itemId
            fieldId: $fieldId
            value: { singleSelectOptionId: $optionId }
          }
        ) {
          projectV2Item {
            id
          }
        }
      }
    `;

    await github.graphql(mutation, {
      projectId: process.env.PROJECT_ID,
      itemId: process.env.ITEM_ID,
      fieldId,
      optionId,
    });
  }

  /**
   * @param {string} fieldName フィールド名
   * @param {string} rawValue 設定値
   * @returns {Promise<void>}
   */
  async function setDateField(fieldName, rawValue) {
    const value = mapDate(rawValue);
    if (!rawValue || normalize(rawValue) === '_no response_') {
      console.log(`Skip ${fieldName}: value is empty.`);
      return;
    }

    if (!value) {
      core.setFailed(`${fieldName} は YYYY-MM-DD または YYYY-M-D 形式で指定してください。受信値: ${rawValue}`);
      return;
    }

    const fieldId = fieldIdByName.get(normalize(fieldName));
    if (!fieldId) {
      core.setFailed(`Project field '${fieldName}' が見つかりません。`);
      return;
    }

    const mutation = `
      mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $date: Date!) {
        updateProjectV2ItemFieldValue(
          input: {
            projectId: $projectId
            itemId: $itemId
            fieldId: $fieldId
            value: { date: $date }
          }
        ) {
          projectV2Item {
            id
          }
        }
      }
    `;

    await github.graphql(mutation, {
      projectId: process.env.PROJECT_ID,
      itemId: process.env.ITEM_ID,
      fieldId,
      date: value,
    });
  }

  if (process.env.STATUS) {
    await setSingleSelectField('Status', process.env.STATUS);
  }

  if (process.env.PRIORITY) {
    await setSingleSelectField('Priority', mapPriority(process.env.PRIORITY));
  }

  if (process.env.START_DATE) {
    await setDateField('Start date', process.env.START_DATE);
  }

  if (process.env.TARGET_DATE) {
    await setDateField('Target date', process.env.TARGET_DATE);
  }
}

module.exports = run;
module.exports.helpers = {
  buildFieldMaps,
  mapDate,
  mapPriority,
  normalize,
  safeParse,
};
