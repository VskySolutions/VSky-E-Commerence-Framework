/*
 * Development environment configuration (QENV=dev, the default).
 *
 * Consumed by quasar.config.js at build time:
 *   - API_BASE_URL        -> exposed to the client as process.env.API_BASE_URL
 *   - BUILD_PUBLIC_PATH   -> quasar build.publicPath
 *   - PUBLISH_FOLDER      -> distDir "../publish/spa/<PUBLISH_FOLDER>"
 *   - IGNORE_PUBLIC_FOLDER-> when true the publicPath is forced back to "/"
 */
module.exports = {
  // HTTPS by default in dev: the Authorize.Net Accept.js card flow requires the storefront page to be served
  // over HTTPS, which in turn forbids calling an http:// API (mixed content) — so point at Kestrel's https
  // endpoint. Overridable via the API_BASE_URL env var (set it back to http://localhost:5144 for an all-http run).
  API_BASE_URL: process.env.API_BASE_URL || 'https://localhost:7238',
  BUILD_PUBLIC_PATH: '/',
  PUBLISH_FOLDER: 'dev',
  IGNORE_PUBLIC_FOLDER: true
}
