import { mkdir, rename, writeFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { DOWNLOAD_ROOT, USER_AGENT } from './constants.js';
import { getFileUrl } from './models.js';
import { sanitizeFileName } from './file-name-sanitizer.js';

const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

export class MediaDownloadService {
  constructor({ fetchImpl = globalThis.fetch, logger = console, downloadRoot = DOWNLOAD_ROOT } = {}) {
    this.fetchImpl = fetchImpl;
    this.logger = logger;
    this.downloadRoot = downloadRoot;
  }

  async downloadMediaFiles(posts, thread) {
    const downloadPath = await this.prepareDownloadDirectory(thread);

    for (const post of posts) {
      await this.downloadSingleFile(post, thread.Board, downloadPath);
    }
  }

  async prepareDownloadDirectory(thread) {
    const folderName = getDownloadFolderName(thread);
    const downloadPath = path.join(this.downloadRoot, thread.Board, folderName);
    await this.renameOldFolderIfNeeded(thread, downloadPath);
    await mkdir(downloadPath, { recursive: true });
    return downloadPath;
  }

  async renameOldFolderIfNeeded(thread, newPath) {
    const oldPath = path.join(this.downloadRoot, thread.Board, String(thread.ThreadId));

    if (!existsSync(oldPath) || oldPath === newPath) {
      return;
    }

    try {
      await rename(oldPath, newPath);
      this.logger.log('Renamed folder to include subject');
    } catch (error) {
      this.logger.error(`Failed to rename folder: ${error.message}`);
    }
  }

  async downloadSingleFile(post, boardCode, downloadPath) {
    try {
      const fileUrl = getFileUrl(post, boardCode);
      if (!fileUrl) {
        return;
      }

      const fileName = buildFileName(post);
      const filePath = path.join(downloadPath, fileName);

      if (existsSync(filePath)) {
        this.logger.log(`> File ${fileName} already exists`);
        return;
      }

      const response = await this.fetchImpl(fileUrl, {
        headers: { 'User-Agent': USER_AGENT }
      });

      if (!response.ok) {
        this.logger.error(`> Failed to download ${fileName} - Status: ${response.status}`);
        return;
      }

      const buffer = Buffer.from(await response.arrayBuffer());
      await writeFile(filePath, buffer);
      this.logger.log(`> Downloaded ${fileName}`);
      await delay(100);
    } catch (error) {
      this.logger.error(`> Error downloading file: ${error.message}`);
    }
  }
}

export function getDownloadFolderName(thread) {
  const sanitizedSubject = sanitizeFileName(thread.Subject);
  return sanitizedSubject ? `${thread.ThreadId} - ${sanitizedSubject}` : String(thread.ThreadId);
}

export function buildFileName(post) {
  const sanitizedFileName = sanitizeFileName(post.FileName);
  const prefix = sanitizedFileName ? `${sanitizedFileName} - ` : '';
  return `${prefix}${post.InternalFileIdentifier}${post.FileExtension}`;
}
