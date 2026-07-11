export const URLS = Object.freeze({
  baseUrl: 'https://a.4cdn.org',
  baseFileUrl: 'https://i.4cdn.org',
  boardsListUrl: 'https://a.4cdn.org/boards.json',
  catalogUrl: board => `https://a.4cdn.org/${board}/catalog.json`,
  threadUrl: (board, threadId) => `https://a.4cdn.org/${board}/thread/${threadId}.json`,
  fileUrl: (board, fileIdentifier, extension) => `https://i.4cdn.org/${board}/${fileIdentifier}${extension}`
});

export const USER_AGENT = 'ChanSentry/1.0';
export const WATCHED_THREADS_FILE = 'watched-threads.json';
export const DOWNLOAD_ROOT = 'downloads';
export const MAX_ERROR_COUNT = 3;
