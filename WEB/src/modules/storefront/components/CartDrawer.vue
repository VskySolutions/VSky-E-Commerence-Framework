<template>
  <q-drawer
    :model-value="modelValue"
    side="right"
    overlay
    bordered
    :width="380"
    class="sf-cart-drawer"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <div class="column full-height">
      <!-- Header -->
      <div class="row items-center justify-between q-pa-md sf-cart-drawer__head">
        <div class="row items-center q-gutter-sm">
          <q-icon name="o_shopping_cart" size="22px" color="primary" />
          <span class="text-weight-bold">My Cart ({{ itemCount }})</span>
        </div>
        <q-btn flat dense round icon="o_close" color="grey-8" @click="close" />
      </div>

      <!-- Items -->
      <q-scroll-area class="col">
        <div v-if="loading && !items.length" class="q-pa-lg text-center text-grey-6">
          <q-spinner size="28px" color="primary" />
        </div>

        <div v-else-if="!items.length" class="q-pa-xl text-center text-grey-6">
          <q-icon name="o_remove_shopping_cart" size="48px" class="q-mb-sm" />
          <div>Your cart is empty.</div>
          <q-btn class="q-mt-md" color="primary" no-caps unelevated label="Start shopping" :to="{ name: 'shop-home' }" @click="close" />
        </div>

        <q-list v-else separator>
          <q-item v-for="item in items" :key="item.id" class="q-py-md">
            <q-item-section avatar>
              <q-avatar rounded size="56px" color="grey-2" text-color="grey-6">
                <img v-if="item.imageUrl || item.primaryImageUrl" :src="item.imageUrl || item.primaryImageUrl" :alt="item.productName">
                <q-icon v-else name="o_image" />
              </q-avatar>
            </q-item-section>
            <q-item-section>
              <q-item-label class="text-weight-medium">{{ item.productName }}</q-item-label>
              <q-item-label caption>{{ item.quantity }} × {{ format(item.unitPrice) }}</q-item-label>
              <q-item-label v-if="!item.available" class="text-negative" caption>Unavailable</q-item-label>
            </q-item-section>
            <q-item-section side top>
              <div class="text-weight-bold">{{ format(item.lineTotal) }}</div>
              <q-btn flat dense round size="sm" icon="o_delete" color="grey-6" :disable="busy[item.id]" @click="remove(item)" />
            </q-item-section>
          </q-item>
        </q-list>
      </q-scroll-area>

      <!-- Footer -->
      <div v-if="items.length" class="sf-cart-drawer__footer q-pa-md">
        <div class="row items-center justify-between q-mb-md">
          <span class="text-grey-7">Subtotal</span>
          <span class="text-h6 text-weight-bold">{{ format(subtotal) }}</span>
        </div>
        <q-btn class="full-width q-mb-sm" color="primary" outline no-caps label="View cart" :to="{ name: 'shop-cart' }" @click="close" />
        <q-btn class="full-width" color="primary" no-caps unelevated label="Checkout" :to="{ name: 'shop-checkout' }" @click="close" />
      </div>
    </div>
  </q-drawer>
</template>

<script setup>
import { reactive, onMounted } from 'vue'
import { useCart } from 'modules/storefront/composables/useCart'
import { useCurrency } from 'modules/storefront/composables/useCurrency'

defineProps({ modelValue: { type: Boolean, default: false } })
const emit = defineEmits(['update:modelValue'])

const { items, itemCount, subtotal, loading, ensureLoaded, removeItem } = useCart()
const { format } = useCurrency()

const busy = reactive({})

function close () {
  emit('update:modelValue', false)
}

async function remove (item) {
  if (busy[item.id]) return
  busy[item.id] = true
  try {
    await removeItem(item.id)
  } finally {
    busy[item.id] = false
  }
}

onMounted(ensureLoaded)
</script>

<style scoped lang="scss">
.sf-cart-drawer__head {
  border-bottom: 1px solid var(--sf-border);
  background: #fff;
}
.sf-cart-drawer__footer {
  border-top: 1px solid var(--sf-border);
  background: #fff;
}
</style>
