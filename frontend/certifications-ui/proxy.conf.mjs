import { readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { homedir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectDirectory = dirname(fileURLToPath(import.meta.url));
const environmentPath = resolve(projectDirectory, '../../.env');

function readEnvironment(path) {
  const values = {};
  for (const sourceLine of readFileSync(path, 'utf8').split(/\r?\n/u)) {
    const line = sourceLine.trim();
    if (!line || line.startsWith('#')) continue;
    const separator = line.indexOf('=');
    if (separator < 1) continue;
    const name = line.slice(0, separator).trim();
    const value = line
      .slice(separator + 1)
      .trim()
      .replace(/^['"]|['"]$/gu, '');
    values[name] = value;
  }
  return values;
}

let localEnvironment;
try {
  localEnvironment = readEnvironment(environmentPath);
} catch {
  throw new Error(
    'Missing repository-root .env. Copy .env.example to .env and set SECURITY_API_KEY.',
  );
}

const apiKey = process.env['SECURITY_API_KEY'] || localEnvironment['SECURITY_API_KEY'];
if (!apiKey) {
  throw new Error('SECURITY_API_KEY must be configured in the ignored repository-root .env.');
}

function readMacOsLocalhostCertificates() {
  if (process.platform !== 'darwin') return undefined;

  try {
    const certificates = execFileSync(
      '/usr/bin/security',
      [
        'find-certificate',
        '-a',
        '-c',
        'localhost',
        '-p',
        resolve(homedir(), 'Library/Keychains/login.keychain-db'),
      ],
      { encoding: 'utf8' },
    );

    if (!certificates.includes('-----BEGIN CERTIFICATE-----')) {
      throw new Error('No localhost certificate was returned by Keychain.');
    }

    return certificates;
  } catch (error) {
    throw new Error(
      'Unable to load the ASP.NET localhost certificate from macOS Keychain. Run "dotnet dev-certs https --trust" and restart npm start.',
      { cause: error },
    );
  }
}

const localCertificateAuthority = readMacOsLocalhostCertificates();

export default {
  '/api': {
    target: 'https://localhost:7055',
    changeOrigin: true,
    secure: true,
    ...(localCertificateAuthority ? { ca: localCertificateAuthority } : {}),
    headers: {
      'X-API-Key': apiKey,
    },
  },
};
