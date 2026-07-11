#!/usr/bin/env node
import { createInterface } from 'node:readline/promises';
import { stdin as input, stdout as output } from 'node:process';
import { fileURLToPath } from 'node:url';
import { DownloadHandler } from './download-handler.js';
import { ManageThreadsHandler, printTitle } from './manage-threads.js';

export function parseMenuChoice(answer, choicesLength) {
  const normalized = String(answer).trim();
  if (!/^\d+$/.test(normalized)) {
    return null;
  }

  const selectedIndex = Number.parseInt(normalized, 10) - 1;
  if (selectedIndex < 0 || selectedIndex >= choicesLength) {
    return null;
  }

  return selectedIndex;
}

async function promptMenu(rl, title, choices) {
  console.clear();
  printTitle();
  console.log(`${title}\n`);
  choices.forEach((choice, index) => console.log(`${index + 1}. ${choice.label}`));

  while (true) {
    const answer = await rl.question('\nChoose an option: ');
    const selectedIndex = parseMenuChoice(answer, choices.length);
    if (selectedIndex !== null) {
      return choices[selectedIndex].value;
    }
    console.log('Invalid selection.');
  }
}

function createCliInterface() {
  return createInterface({ input, output });
}

function resetTerminalInput() {
  if (input.isTTY) {
    input.setRawMode(false);
  }
  input.pause();
}

export async function main() {
  let rl = createCliInterface();

  try {
    let currentMenu = 'main';

    while (true) {
      if (currentMenu === 'main') {
        const selected = await promptMenu(rl, 'What would you like to do?', [
          { label: 'Manage Watched Threads', value: 'manage' },
          { label: 'Start Downloader', value: 'download' },
          { label: 'Exit', value: 'exit' }
        ]);

        if (selected === 'exit') {
          console.log('Goodbye!');
          return;
        }

        currentMenu = selected;
      } else if (currentMenu === 'manage') {
        const selected = await promptMenu(rl, 'What would you like to do?', [
          { label: 'Add Watched Thread', value: 'add' },
          { label: 'Search Threads', value: 'search' },
          { label: 'List/Delete Watched Threads', value: 'list' },
          { label: 'Back', value: 'back' }
        ]);

        const handler = new ManageThreadsHandler({ io: rl });
        if (selected === 'add') {
          await handler.addThread();
          await rl.question('\nPress enter to continue...');
        } else if (selected === 'search') {
          await handler.searchAndAddThreads();
          await rl.question('\nPress enter to continue...');
        } else if (selected === 'list') {
          await handler.listAndDeleteThreads();
          await rl.question('\nPress enter to continue...');
        } else {
          currentMenu = 'main';
          continue;
        }

        currentMenu = 'manage';
      } else if (currentMenu === 'download') {
        rl.close();
        await new DownloadHandler().start();
        rl = createCliInterface();
        currentMenu = 'main';
      }
    }
  } finally {
    rl.close();
    resetTerminalInput();
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  main()
    .then(() => {
      process.exit(0);
    })
    .catch(error => {
      console.error(error);
      process.exit(1);
    });
}
