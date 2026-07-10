/*
 * Tenant store (Pinia, setup style) — WO-94 Step 6.
 *
 * Holds the signed-in user's tenant assignments + the active tenant, tracks a
 * global "unsaved form" flag (used to guard tenant switches), and owns tenant
 * branding: it fetches GET /api/tenant/branding and applies the colour palette
 * as CSS custom properties while exposing logo + font to the UI.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { setCssVar } from 'quasar'
import { api, authApi } from 'services/api'
import { useAuthStore } from 'stores/auth'
import { STORAGE_KEYS, getItem, setItem, removeItem } from 'services/storage'
import { setActiveTimeZone } from 'src/utils/datetime'

function defaultTimeZone () {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'
  } catch (e) {
    return 'UTC'
  }
}

const DEFAULT_BRANDING = Object.freeze({
  brandName: 'VSky E-Commerce',
  primaryColor: null,
  secondaryColor: null,
  accentColor: null,
  logoUrl: null,
  fontFamily: null
})

export const useTenantStore = defineStore('tenant', () => {
  // ---- State ---------------------------------------------------------------
  const assignments = ref(getItem(STORAGE_KEYS.TENANT_ASSIGNMENTS, []) || [])
  const activeTenantId = ref(getItem(STORAGE_KEYS.ACTIVE_TENANT_ID, null))
  const hasUnsavedForm = ref(false)
  const loading = ref(false)

  const branding = ref({ ...DEFAULT_BRANDING })
  const timeZone = ref(defaultTimeZone())

  // ---- Getters -------------------------------------------------------------
  const activeTenant = computed(() =>
    assignments.value.find((a) => String(a.tenantId) === String(activeTenantId.value)) || null
  )
  const activeRole = computed(() => (activeTenant.value ? activeTenant.value.role : null))
  const hasMultipleTenants = computed(() => assignments.value.length > 1)
  const brandName = computed(() => branding.value.brandName || DEFAULT_BRANDING.brandName)
  const logoUrl = computed(() => branding.value.logoUrl || null)
  const fontFamily = computed(() => branding.value.fontFamily || null)

  // ---- Internal ------------------------------------------------------------
  function applyBranding (b) {
    if (typeof document === 'undefined' || !b) return
    const root = document.documentElement
    // Map the tenant branding palette onto Quasar's brand tokens so every color="primary" /
    // text-primary / bg-primary across the app follows the branding table, and keep the raw
    // --brand-* custom properties for any bespoke usage.
    if (b.primaryColor) {
      root.style.setProperty('--brand-primary', b.primaryColor)
      setCssVar('primary', b.primaryColor)
    }
    if (b.secondaryColor) {
      root.style.setProperty('--brand-secondary', b.secondaryColor)
      setCssVar('secondary', b.secondaryColor)
    }
    if (b.accentColor) {
      root.style.setProperty('--brand-accent', b.accentColor)
      setCssVar('accent', b.accentColor)
    }
    if (b.fontFamily) root.style.setProperty('--brand-font-family', b.fontFamily)
  }

  // ---- Actions -------------------------------------------------------------
  function setAssignments (list) {
    assignments.value = Array.isArray(list) ? list : []
    setItem(STORAGE_KEYS.TENANT_ASSIGNMENTS, assignments.value)
    // Default the active tenant when unset or no longer valid.
    if (
      !activeTenantId.value ||
      !assignments.value.some((a) => String(a.tenantId) === String(activeTenantId.value))
    ) {
      const first = assignments.value[0]
      setActiveTenant(first ? first.tenantId : null)
    }
  }

  function setActiveTenant (tenantId) {
    activeTenantId.value = tenantId ?? null
    if (tenantId != null) setItem(STORAGE_KEYS.ACTIVE_TENANT_ID, tenantId)
    else removeItem(STORAGE_KEYS.ACTIVE_TENANT_ID)
  }

  async function switchTenant (tenantId) {
    if (tenantId == null || String(tenantId) === String(activeTenantId.value)) return
    loading.value = true
    try {
      setActiveTenant(tenantId)
      // Ask the backend to re-scope the session, then refresh the token.
      const auth = useAuthStore()
      await authApi.switchTenant(tenantId)
      if (auth.refreshToken) {
        await auth.refresh().catch(() => {})
      }
      // Re-pull branding for the newly active tenant.
      await loadBranding().catch(() => {})
    } finally {
      loading.value = false
    }
  }

  async function loadBranding () {
    try {
      const { data } = await api.get('/api/tenant/branding')
      branding.value = { ...DEFAULT_BRANDING, ...(data || {}) }
    } catch (e) {
      branding.value = { ...DEFAULT_BRANDING }
    }
    applyBranding(branding.value)
    // Admin follows the tenant display timezone (whole-app standard).
    if (branding.value.displayTimeZone) {
      timeZone.value = branding.value.displayTimeZone
      setActiveTimeZone(branding.value.displayTimeZone)
    }
    return branding.value
  }

  function setUnsavedForm (value) {
    hasUnsavedForm.value = !!value
  }

  function clear () {
    assignments.value = []
    activeTenantId.value = null
    hasUnsavedForm.value = false
    loading.value = false
    branding.value = { ...DEFAULT_BRANDING }
    removeItem(STORAGE_KEYS.TENANT_ASSIGNMENTS)
    removeItem(STORAGE_KEYS.ACTIVE_TENANT_ID)
  }

  return {
    // state
    assignments,
    activeTenantId,
    hasUnsavedForm,
    loading,
    branding,
    timeZone,
    // getters
    activeTenant,
    activeRole,
    hasMultipleTenants,
    brandName,
    logoUrl,
    fontFamily,
    // actions
    setAssignments,
    setActiveTenant,
    switchTenant,
    loadBranding,
    setUnsavedForm,
    clear
  }
})
