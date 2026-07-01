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
  API_BASE_URL: process.env.API_BASE_URL || 'http://localhost:5144',
  BUILD_PUBLIC_PATH: '/',
  PUBLISH_FOLDER: 'dev',
  IGNORE_PUBLIC_FOLDER: true
}
