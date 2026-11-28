// Executed by a fake native editor, with the real -createProject arguments.
import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const args = process.argv.slice(2);
if (args[0] === '--worker') {
  let ticks = 0;
  const timer = setInterval(() => fs.writeFileSync(path.join(args[1], 'heartbeat'), String(++ticks)), 50);
  setTimeout(() => { clearInterval(timer); }, 30_000);
} else {
  const project = args[args.indexOf('-createProject') + 1];
  fs.writeFileSync(path.join(project, 'parent.pid'), String(process.pid));
  fs.writeFileSync(path.join(project, 'args.json'), JSON.stringify(args));
  const mode = fs.readFileSync(path.join(project, 'mode'), 'utf8');
  if (mode === 'success') {
    process.stdout.write('x'.repeat(2 * 1024 * 1024));
  } else if (mode === 'failure') {
    process.stderr.write('fixture editor diagnostic\n');
    process.exitCode = 7;
  } else {
    const worker = spawn(process.execPath, [fileURLToPath(import.meta.url), '--worker', project], {
      windowsHide: true, stdio: 'ignore',
    });
    worker.unref();
    fs.writeFileSync(path.join(project, 'worker.pid'), String(worker.pid));
    setTimeout(() => {}, 30_000);
  }
}
