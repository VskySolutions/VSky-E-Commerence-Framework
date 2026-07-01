/*
 * Global error boot file (WO-94 Step 4).
 *
 * Installs a Vue error handler + an unhandledrejection listener so uncaught
 * errors are logged consistently (and could later be shipped to a monitoring
 * backend). Runs first in the boot chain.
 */
export default ({ app }) => {
  app.config.errorHandler = (err, instance, info) => {
    // eslint-disable-next-line no-console
    console.error('[VSky] Vue error:', info, err)
  }

  app.config.warnHandler = (msg, instance, trace) => {
    if (process.env.DEV) {
      // eslint-disable-next-line no-console
      console.warn('[VSky] Vue warn:', msg, trace)
    }
  }

  if (typeof window !== 'undefined') {
    window.addEventListener('unhandledrejection', (event) => {
      // eslint-disable-next-line no-console
      console.error('[VSky] Unhandled promise rejection:', event.reason)
    })
  }
}
