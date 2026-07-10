/*
 * useStorefront (WO-109) — storefront theme + branding.
 *
 * Loads tenant branding from the PUBLIC endpoint (anonApi — the storefront is
 * anonymous and must not depend on an admin session), exposes it as reactive
 * state, and applies the palette onto the Porto `--sf-*` design tokens. Parses
 * the raw SocialLinksJson / LayoutOptionsJson strings into usable objects.
 *
 * Module-singleton: branding is fetched once and shared across the shell.
 */
import { ref } from 'vue'
import { anonApi, unwrap } from 'services/api'
import { setActiveTimeZone } from 'src/utils/datetime'

// Static fallbacks used until (or when) branding is unavailable.
const DEFAULTS = {
  brandName: 'VSky Shop',
  logoUrl: null,
  faviconUrl: null,
  supportPhone: null,
  supportEmail: null,
  tagline: 'Quality products, delivered.',
  social: [],
  accentColor: null,
  primaryColor: null,
  secondaryColor: null,
  displayTimeZone: 'UTC'
}

// Seed with DEFAULTS (not null) so the shell can render before loadBranding() resolves —
// e.g. a hard load of any storefront_layout page, including /auth/login (WO-112).
const branding = ref({ ...DEFAULTS })
const loaded = ref(false)
const loading = ref(false)

function parseJson (raw, fallback) {
  if (!raw || typeof raw !== 'string') return fallback
  try {
    const val = JSON.parse(raw)
    return val ?? fallback
  } catch (e) {
    return fallback
  }
}

// SocialLinksJson may be an object ({facebook:'…'}) or an array ([{platform,url}]).
// Normalise to [{ platform, url, icon }].
const SOCIAL_ICONS = {
  facebook: 'fab fa-facebook-f',
  twitter: 'fab fa-x-twitter',
  x: 'fab fa-x-twitter',
  instagram: 'fab fa-instagram',
  youtube: 'fab fa-youtube',
  linkedin: 'fab fa-linkedin-in',
  pinterest: 'fab fa-pinterest-p',
  tiktok: 'fab fa-tiktok'
}

function normalizeSocial (raw) {
  const parsed = parseJson(raw, null)
  if (!parsed) return []
  const list = Array.isArray(parsed)
    ? parsed.map((s) => ({ platform: (s.platform || s.name || '').toLowerCase(), url: s.url || s.href }))
    : Object.entries(parsed).map(([platform, url]) => ({ platform: platform.toLowerCase(), url }))
  return list
    .filter((s) => s.platform && s.url)
    .map((s) => ({ ...s, icon: SOCIAL_ICONS[s.platform] || 'fas fa-link' }))
}

function toModel (dto) {
  if (!dto) return { ...DEFAULTS }
  const layout = parseJson(dto.layoutOptionsJson, {})
  return {
    brandName: dto.brandName || DEFAULTS.brandName,
    logoUrl: dto.logoUrl || null,
    faviconUrl: dto.faviconUrl || null,
    supportPhone: dto.supportPhone || null,
    supportEmail: dto.supportEmail || null,
    tagline: layout.tagline || layout.storefrontTagline || DEFAULTS.tagline,
    social: normalizeSocial(dto.socialLinksJson),
    accentColor: dto.accentColor || null,
    primaryColor: dto.primaryColor || null,
    secondaryColor: dto.secondaryColor || null,
    displayTimeZone: dto.displayTimeZone || DEFAULTS.displayTimeZone
  }
}

// Apply branding colours onto the storefront tokens. The chrome (header/topbar/footer) is
// light/white by design, so brand colour comes through the accent + primary tokens: accent
// drives links/hover/badges, primary drives the solid CTA buttons. We also map Quasar's
// --q-primary/secondary/accent so storefront `color="primary"` q-btns follow branding even
// when the admin tenant store hasn't run. No-ops for unset colours (keeps the defaults).
function applyTheme (model) {
  const root = document.documentElement
  if (model.accentColor) {
    root.style.setProperty('--sf-accent', model.accentColor)
    root.style.setProperty('--sf-badge-sale', model.accentColor)
    root.style.setProperty('--q-accent', model.accentColor)
  }
  if (model.primaryColor) {
    root.style.setProperty('--sf-primary', model.primaryColor)
    root.style.setProperty('--q-primary', model.primaryColor)
  }
  if (model.secondaryColor) {
    root.style.setProperty('--q-secondary', model.secondaryColor)
  }
}

export function useStorefront () {
  async function loadBranding (force = false) {
    if (loaded.value && !force) return branding.value
    if (loading.value) return branding.value
    loading.value = true
    try {
      const dto = await anonApi.get('/api/tenant/branding').then(unwrap)
      branding.value = toModel(dto)
    } catch (e) {
      branding.value = { ...DEFAULTS }
    } finally {
      applyTheme(branding.value)
      // The storefront default zone is the tenant's; a signed-in customer's preference overrides this
      // afterwards (see the storefront layout). Guests always get the tenant default.
      setActiveTimeZone(branding.value.displayTimeZone)
      loaded.value = true
      loading.value = false
    }
    return branding.value
  }

  return { branding, loaded, loading, loadBranding, applyTheme }
}
