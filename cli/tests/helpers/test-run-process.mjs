// A separate CLI process with a controlled editor boundary. The production
// runTests implementation and filesystem lock are real; no Unity is required.
import { EventEmitter } from 'node:events';
import fs from 'node:fs';
import { runTests } from '../../dist/lib/run-tests.js';

const [projectPath, role] = process.argv.slice(2);
const result = await runTests({
  projectPath,
  timeoutSeconds: 15,
  onStatus: message => {
    if (message.includes('Another test run')) process.send({ event: 'admission', admitted: false });
  },
  deps: {
    findEditor: async () => '/fake/Unity',
    findOpenEditor: () => undefined,
    spawn: (_command, args) => {
      const editor = Object.assign(new EventEmitter(), { pid: process.pid, kill: () => true });
      process.once('message', () => {
        if (role === 'writer') {
          const output = args[args.indexOf('-testResults') + 1];
          fs.writeFileSync(output, '<test-run result="Passed" total="1" passed="1" failed="0"><test-case fullname="Writer.Only" name="Only" result="Passed"/></test-run>');
        }
        editor.emit('exit', role === 'writer' ? 0 : 1);
      });
      process.send({ event: 'admission', admitted: true });
      return editor;
    },
  },
});
process.send({ event: 'result', result }, () => process.disconnect());
