import { spawnSync } from 'node:child_process';
import { writeFileSync } from 'node:fs';

const value = process.env.DATAGUARD_API_URL;
if (!value) {
  console.error('Set DATAGUARD_API_URL to your deployed ASP.NET Core backend origin before publishing.');
  process.exit(1);
}
let api;
try {
  api = new URL(value);
  if (api.protocol !== 'https:' || api.username || api.password || api.search || api.hash || api.pathname !== '/') {
    throw new Error('Expected an HTTPS origin without credentials, a path, query, or fragment.');
  }
} catch (error) {
  console.error('Invalid DATAGUARD_API_URL:', error.message);
  process.exit(1);
}

// Check the real API before creating a frontend deployment.
try {
  const response = await fetch(`${api.origin}/api/health`, { signal: AbortSignal.timeout(120000) });
  if (!response.ok || (await response.json()).status !== 'ok') {
    throw new Error(`Backend health check failed (${response.status}).`);
  }
} catch (error) {
  console.error('Cannot reach the DataGuard backend:', error.message);
  process.exit(1);
}

const build = spawnSync(process.execPath, ['node_modules/@angular/cli/bin/ng.js', 'build'], {
  stdio: 'inherit',
  env: process.env,
});
if (build.error || build.status !== 0) process.exit(build.status || 1);

// Keep browser calls same-origin. Netlify forwards these requests to C#.
writeFileSync('dist/dataguard/browser/_redirects',
  `/api/* ${api.origin}/api/:splat 200!\n/* /index.html 200\n`);
console.log('Netlify build ready, API proxy configured for', api.origin);
