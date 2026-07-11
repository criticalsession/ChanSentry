const state = {
  threads: [],
  watcher: null,
  events: [],
  threadActivity: null
};

const elements = {
  watcherBadge: document.querySelector('#watcherBadge'),
  startWatcher: document.querySelector('#startWatcher'),
  stopWatcher: document.querySelector('#stopWatcher'),
  refreshThreads: document.querySelector('#refreshThreads'),
  threadSummary: document.querySelector('#threadSummary'),
  threadRows: document.querySelector('#threadRows'),
  addThreadForm: document.querySelector('#addThreadForm'),
  addBoard: document.querySelector('#addBoard'),
  addThreadId: document.querySelector('#addThreadId'),
  searchForm: document.querySelector('#searchForm'),
  searchBoard: document.querySelector('#searchBoard'),
  searchQuery: document.querySelector('#searchQuery'),
  searchResults: document.querySelector('#searchResults'),
  activitySummary: document.querySelector('#activitySummary'),
  activityLog: document.querySelector('#activityLog'),
  toast: document.querySelector('#toast')
};

async function api(path, options = {}) {
  const response = await fetch(path, {
    headers: { 'Content-Type': 'application/json', ...(options.headers ?? {}) },
    ...options
  });
  const payload = await response.json();
  if (!response.ok) {
    throw new Error(payload.error ?? 'Request failed');
  }
  return payload;
}

function setBusy(element, busy) {
  element.disabled = busy;
}

function showToast(message) {
  elements.toast.textContent = message;
  elements.toast.classList.add('visible');
  window.clearTimeout(showToast.timeoutId);
  showToast.timeoutId = window.setTimeout(() => {
    elements.toast.classList.remove('visible');
  }, 2600);
}

function renderThreads() {
  const count = state.threads.length;
  const activeThread = state.watcher?.currentThread;
  const activeActivity = state.threadActivity;
  const activeLabel = activeThread
    ? ` / ${getActivityText(activeActivity) ?? 'checking'} /${activeThread.Board}/${activeThread.ThreadId}`
    : '';
  elements.threadSummary.textContent = `${count === 1 ? '1 watched thread' : `${count} watched threads`}${activeLabel}`;

  if (count === 0) {
    elements.threadRows.innerHTML = '<tr><td colspan="7" class="empty-state">No watched threads yet.</td></tr>';
    return;
  }

  elements.threadRows.replaceChildren(...state.threads.map(thread => {
    const isActive = state.watcher?.running
      && state.watcher.currentThread
      && state.watcher.currentThread.Board.toLowerCase() === thread.board.toLowerCase()
      && state.watcher.currentThread.ThreadId === thread.threadId;
    const activityText = isActive ? getActivityText(state.threadActivity) : null;
    const row = document.createElement('tr');
    if (isActive) {
      row.classList.add('is-active-thread');
    }
    row.innerHTML = `
      <td data-label="Board"><span class="badge badge-neutral mono">/${escapeHtml(thread.board)}/</span></td>
      <td data-label="Thread" class="mono">
        <button class="thread-link mono" type="button" data-open-downloads="${escapeHtml(thread.board)}:${thread.threadId}" title="Open downloaded files">${thread.threadId}</button>
      </td>
      <td data-label="Subject">
        <div class="thread-subject-cell">
          <div class="thread-subject" title="${escapeHtml(thread.subject)}">${escapeHtml(thread.subject)}</div>
          ${isActive ? `<span class="checking-pill">${escapeHtml(activityText ?? 'Checking')}</span>` : ''}
        </div>
      </td>
      <td data-label="Files">${thread.downloaded}</td>
      <td data-label="Errors">${renderErrorBadge(thread.errors)}</td>
      <td data-label="Last checked">${escapeHtml(thread.lastCheckedDisplay)}</td>
      <td data-label="Action"><button class="button button-compact" type="button" data-delete="${escapeHtml(thread.board)}:${thread.threadId}">Delete</button></td>
    `;
    return row;
  }));
}

