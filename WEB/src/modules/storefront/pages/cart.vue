<template>
  <q-page class="q-pa-md storefront-container">
    <div class="text-h5 text-weight-bold q-mb-md">Your cart</div>

    <q-inner-loading :showing="loading && !cart" color="primary" />

    <!-- Empty cart -->
    <q-banner v-if="cart && !items.length" class="bg-grey-2 rounded-borders q-my-md">
      Your cart is empty.
      <template #action>
        <q-btn flat no-caps color="primary" label="Continue shopping" :to="{ name: 'shop-home' }" />
      </template>
    </q-banner>

    <template v-if="items.length">
      <!-- Availability warnings (AC-CHK-001.5/6) -->
      <q-banner
        v-if="warnings.length"
        dense
        class="bg-orange-1 text-orange-9 rounded-borders q-mb-md"
      >
        <template #avatar>
          <q-icon name="o_warning" color="orange-9" />
        </template>
        <ul class="q-my-none q-pl-md">
          <li v-for="(w, i) in warnings" :key="i">{{ w }}</li>
        </ul>
      </q-banner>

      <div class="row q-col-gutter-lg">
        <!-- Line items -->
        <div class="col-12 col-md-8">
          <q-card flat bordered>
            <q-list separator>
              <q-item v-for="item in items" :key="item.id" class="q-py-md">
                <!-- Thumbnail -->
                <q-item-section avatar>
                  <router-link
                    :to="productLink(item)"
                    class="cart-thumb flex flex-center bg-grey-2 rounded-borders"
                  >
                    <q-icon name="o_inventory_2" size="28px" color="grey-6" />
                  </router-link>
                </q-item-section>

                <!-- Name + meta -->
                <q-item-section>
                  <q-item-label>
                    <router-link :to="productLink(item)" class="text-dark cart-name">
                      {{ item.productName }}
                    </router-link>
                  </q-item-label>
                  <q-item-label v-if="item.sku" caption>SKU: {{ item.sku }}</q-item-label>
                  <q-item-label caption>{{ format(item.unitPrice) }} each</q-item-label>
                  <q-item-label v-if="!item.available">
                    <q-badge color="negative" label="Unavailable" class="q-mt-xs" />
                  </q-item-label>

                  <!-- Quantity controls -->
                  <div class="row items-center q-gutter-xs q-mt-sm">
                    <q-btn
                      round
                      dense
                      flat
                      icon="o_remove"
                      size="sm"
                      :disable="rowBusy[item.id] || item.quantity <= 1"
                      aria-label="Decrease quantity"
                      @click="changeQty(item, item.quantity - 1)"
                    />
                    <div class="cart-qty text-center">{{ item.quantity }}</div>
                    <q-btn
                      round
                      dense
                      flat
                      icon="o_add"
                      size="sm"
                      :disable="rowBusy[item.id]"
                      aria-label="Increase quantity"
                      @click="changeQty(item, item.quantity + 1)"
                    />
                    <q-spinner v-if="rowBusy[item.id]" size="18px" color="primary" class="q-ml-sm" />
                  </div>
                </q-item-section>

                <!-- Line total + remove -->
                <q-item-section side top class="text-right">
                  <q-item-label class="text-subtitle1 text-weight-bold text-dark">
                    {{ format(item.lineTotal) }}
                  </q-item-label>
                  <q-btn
                    flat
                    dense
                    no-caps
                    size="sm"
                    color="negative"
                    icon="o_delete"
                    label="Remove"
                    class="q-mt-sm"
                    :disable="rowBusy[item.id]"
                    @click="removeLine(item)"
                  />
                </q-item-section>
              </q-item>
            </q-list>
          </q-card>

          <div class="q-mt-sm">
            <q-btn flat no-caps color="primary" icon="o_arrow_back" label="Continue shopping" :to="{ name: 'shop-home' }" />
          </div>
        </div>

        <!-- Summary -->
        <div class="col-12 col-md-4">
          <q-card flat bordered>
            <q-card-section>
              <div class="text-subtitle1 text-weight-bold q-mb-sm">Order summary</div>

              <!-- Coupon -->
              <div v-if="appliedCouponCode" class="row items-center q-mb-md">
                <q-chip
                  removable
                  color="green-1"
                  text-color="green-9"
                  icon="o_local_offer"
                  :label="appliedCouponCode"
                  :disable="couponBusy"
                  @remove="onRemoveCoupon"
                />
              </div>
              <div v-else class="q-mb-md">
                <q-input
                  v-model="couponCode"
                  dense
                  outlined
                  placeholder="Coupon code"
                  :disable="couponBusy"
                  @keyup.enter="onApplyCoupon"
                >
                  <template #append>
                    <q-btn
                      flat
                      dense
                      no-caps
                      color="primary"
                      label="Apply"
                      :loading="couponBusy"
                      :disable="!couponCode.trim()"
                      @click="onApplyCoupon"
                    />
                  </template>
                </q-input>
              </div>

              <q-separator class="q-mb-md" />

              <div class="row items-center justify-between q-mb-xs">
                <span class="text-grey-8">Subtotal</span>
                <span class="text-weight-medium">{{ format(subtotal) }}</span>
              </div>
              <div class="text-caption text-grey-6 q-mb-md">
                Shipping &amp; taxes are calculated at checkout.
              </div>

              <q-btn
                unelevated
                color="primary"
                class="full-width"
                no-caps
                label="Proceed to checkout"
                icon-right="o_arrow_forward"
                :disable="!items.length"
                :to="{ name: 'shop-checkout' }"
              />
            </q-card-section>
          </q-card>
        </div>
      </div>
    </template>
  </q-page>
