import { X509Certificate } from 'node:crypto';
import { chmodSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const projectDirectory = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const certificateDirectory = resolve(projectDirectory, '.certificates');
const certificatePath = resolve(certificateDirectory, 'localhost.pem');
const keyPath = resolve(certificateDirectory, 'localhost.key');

function run(command, arguments_, { quiet = false } = {}) {
  const result = spawnSync(command, arguments_, {
    encoding: 'utf8',
    stdio: quiet ? 'ignore' : 'pipe',
  });

  if (result.error?.code === 'ENOENT') {
    throw new Error(`${command} is required to prepare the local HTTPS certificate.`);
  }

  return result;
}

function extractPem(source, labelPattern) {
  return source.match(
    new RegExp(`-----BEGIN (${labelPattern})-----[\\s\\S]*?-----END \\1-----`, 'u'),
  )?.[0];
}

function exportFromMacOsPfxStore() {
  const storeDirectory = resolve(homedir(), '.aspnet/dev-certs/https');
  const candidates = readdirSync(storeDirectory)
    .filter((name) => name.startsWith('aspnetcore-localhost-') && name.endsWith('.pfx'))
    .map((name) => resolve(storeDirectory, name))
    .flatMap((path) => {
      const result = run('openssl', [
        'pkcs12',
        '-in',
        path,
        '-passin',
        'pass:',
        '-clcerts',
        '-nokeys',
      ]);
      if (result.status !== 0) return [];

      const pem = extractPem(result.stdout, 'CERTIFICATE');
      if (!pem) return [];

      const certificate = new X509Certificate(pem);
      const now = Date.now();
      if (
        certificate.checkHost('localhost') !== 'localhost' ||
        Date.parse(certificate.validFrom) > now ||
        Date.parse(certificate.validTo) <= now
      ) {
        return [];
      }

      return [{ path, pem, validTo: Date.parse(certificate.validTo) }];
    })
    .sort((left, right) => right.validTo - left.validTo);

  const selected = candidates[0];
  if (!selected) {
    throw new Error('No valid ASP.NET localhost certificate was found in the local PFX store.');
  }

  const keyResult = run('openssl', [
    'pkcs12',
    '-in',
    selected.path,
    '-passin',
    'pass:',
    '-nocerts',
    '-nodes',
  ]);
  const key = extractPem(keyResult.stdout, '(?:RSA |EC )?PRIVATE KEY');
  if (keyResult.status !== 0 || !key) {
    throw new Error('Unable to read the ASP.NET development certificate private key.');
  }

  writeFileSync(certificatePath, `${selected.pem}\n`, { mode: 0o600 });
  writeFileSync(keyPath, `${key}\n`, { mode: 0o600 });
}

function exportWithDotnet() {
  const result = run('dotnet', [
    'dev-certs',
    'https',
    '--export-path',
    certificatePath,
    '--format',
    'Pem',
    '--no-password',
  ]);

  if (result.status !== 0) {
    throw new Error(
      `Unable to export the ASP.NET HTTPS development certificate.\n${result.stderr}`,
    );
  }
}

const trustCheck = run('dotnet', ['dev-certs', 'https', '--check', '--trust'], { quiet: true });
if (trustCheck.status !== 0) {
  throw new Error(
    'No trusted ASP.NET HTTPS development certificate was found. Run "dotnet dev-certs https --trust" and retry npm start.',
  );
}

mkdirSync(certificateDirectory, { recursive: true, mode: 0o700 });
rmSync(certificatePath, { force: true });
rmSync(keyPath, { force: true });

if (process.platform === 'darwin') {
  exportFromMacOsPfxStore();
} else {
  exportWithDotnet();
}

if (process.platform !== 'win32') {
  chmodSync(certificateDirectory, 0o700);
  chmodSync(certificatePath, 0o600);
  chmodSync(keyPath, 0o600);
}

const certificate = new X509Certificate(readFileSync(certificatePath));
if (certificate.checkHost('localhost') !== 'localhost') {
  throw new Error('The exported HTTPS certificate is not valid for localhost.');
}

console.log('Prepared the trusted HTTPS certificate for https://localhost:4200.');
