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

const branding = ref(null)
const loaded = ref(false)
const loading = ref(false)

// Static fallbacks used until (or when) branding is unavailable.
const DEFAULTS = {
  brandName: 'VSky Shop',
  logoUrl: null,
  supportPhone: null,
  supportEmail: null,
  tagline: 'Quality products, delivered.',
  social: [],
  accentColor: null,
  primaryColor: null
}

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
    secondaryColor: dto.secondaryColor || null
  }
}

// Apply branding colours onto the Porto tokens (accent drives buttons/badges/hover;
// primary drives the dark header/footer). No-ops for unset colours (keeps the defaults).
function applyTheme (model) {
  const root = document.documentElement
  if (model.accentColor) {
    root.style.setProperty('--sf-accent', model.accentColor)
    root.style.setProperty('--sf-badge-sale', model.accentColor)
  }
  if (model.primaryColor) {
    root.style.setProperty('--sf-primary', model.primaryColor)
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
      loaded.value = true
      loading.value = false
    }
    return branding.value
  }

  return { branding, loaded, loading, loadBranding, applyTheme }
}
