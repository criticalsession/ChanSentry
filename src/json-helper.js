import {
  createBoard,
  createBoards,
  createCatalogThread,
  createCatalogThreads,
  createPost,
  createThread
} from './models.js';

const factories = Object.freeze({
  Board: createBoard,
  Boards: createBoards,
  CatalogThread: createCatalogThread,
  CatalogThreads: createCatalogThreads,
  Post: createPost,
  Thread: createThread
});

export function deserialize(json, typeName) {
  if (json === null || json === undefined) {
    throw new TypeError('Value cannot be null.');
  }

  const parsed = JSON.parse(json);
  const factory = factories[typeName];
  return factory ? factory(parsed) : parsed;
}