</template>

<script setup>
/*
 * Storefront cart page (WO-28, REQ-CHK-001). Lists the guest cart's lines with a
 * thumbnail, name, per-unit price, a quantity stepper and line total (all money
 * formatted via useCurrency so it follows the selected display currency), plus
 * remove buttons and the availability warnings raised by the backend. A coupon
 * apply/remove control and the subtotal live in the summary, which leads to the
 * one-page checkout. Resilient to empty carts and slow/failed calls.
 */
import { ref, reactive, onMounted } from 'vue'
import { useCart } from 'modules/storefront/composables/useCart'
import { useCurrency } from 'modules/storefront/composables/useCurrency'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'

const {
  cart,
  items,
  subtotal,
  warnings,
  appliedCouponCode,
  loading,
  refresh,
  updateItem,
  removeItem,
  applyCoupon,
  removeCoupon
} = useCart()
const { format, load: loadCurrencies } = useCurrency()
const notify = useNotify()

const rowBusy = reactive({})
const couponCode = ref('')
const couponBusy = ref(false)

function productLink (item) {
  return { name: 'shop-product', params: { idOrSlug: item.productId } }
}

async function changeQty (item, quantity) {
  if (quantity < 1 || rowBusy[item.id]) return
  rowBusy[item.id] = true
  try {
    await updateItem(item.id, quantity)
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    rowBusy[item.id] = false
  }
}

async function removeLine (item) {
  if (rowBusy[item.id]) return
  rowBusy[item.id] = true
  try {
    await removeItem(item.id)
    notify.info('Item removed from cart')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    rowBusy[item.id] = false
  }
}

async function onApplyCoupon () {
  const code = couponCode.value.trim()
  if (!code || couponBusy.value) return
  couponBusy.value = true
  try {
    await applyCoupon(code)
    couponCode.value = ''
    notify.success('Coupon applied')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    couponBusy.value = false
  }
}

async function onRemoveCoupon () {
  if (couponBusy.value) return
  couponBusy.value = true
  try {
    await removeCoupon()
    notify.info('Coupon removed')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    couponBusy.value = false
  }
}

onMounted(() => {
  loadCurrencies()
  refresh().catch((err) => notify.error(getApiErrorMessage(err)))
})
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1200px;
  margin: 0 auto;
}

.cart-thumb {
  width: 64px;
  height: 64px;
}

.cart-name {
  text-decoration: none;
  font-weight: 500;

  &:hover {
    text-decoration: underline;
  }
}

.cart-qty {
  min-width: 32px;
}
</style>
