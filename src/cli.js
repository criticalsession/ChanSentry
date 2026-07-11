#!/usr/bin/env node
import { createInterface } from 'node:readline/promises';
import { stdin as input, stdout as output } from 'node:process';
import { fileURLToPath } from 'node:url';
import { DownloadHandler } from './download-handler.js';
import { ManageThreadsHandler, printTitle } from './manage-threads.js';

async function promptMenu(rl, title, choices) {
  console.clear();
  printTitle();
  console.log(`${title}\n`);
  choices.forEach((choice, index) => console.log(`${index + 1}. ${choice.label}`));

  while (true) {
    const answer = Number(await rl.question('\nChoose an option: '));
    if (Number.isInteger(answer) && answer >= 1 && answer <= choices.length) {
      return choices[answer - 1].value;
    }
    console.log('Invalid selection.');
  }
}

export async function main() {
  const rl = createInterface({ input, output });

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
          { label: 'List/Delete Watched Threads', value: 'list' },
          { label: 'Back', value: 'back' }
        ]);

        const handler = new ManageThreadsHandler({ io: rl });
        if (selected === 'add') {
          await handler.addThread();
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
        rl.pause();
        await new DownloadHandler().start();
        rl.resume();
        currentMenu = 'main';
      }
    }
  } finally {
    rl.close();
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  main().catch(error => {
    console.error(error);
    process.exitCode = 1;
  });
}
