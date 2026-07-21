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
 * AppMenu (WO-94 Step 9; standardized grouping): declarative navigation grouped into
 * priority-ordered categories. Each item may carry a `permissions` array (null = everyone);
 * a section or item may carry a `roles` array to gate by role. Platform-infrastructure lives
 * in a SuperAdmin-only "Platform" section; the store-operations sections are shared by
 * SuperAdmin and the store admin (TenantAdmin). Sections whose items all filter out are dropped.
 */
import { computed } from 'vue'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useAuthStore } from 'stores/auth'

const { hasAny } = usePermissions()
const auth = useAuthStore()
const userRoles = computed(() => (Array.isArray(auth.roles) ? auth.roles : []))

const sections = [
  {
    header: null,
    items: [
      { label: 'Dashboard', icon: 'o_dashboard', to: '/dashboard', permissions: null },
    ]
  },
  {
    header: 'Store Management',
    items: [
      { label: 'Stores', icon: 'o_store', to: '/stores', permissions: [Permissions.StoresRead] },
      { label: 'Inventory', icon: 'o_warehouse', to: '/catalog/inventory', permissions: [Permissions.CatalogRead] },
      { label: 'Shipping', icon: 'o_local_shipping', to: '/shipping', permissions: [Permissions.StoresRead] },
      { label: 'Tax', icon: 'o_receipt', to: '/tax', permissions: [Permissions.SettingsRead] },
      { label: 'Orders', icon: 'o_receipt_long', to: '/orders', permissions: [Permissions.OrdersRead] },
      { label: 'Returns', icon: 'o_assignment_return', to: '/returns', permissions: [Permissions.OrdersRead] },
      { label: 'Subscriptions', icon: 'o_autorenew', to: '/subscriptions', permissions: [Permissions.OrdersRead] },
    ]
  },
  {
    header: 'Catalog',
    items: [
      { label: 'Manufacturers', icon: 'o_factory', to: '/catalog/manufacturers', permissions: [Permissions.CatalogRead] },
      { label: 'Categories', icon: 'o_account_tree', to: '/catalog/categories', permissions: [Permissions.CatalogRead] },
      { label: 'Products', icon: 'o_inventory_2', to: '/catalog/products', permissions: [Permissions.CatalogRead] },
      { label: 'Attributes', icon: 'o_tune', to: '/catalog/attributes', permissions: [Permissions.CatalogRead] },
      { label: 'Promotions', icon: 'o_local_offer', to: '/promotions', permissions: [Permissions.CatalogRead] },
      { label: 'Reviews', icon: 'o_reviews', to: '/catalog/reviews', permissions: [Permissions.CatalogRead] },
      { label: 'Loyalty Points', icon: 'o_loyalty', to: '/loyalty', permissions: [Permissions.CatalogRead] }
    ]
  },
  {
    header: 'CMS',
    items: [
      { label: 'Pages', icon: 'o_description', to: '/cms/pages', permissions: [Permissions.CmsRead] },
      { label: 'Page Groups', icon: 'o_folder', to: '/cms/page-groups', permissions: [Permissions.CmsRead] },
      { label: 'Blog', icon: 'o_article', to: '/cms/blog', permissions: [Permissions.CmsRead] },
      { label: 'Banners', icon: 'o_view_carousel', to: '/cms/banners', permissions: [Permissions.CmsRead] },
      { label: 'Home Sections', icon: 'o_dashboard_customize', to: '/cms/home-sections', permissions: [Permissions.CmsRead] },
      { label: 'Collections', icon: 'o_collections_bookmark', to: '/cms/collections', permissions: [Permissions.CmsRead] },
      { label: 'Featured', icon: 'o_star', to: '/cms/featured', permissions: [Permissions.CmsRead] },
      { label: 'Category Config', icon: 'o_category', to: '/cms/category-config', permissions: [Permissions.CmsRead] },
      { label: 'Search Content', icon: 'o_search', to: '/cms/search-content', permissions: [Permissions.CmsRead] },
      { label: 'Product Q&A', icon: 'o_forum', to: '/cms/product-qa', permissions: [Permissions.CmsRead] },
      { label: 'Newsletter', icon: 'o_email', to: '/cms/newsletter', permissions: [Permissions.CmsRead] },
      { label: 'SEO', icon: 'o_travel_explore', to: '/cms/seo', permissions: [Permissions.CmsRead] }
    ]
  },
  {
    header: 'Customer Management',
    items: [
      { label: 'Customers', icon: 'o_people', to: '/customers', permissions: [Permissions.UsersRead] },
      { label: 'Customer Groups', icon: 'o_sell', to: '/customer-groups', permissions: [Permissions.UsersRead] },
      { label: 'Tax Exemptions', icon: 'o_verified', to: '/tax-exemption-requests', permissions: [Permissions.UsersRead] }
    ]
  },
  {
    header: 'Access Management',
    items: [

      { label: 'Users', icon: 'o_group', to: '/users', permissions: [Permissions.UsersRead] },
      { label: 'Roles', icon: 'o_admin_panel_settings', to: '/roles', permissions: [Permissions.RolesRead] }
    ]
  },
  {
    header: 'Communication',
    items: [
      { label: 'Email Templates', icon: 'o_mail', to: '/email-templates', permissions: [Permissions.EmailTemplatesRead] },
      { label: 'Email Log', icon: 'o_history', to: '/email-log', permissions: [Permissions.EmailLogRead] }
    ]
  },
  {
    header: 'Reports & Logs',
    items: [
      { label: 'Analytics', icon: 'o_insights', to: '/analytics', permissions: [Permissions.DashboardRead] },
      { label: 'Reports', icon: 'o_bar_chart', to: '/operational-reports', permissions: [Permissions.DashboardRead] },
      { label: 'Logs', icon: 'o_bug_report', to: '/logs', permissions: [Permissions.LogsRead] },
      { label: 'Audit Trail', icon: 'o_fact_check', to: '/audit-trail', permissions: [Permissions.AuditTrailRead] }
    ]
  },
  {
    roles: ['SuperAdmin'],
    header: 'Store Setup',
    items: [
      { label: 'Branding', icon: 'o_palette', to: '/branding', permissions: [Permissions.BrandingRead] },
      { label: 'Integrations', icon: 'o_extension', to: '/integrations', permissions: [Permissions.CredentialsRead] },
      { label: 'Currencies', icon: 'o_payments', to: '/currencies', permissions: [Permissions.CurrenciesRead] },
      { label: 'Admin Alerts', icon: 'o_notification_important', to: '/alerts', permissions: [Permissions.AlertsRead] }
    ]
  },
  // {
  //   header: 'General',
  //   items: [
  //     { label: 'Widgets', icon: 'o_widgets', to: '/widgets', permissions: null }
  //   ]
  // }
]

function hasRole (list) {
  return !Array.isArray(list) || list.length === 0 || list.some((r) => userRoles.value.includes(r))
}

function isVisible (item) {
  return (!item.permissions || hasAny(item.permissions)) && hasRole(item.roles)
}

const visibleSections = computed(() =>
  sections
    .filter((section) => hasRole(section.roles))
    .map((section) => ({ ...section, items: section.items.filter(isVisible) }))
    .filter((section) => section.items.length > 0)
)
</script>