function renderErrorBadge(errors) {
  if (errors <= 0) {
    return '<span class="badge badge-ok">0</span>';
  }
  if (errors >= 3) {
    return `<span class="badge badge-danger">${errors}</span>`;
  }
  return `<span class="badge badge-warning">${errors}</span>`;
}

function renderWatcher() {
  const status = state.watcher ?? { running: false, stopping: false };
  elements.startWatcher.disabled = status.running;
  elements.stopWatcher.disabled = !status.running || status.stopping;

  elements.watcherBadge.className = 'badge';
  if (status.stopping) {
    elements.watcherBadge.classList.add('badge-warning');
    elements.watcherBadge.textContent = 'Stopping';
  } else if (status.running) {
    elements.watcherBadge.classList.add('badge-ok');
    elements.watcherBadge.textContent = 'Running';
  } else {
    elements.watcherBadge.classList.add('badge-neutral');
    elements.watcherBadge.textContent = 'Idle';
    state.threadActivity = null;
  }

  renderThreads();
}

function renderSearchResults(matches, board) {
  elements.searchResults.replaceChildren();
  if (matches.length === 0) {
    elements.searchResults.innerHTML = '<div class="empty-state">No unwatched matches found.</div>';
    return;
  }

  for (const match of matches) {
    const item = document.createElement('div');
    item.className = 'result-item';
    item.innerHTML = `
      <div class="result-meta">
        <strong class="result-title">${escapeHtml(match.subject)}</strong>
        <span class="mono">/${escapeHtml(board)}/${match.threadId}</span>
      </div>
      <div class="result-preview">${escapeHtml(match.preview)}</div>
      <div class="result-meta">
        <span>${match.replies} replies / ${match.images} images</span>
        <button class="button button-primary" type="button" data-add-result="${escapeHtml(board)}:${match.threadId}">Add</button>
      </div>
    `;
    elements.searchResults.append(item);
  }
}

function renderActivity() {
  elements.activitySummary.textContent = state.events.length === 0 ? 'No recent events' : `${state.events.length} recent events`;
  elements.activityLog.replaceChildren(...state.events.slice(0, 20).map(event => {
    const row = document.createElement('div');
    row.className = 'activity-row';
    row.innerHTML = `
      <span>${formatTime(event.timestamp)}</span>
      <strong>${escapeHtml(event.message ?? event.type)}</strong>
    `;
    return row;
  }));
}

async function loadThreads() {
  const payload = await api('/api/threads');
  state.threads = payload.threads;
  renderThreads();
}

async function loadWatcherStatus() {
  const payload = await api('/api/watcher/status');
  state.watcher = payload.status;
  renderWatcher();
}

async function addThread(board, threadId) {
  const payload = await api('/api/threads', {
    method: 'POST',
    body: JSON.stringify({ board, threadId: Number(threadId) })
  });
  if (payload.stoppedWatcher) {
    await loadWatcherStatus();
  }
  state.threads = [...state.threads, payload.thread];
  renderThreads();
  showToast(payload.stoppedWatcher
    ? `Stopped watcher and added /${board}/${threadId}`
    : `Added /${board}/${threadId}`);
}

async function deleteThread(board, threadId) {
  const payload = await api(`/api/threads/${encodeURIComponent(board)}/${threadId}`, { method: 'DELETE' });
  state.threads = payload.threads;
  renderThreads();
  showToast(`Deleted /${board}/${threadId}`);
}

async function openThreadDownloads(board, threadId) {
  await api(`/api/threads/${encodeURIComponent(board)}/${threadId}/open-downloads`, { method: 'POST' });
  showToast(`Opened downloads for /${board}/${threadId}`);
}

function connectEvents() {
  const source = new EventSource('/api/events');
  source.addEventListener('status', message => {
    state.watcher = JSON.parse(message.data).status;
    renderWatcher();
  });
  source.addEventListener('activity', message => {
    const event = JSON.parse(message.data);
    state.watcher = event.status;
    updateThreadActivity(event);
    state.events.unshift(event);
    renderWatcher();
    renderActivity();
    if (event.type === 'cycle-finished' || event.type === 'thread-finished') {
      loadThreads().catch(error => showToast(error.message));
    }
  });
}

