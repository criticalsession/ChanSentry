import test from 'node:test';
import assert from 'node:assert/strict';
import {
  createBoard,
  createBoards,
  createCatalogThread,
  createCatalogThreads,
  createPost,
  createThread,
  createWatchedThread,
  getFileUrl,
  postHasMedia
} from '../src/models.js';
import { deserialize } from '../src/json-helper.js';

test('board API JSON maps to internal model defaults', () => {
  assert.deepEqual(createBoard({
    board: 'g',
    title: 'Technology',
    ws_board: 1,
    meta_description: 'Discussion of technology and related topics.'
  }), {
    BoardCode: 'g',
    Title: 'Technology',
    IsWorkSafe: 1,
    Description: 'Discussion of technology and related topics.'
  });

  assert.deepEqual(createBoard({ board: 'g', title: 'Technology' }), {
    BoardCode: 'g',
    Title: 'Technology',
    IsWorkSafe: 0,
    Description: ''
  });
});

test('boards and catalog API JSON map to list models', () => {
  const boards = createBoards({
    boards: [
      { board: 'g', title: 'Technology', ws_board: 1 },
      { board: 'pol', title: 'Politically Incorrect', ws_board: 0 }
    ]
  });

  assert.equal(boards.BoardsList.length, 2);
  assert.equal(boards.BoardsList[1].BoardCode, 'pol');

  const catalogThread = createCatalogThread({
    no: 98765432,
    sub: 'Test Thread',
    com: 'This is a test comment',
    replies: 42,
    images: 10
  });

  assert.equal(catalogThread.ThreadId, 98765432);
  assert.equal(catalogThread.ImageCount, 10);

  const catalogThreads = createCatalogThreads({
    page: 0,
    threads: [{ no: 1 }, { no: 2, replies: 20 }]
  });

  assert.equal(catalogThreads.Page, 0);
  assert.equal(catalogThreads.ThreadList.length, 2);
  assert.equal(catalogThreads.ThreadList[1].ReplyCount, 20);
});

test('post media detection and file URLs match 4chan media format', () => {
  const mediaPost = createPost({
    filename: 'test_image',
    tim: 1745612650141704,
    ext: '.png',
    time: 1745612650
  });

  assert.equal(postHasMedia(mediaPost), true);
  assert.equal(getFileUrl(mediaPost, 'g'), 'https://i.4cdn.org/g/1745612650141704.png');

  assert.equal(postHasMedia(createPost({ tim: null, ext: '.jpg' })), false);
  assert.equal(postHasMedia(createPost({ tim: 123, ext: null })), false);
  assert.equal(getFileUrl(createPost({ time: 1 }), 'g'), null);
});

test('thread JSON maps posts and subjects', () => {
  const thread = createThread({
    posts: [
      { sub: 'Thread Subject', filename: 'op_image', tim: 123456, ext: '.jpg' },
      { filename: 'reply_image', tim: 789012, ext: '.png' }
    ]
  });

  assert.equal(thread.Posts.length, 2);
  assert.equal(thread.Posts[0].Subject, 'Thread Subject');
  assert.equal(thread.Posts[1].Subject, null);
});

test('watched thread keeps C# compatible PascalCase schema', () => {
  const watchedThread = createWatchedThread({
    Board: 'g',
    ThreadId: 12345,
    Subject: 'Test Thread',
    ErrorCount: 1,
    TotalDownloadedFiles: 5,
    LastChecked: '2024-01-01T00:00:00Z'
  });

  assert.deepEqual(Object.keys(watchedThread), [
    'Board',
    'ThreadId',
    'Subject',
    'ErrorCount',
    'TotalDownloadedFiles',
    'LastChecked'
  ]);
  assert.equal(watchedThread.TotalDownloadedFiles, 5);
});

test('deserialize maps known models and throws for invalid inputs', () => {
  assert.equal(deserialize('{"board":"g","title":"Technology"}', 'Board').BoardCode, 'g');
  assert.equal(deserialize('{"posts":[{"filename":"image1","tim":123,"ext":".jpg"}]}', 'Thread').Posts[0].FileName, 'image1');

  assert.throws(() => deserialize('', 'Board'), SyntaxError);
  assert.throws(() => deserialize('{ invalid json }', 'Board'), SyntaxError);
  assert.throws(() => deserialize(null, 'Board'), TypeError);
});
