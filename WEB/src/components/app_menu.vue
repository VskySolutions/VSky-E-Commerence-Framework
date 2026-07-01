<template>
  <q-list padding class="app-menu">
    <template v-for="(section, si) in visibleSections" :key="si">
      <q-item-label v-if="section.header" header class="text-uppercase text-caption text-grey-7">
        {{ section.header }}
      </q-item-label>

      <q-item
        v-for="item in section.items"
        :key="item.to"
        v-ripple
        clickable
        :to="item.to"
        exact
      >
        <q-item-section avatar>
          <q-icon :name="item.icon" />
        </q-item-section>
        <q-item-section>{{ item.label }}</q-item-section>
      </q-item>

      <q-separator v-if="si < visibleSections.length - 1" class="q-my-sm" />
    </template>
  </q-list>
</template>

<script setup>
/*
 * AppMenu (WO-94 Step 9): declarative navigation. Each item may carry a
 * `permissions` array (null = visible to everyone). Sections whose items all
 * get filtered out are dropped. Icons use the material-icons-outlined (o_) set.
 */
import { computed } from 'vue'
import { usePermissions, Permissions } from 'composables/usePermissions'

const { hasAny } = usePermissions()

const sections = [
  {
    header: null,
    items: [
      { label: 'Dashboard', icon: 'o_dashboard', to: '/dashboard', permissions: null },
      { label: 'Widgets', icon: 'o_widgets', to: '/widgets', permissions: null }
    ]
  },
  {
    header: 'Administration',
    items: [
      { label: 'Stores', icon: 'o_store', to: '/stores', permissions: [Permissions.StoresRead] },
      { label: 'Currencies', icon: 'o_payments', to: '/currencies', permissions: [Permissions.CurrenciesRead] },
      { label: 'Email Templates', icon: 'o_mail', to: '/email-templates', permissions: [Permissions.EmailTemplatesRead] },
      { label: 'Branding', icon: 'o_palette', to: '/branding', permissions: [Permissions.BrandingRead] },
      { label: 'Settings', icon: 'o_settings', to: '/settings', permissions: [Permissions.SettingsRead] },
      { label: 'Credentials', icon: 'o_key', to: '/credentials', permissions: [Permissions.CredentialsRead] }
    ]
  }
]

function isVisible (item) {
  return !item.permissions || hasAny(item.permissions)
}

const visibleSections = computed(() =>
  sections
    .map((section) => ({ ...section, items: section.items.filter(isVisible) }))
    .filter((section) => section.items.length > 0)
)
</script>
