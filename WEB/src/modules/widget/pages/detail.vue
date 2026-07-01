<template>
  <q-page class="app-page">
    <AppDetailHeader
      :title="widget?.name || 'Widget'"
      :subtitle="widget?.slug"
      :status="widget?.status"
      :status-color="widget?.isActive ? 'positive' : 'grey'"
      @back="router.back()"
    >
      <template #actions>
        <q-btn unelevated color="primary" icon="o_edit" label="Edit" no-caps @click="editOpen = true" />
      </template>
    </AppDetailHeader>

    <q-inner-loading :showing="loading" color="primary" />

    <q-card v-if="widget" flat bordered>
      <q-list>
        <q-item>
          <q-item-section><q-item-label caption>Name</q-item-label>{{ widget.name }}</q-item-section>
        </q-item>
        <q-separator />
        <q-item>
          <q-item-section><q-item-label caption>Slug</q-item-label>{{ widget.slug || '—' }}</q-item-section>
        </q-item>
        <q-separator />
        <q-item>
          <q-item-section><q-item-label caption>Description</q-item-label>{{ widget.description || '—' }}</q-item-section>
        </q-item>
        <q-separator />
        <q-item>
          <q-item-section>
            <q-item-label caption>Status</q-item-label>
            <div>
              <q-badge :color="widget.isActive ? 'positive' : 'grey'" :label="widget.status || '—'" />
            </div>
          </q-item-section>
        </q-item>
      </q-list>
    </q-card>

    <q-banner v-else-if="!loading" class="bg-grey-2 rounded-borders">
      Widget not found.
    </q-banner>

    <WidgetFormDrawer
      v-model="editOpen"
      :item="widget"
      :saving="saving"
      @submit="onSubmit"
      @cancel="editOpen = false"
    />
  </q-page>
</template>

<script setup>
/*
 * Widget detail page (WO-94 Step 12).
 */
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { widgetApi, getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import WidgetFormDrawer from 'modules/widget/components/WidgetFormDrawer.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()

const widget = ref(null)
const loading = ref(false)
const editOpen = ref(false)
const saving = ref(false)

async function load () {
  loading.value = true
  try {
    widget.value = await widgetApi.get(route.params.id)
  } catch (err) {
    widget.value = null
    notify.warning('Unable to load widget: ' + getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

async function onSubmit (payload) {
  saving.value = true
  try {
    await widgetApi.update(route.params.id, payload)
    notify.success('Widget updated')
    editOpen.value = false
    load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>
