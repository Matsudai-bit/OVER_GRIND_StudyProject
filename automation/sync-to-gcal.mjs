#!/usr/bin/env node
// automation/sync-to-gcal.mjs
// GitHub Projects → Google Calendar 同期スクリプト

import { google } from 'googleapis';

// ===== 設定 =====
// ここを自分の情報に書き換えてください
const PROJECT_OWNER = 'Matsudai-bit';  // GitHub ユーザー名
const PROJECT_NUMBER = 2;  // プロジェクト番号（プロジェクトURLの末尾の数字）

// ===== GitHub Projects からアイテムを取得 =====
async function fetchProjectItems() {
  const query = `
    query {
      user(login: "${PROJECT_OWNER}") {
        projectV2(number: ${PROJECT_NUMBER}) {
          items(first: 100) {
            nodes {
              id
              fieldValues(first: 20) {
                nodes {
                  ... on ProjectV2ItemFieldTextValue {
                    text
                    field { ... on ProjectV2Field { name } }
                  }
                  ... on ProjectV2ItemFieldDateValue {
                    date
                    field { ... on ProjectV2Field { name } }
                  }
                  ... on ProjectV2ItemFieldSingleSelectValue {
                    name
                    field { ... on ProjectV2SingleSelectField { name } }
                  }
                }
              }
              content {
                ... on DraftIssue {
                  title
                  body
                }
                ... on Issue {
                  title
                  body
                  number
                }
              }
            }
          }
        }
      }
    }
  `;

  const response = await fetch('https://api.github.com/graphql', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${process.env.GITHUB_TOKEN}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ query }),
  });

  const data = await response.json();
  if (data.errors) {
    console.error('GraphQL errors:', data.errors);
    throw new Error('Failed to fetch project items');
  }

  return data.data.user.projectV2.items.nodes;
}

// ===== プロジェクトアイテムをパース =====
function parseProjectItem(item) {
  const result = {
    id: item.id,
    title: item.content?.title || 'Untitled',
    body: item.content?.body || '',
    startDate: null,
    targetDate: null,
    status: null,
    priority: null,
  };

  for (const field of item.fieldValues.nodes) {
    if (!field.field) continue;
    const fieldName = field.field.name;

    if (fieldName === 'Start date' && field.date) {
      result.startDate = field.date;
    } else if (fieldName === 'Target date' && field.date) {
      result.targetDate = field.date;
    } else if (fieldName === 'Status' && field.name) {
      result.status = field.name;
    } else if (fieldName === 'Priority' && field.name) {
      result.priority = field.name;
    }
  }

  return result;
}

// ===== Google Calendar サービスを初期化 =====
async function getCalendarService() {
  const credentials = JSON.parse(process.env.GOOGLE_SERVICE_ACCOUNT_KEY);

  const auth = new google.auth.GoogleAuth({
    credentials,
    scopes: ['https://www.googleapis.com/auth/calendar'],
  });

  return google.calendar({ version: 'v3', auth });
}

// ===== Google Calendar に同期 =====
async function syncToCalendar(items) {
  const calendar = await getCalendarService();
  const calendarId = process.env.GOOGLE_CALENDAR_ID;

  // このスクリプトで作成したイベントを取得（source=github-project タグで識別）
  const existingEvents = await calendar.events.list({
    calendarId,
    privateExtendedProperty: 'source=github-project',
    maxResults: 100,
  });

  const existingByProjectId = new Map();
  for (const event of existingEvents.data.items || []) {
    const projectItemId = event.extendedProperties?.private?.projectItemId;
    if (projectItemId) {
      existingByProjectId.set(projectItemId, event);
    }
  }

  for (const item of items) {
    // 日付が設定されていないアイテムはスキップ
    if (!item.startDate && !item.targetDate) {
      console.log(`Skipping "${item.title}" - no dates set`);
      continue;
    }

    // 完了済み（Done）のアイテムはスキップ
    if (item.status === 'Done') {
      console.log(`Skipping "${item.title}" - already done`);
      continue;
    }

    const startDate = item.startDate || item.targetDate;
    const endDate = item.targetDate || item.startDate;

    // イベントの説明文を組み立て
    let description = item.body || '';
    if (item.status) description += `\n\nStatus: ${item.status}`;
    if (item.priority) description += `\nPriority: ${item.priority}`;
    description += `\n\n---\nSynced from GitHub Projects`;

    const eventData = {
      summary: item.title,
      description,
      start: { date: startDate },
      end: { date: addDays(endDate, 1) }, // Google Calendar の終了日は「その日を含まない」ので +1 日
      extendedProperties: {
        private: {
          source: 'github-project',
          projectItemId: item.id,
        },
      },
    };

    // Priority に応じて色を設定
    if (item.priority === 'Urgent') {
      eventData.colorId = '11'; // 赤
    } else if (item.priority === 'Important') {
      eventData.colorId = '5'; // 黄
    }

    const existing = existingByProjectId.get(item.id);
    if (existing) {
      // 既存のイベントを更新
      await calendar.events.update({
        calendarId,
        eventId: existing.id,
        requestBody: eventData,
      });
      console.log(`Updated: ${item.title}`);
      existingByProjectId.delete(item.id);
    } else {
      // 新しいイベントを作成
      await calendar.events.insert({
        calendarId,
        requestBody: eventData,
      });
      console.log(`Created: ${item.title}`);
    }
  }

  // GitHub側で削除されたアイテムに対応するイベントを削除
  for (const [projectItemId, event] of existingByProjectId) {
    await calendar.events.delete({
      calendarId,
      eventId: event.id,
    });
    console.log(`Deleted: ${event.summary}`);
  }
}

// ===== ユーティリティ関数 =====
function addDays(dateStr, days) {
  const date = new Date(dateStr);
  date.setDate(date.getDate() + days);
  return date.toISOString().split('T')[0];
}

// ===== メイン処理 =====
async function main() {
  console.log('Fetching GitHub Project items...');
  const rawItems = await fetchProjectItems();

  console.log(`Found ${rawItems.length} items`);
  const items = rawItems.map(parseProjectItem);

  console.log('Syncing to Google Calendar...');
  await syncToCalendar(items);

  console.log('Sync complete!');
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
