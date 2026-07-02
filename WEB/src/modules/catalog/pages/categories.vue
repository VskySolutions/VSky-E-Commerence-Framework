<template>
  <q-page class="app-page">
    <AppListHeader
      title="Categories"
      subtitle="Organise the catalog category tree."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Categories' }]"
      :show-add="canWrite"
      add-label="New root category"
      @add="onAddRoot"
    />

    <q-card flat bordered>
      <q-inner-loading :showing="loading" color="primary" />

      <q-card-section v-if="!loading && !nodes.length" class="text-grey-6 row flex-center q-py-lg">
        <q-icon name="o_inbox" size="22px" class="q-mr-sm" /> No categories yet.
      </q-card-section>

      <q-card-section v-else-if="nodes.length">
        <q-tree
          :nodes="nodes"
          node-key="id"
          label-key="name"
          children-key="children"
          default-expand-all
          no-connectors
        >
          <template #default-header="prop">
            <div class="row items-center full-width no-wrap">
              <div class="col">
                <span :class="{ 'text-strike text-grey-6': !prop.node.isEnabled }">{{ prop.node.name }}</span>
                <span class="text-grey-6 text-caption q-ml-sm">/{{ prop.node.slug || '—' }}</span>
                <q-badge v-if="!prop.node.isEnabled" color="grey" label="Disabled" class="q-ml-sm" />
              </div>
              <div v-if="canWrite" class="col-auto" @click.stop>
                <q-btn flat round dense icon="o_add" @click="onAddChild(prop.node)">
                  <q-tooltip>Add child</q-tooltip>
                </q-btn>
                <q-btn
                  flat
                  round
                  dense
                  :icon="prop.node.isEnabled ? 'o_toggle_on' : 'o_toggle_off'"
                  :color="prop.node.isEnabled ? 'positive' : 'grey'"
                  @click="toggleEnabled(prop.node)"
                >
                  <q-tooltip>{{ prop.node.isEnabled ? 'Disable' : 'Enable' }}</q-tooltip>
                </q-btn>
                <q-btn flat round dense icon="o_edit" @click="onEdit(prop.node)">
                  <q-tooltip>Edit</q-tooltip>
                </q-btn>
                <q-btn flat round dense icon="o_delete" color="negative" @click="onDelete(prop.node)">
                  <q-tooltip>Delete</q-tooltip>
                </q-btn>
              </div>
            </div>
          </template>
        </q-tree>
      </q-card-section>
    </q-card>

    <CategoryFormDrawer
      v-model="drawerOpen"
      :item="editing"
      :parent-options="parentOptions"
      :saving="saving"
      @submit="onSubmit"
      @cancel="drawerOpen = false"
    />
  </q-page>
</template>

<script setup>
/*
 * Categories page (WO-15): renders the category tree (GET /categories/tree) with
 * q-tree, and supports create / edit / enable-disable / delete per node. Ordering
 * is controlled via the displayOrder field on the form (drag-drop is a follow-up).
 */
import { ref, computed, onMounted } from 'vue'
import { getApiErrorMessage } from 'services/api'
import { categoryApi } from 'modules/catalog/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import CategoryFormDrawer from 'modules/catalog/components/CategoryFormDrawer.vue'

const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const nodes = ref([])
const loading = ref(false)

const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

function flatten (list, depth = 0, acc = []) {
  for (const node of list || []) {
    acc.push({ label: `${'— '.repeat(depth)}${node.name}`, value: node.id })
    if (node.children && node.children.length) flatten(node.children, depth + 1, acc)
  }
  return acc
}

const parentOptions = computed(() => flatten(nodes.value))

async function load () {
  loading.value = true
  try {
    const tree = await categoryApi.tree()
    nodes.value = Array.isArray(tree) ? tree : []
  } catch (err) {
    nodes.value = []
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function onAddRoot () {
  editing.value = null
  drawerOpen.value = true
}

function onAddChild (node) {
  editing.value = { parentId: node.id }
  drawerOpen.value = true
}

function onEdit (node) {
  editing.value = { ...node }
  drawerOpen.value = true
}

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value && editing.value.id) {
      await categoryApi.update(editing.value.id, payload)
      notify.success('Category updated')
    } else {
      await categoryApi.create(payload)
      notify.success('Category created')
    }
    drawerOpen.value = false
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value = false
  }
}

async function toggleEnabled (node) {
  try {
    await categoryApi.update(node.id, {
      name: node.name,
      parentId: node.parentId || null,
      slug: node.slug || null,
      description: node.description || null,
      metaTitle: node.metaTitle || null,
      metaDescription: node.metaDescription || null,
      metaKeywords: node.metaKeywords || null,
      canonicalUrl: node.canonicalUrl || null,
      displayOrder: node.displayOrder || 0,
      isEnabled: !node.isEnabled
    })
    notify.success(node.isEnabled ? 'Category disabled' : 'Category enabled')
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

async function onDelete (node) {
  if (!(await deleteConfirmation(`the category "${node.name}"`))) return
  try {
    await categoryApi.remove(node.id)
    notify.success('Category deleted')
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(load)
</script>