elements.refreshThreads.addEventListener('click', async () => {
  setBusy(elements.refreshThreads, true);
  try {
    await loadThreads();
  } catch (error) {
    showToast(error.message);
  } finally {
    setBusy(elements.refreshThreads, false);
  }
});

function updateThreadActivity(event) {
  if (event.type === 'thread-started') {
    state.threadActivity = { type: 'checking', thread: event.thread };
    return;
  }

  if (event.type === 'download-progress') {
    state.threadActivity = {
      type: 'downloading',
      thread: event.thread,
      completed: event.download.completed,
      total: event.download.total
    };
    return;
  }

  if (event.type === 'thread-finished' || event.type === 'cycle-finished' || event.type === 'watcher-stopped') {
    state.threadActivity = null;
  }
}

function getActivityText(activity) {
  if (!activity) {
    return null;
  }

  if (activity.type === 'downloading') {
    return `Downloading ${activity.completed}/${activity.total}`;
  }

  return 'Checking';
}

elements.startWatcher.addEventListener('click', async () => {
  try {
    const payload = await api('/api/watcher/start', { method: 'POST' });
    state.watcher = payload.status;
    renderWatcher();
  } catch (error) {
    showToast(error.message);
  }
});

elements.stopWatcher.addEventListener('click', async () => {
  try {
    const payload = await api('/api/watcher/stop', { method: 'POST' });
    state.watcher = payload.status;
    renderWatcher();
  } catch (error) {
    showToast(error.message);
  }
});

elements.addThreadForm.addEventListener('submit', async event => {
  event.preventDefault();
  const submit = event.submitter;
  setBusy(submit, true);
  try {
    await addThread(elements.addBoard.value.trim(), elements.addThreadId.value.trim());
    elements.addThreadForm.reset();
  } catch (error) {
    showToast(error.message);
  } finally {
    setBusy(submit, false);
  }
});

elements.searchForm.addEventListener('submit', async event => {
  event.preventDefault();
  const submit = event.submitter;
  const board = elements.searchBoard.value.trim().toLowerCase();
  const query = elements.searchQuery.value.trim();
  setBusy(submit, true);
  elements.searchResults.innerHTML = '<div class="empty-state">Searching catalog...</div>';
  try {
    const payload = await api(`/api/catalog/search?board=${encodeURIComponent(board)}&q=${encodeURIComponent(query)}`);
    renderSearchResults(payload.matches, board);
  } catch (error) {
    elements.searchResults.replaceChildren();
    showToast(error.message);
  } finally {
    setBusy(submit, false);
  }
});

document.addEventListener('click', async event => {
  const openDownloadsValue = event.target.closest('[data-open-downloads]')?.dataset.openDownloads;
  if (openDownloadsValue) {
    const [board, threadId] = openDownloadsValue.split(':');
    try {
      await openThreadDownloads(board, Number(threadId));
    } catch (error) {
      showToast(error.message);
    }
    return;
  }

  const deleteValue = event.target.closest('[data-delete]')?.dataset.delete;
  if (deleteValue) {
    const [board, threadId] = deleteValue.split(':');
    try {
      await deleteThread(board, Number(threadId));
    } catch (error) {
      showToast(error.message);
    }
    return;
  }

  const addValue = event.target.closest('[data-add-result]')?.dataset.addResult;
  if (addValue) {
    const [board, threadId] = addValue.split(':');
    try {
      await addThread(board, Number(threadId));
      event.target.closest('.result-item')?.remove();
    } catch (error) {
      showToast(error.message);
    }
  }
});

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll('\'', '&#039;');
}

function formatTime(value) {
  if (!value) {
    return '--:--:--';
  }

  return new Date(value).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
}

await Promise.all([
  loadThreads().catch(error => showToast(error.message)),
  loadWatcherStatus().catch(error => showToast(error.message))
]);
renderActivity();
connectEvents();
