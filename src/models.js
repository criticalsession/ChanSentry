import { URLS } from './constants.js';

export function createBoard(data = {}) {
  return {
    BoardCode: data.board ?? data.BoardCode ?? '',
    Title: data.title ?? data.Title ?? '',
    IsWorkSafe: data.ws_board ?? data.IsWorkSafe ?? 0,
    Description: data.meta_description ?? data.Description ?? ''
  };
}

export function createBoards(data = {}) {
  return {
    BoardsList: (data.boards ?? data.BoardsList ?? []).map(createBoard)
  };
}

export function createCatalogThread(data = {}) {
  return {
    ThreadId: data.no ?? data.ThreadId ?? 0,
    Subject: data.sub ?? data.Subject ?? '',
    Comment: data.com ?? data.Comment ?? '',
    ReplyCount: data.replies ?? data.ReplyCount ?? 0,
    ImageCount: data.images ?? data.ImageCount ?? 0
  };
}

export function createCatalogThreads(data = {}) {
  return {
    Page: data.page ?? data.Page ?? 0,
    ThreadList: (data.threads ?? data.ThreadList ?? []).map(createCatalogThread)
  };
}

export function createPost(data = {}) {
  return {
    FileName: data.filename ?? data.FileName ?? null,
    InternalFileIdentifier: data.tim ?? data.InternalFileIdentifier ?? null,
    FileExtension: data.ext ?? data.FileExtension ?? null,
    Timestamp: data.time ?? data.Timestamp ?? 0,
    Subject: data.sub ?? data.Subject ?? null
  };
}

export function postHasMedia(post) {
  return post.InternalFileIdentifier !== null
    && post.InternalFileIdentifier !== undefined
    && post.FileExtension !== null
    && post.FileExtension !== undefined;
}

export function getFileUrl(post, boardCode) {
  return postHasMedia(post)
    ? URLS.fileUrl(boardCode, post.InternalFileIdentifier, post.FileExtension)
    : null;
}

export function createThread(data = {}) {
  return {
    Posts: (data.posts ?? data.Posts ?? []).map(createPost)
  };
}

export function createWatchedThread(data = {}) {
  return {
    Board: data.Board ?? data.board ?? '',
    ThreadId: Number(data.ThreadId ?? data.threadId ?? 0),
    Subject: data.Subject ?? data.subject ?? '',
    ErrorCount: Number(data.ErrorCount ?? data.errorCount ?? 0),
    TotalDownloadedFiles: Number(data.TotalDownloadedFiles ?? data.totalDownloadedFiles ?? 0),
    LastChecked: data.LastChecked ?? data.lastChecked ?? '0001-01-01T00:00:00'
  };
}
