const invalidFileNameChars = /[<>:"/\\|?*\x00-\x1F'\[\]]/g;

export function sanitizeFileName(fileName) {
  if (fileName === null || fileName === undefined || String(fileName).trim() === '') {
    return '';
  }

  const sanitized = String(fileName).replace(invalidFileNameChars, '_').trim();
  return sanitized.length > 200 ? sanitized.slice(0, 200) : sanitized;
}

export function escapeMarkup(text) {
  if (text === null || text === undefined) {
    return '';
  }

  return String(text).replaceAll('[', '[[').replaceAll(']', ']]');
}
