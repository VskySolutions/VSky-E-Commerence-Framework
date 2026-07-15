<template>
  <q-page class="q-pa-md storefront-container">
    <div class="text-h5 text-weight-bold q-mb-md">Checkout</div>

    <q-inner-loading :showing="loading && !cart" color="primary" />

    <!-- Order confirmation (AC-CHK-003.8) -->
    <q-card v-if="orderResult && orderResult.success" flat bordered class="q-mx-auto confirm-card">
      <q-card-section class="text-center q-pa-xl">
        <q-icon name="o_check_circle" color="positive" size="72px" />
        <div class="text-h5 text-weight-bold q-mt-md">Thank you for your order!</div>
        <div class="text-body1 text-grey-8 q-mt-sm">
          Your order <span class="text-weight-bold text-dark">{{ orderResult.orderNumber }}</span> has been placed.
        </div>
        <div class="row justify-center q-gutter-xl q-mt-lg">
          <div>
            <div class="text-caption text-grey-6">Order total</div>
            <div class="text-subtitle1 text-weight-bold">{{ format(orderResult.total) }}</div>
          </div>
          <div>
            <div class="text-caption text-grey-6">Payment</div>
            <div class="text-subtitle1 text-weight-bold">{{ orderResult.paymentStatus }}</div>
          </div>
        </div>
        <div v-if="orderResult.transactionId" class="q-mt-lg">
          <div class="text-caption text-grey-6">Transaction ID</div>
          <div class="text-body2 text-weight-medium sf-txn-id">{{ orderResult.transactionId }}</div>
        </div>
        <div class="text-caption text-grey-6 q-mt-lg">
          A confirmation email with your order details is on its way.
        </div>
        <div class="row justify-center q-gutter-sm q-mt-lg">
          <q-btn
            unelevated
            color="primary"
            no-caps
            label="Continue shopping"
            :to="{ name: 'shop-home' }"
          />
          <q-btn
            v-if="isAuthed"
            outline
            color="primary"
            no-caps
            icon="o_receipt_long"
            label="My orders"
            :to="{ name: 'shop-account-orders' }"
          />
        </div>
      </q-card-section>
    </q-card>

    <!-- Verifying a redirect payment on return from the gateway -->
    <q-card v-else-if="confirming" flat bordered class="q-mx-auto confirm-card">
      <q-card-section class="text-center q-pa-xl">
        <q-spinner color="primary" size="48px" />
        <div class="text-subtitle1 text-weight-medium q-mt-md">Confirming your payment…</div>
        <div class="text-caption text-grey-6 q-mt-sm">Please wait, this only takes a moment.</div>
      </q-card-section>
    </q-card>

    <!-- Payment cancelled / not completed — offer a retry (AC-PAY-001.3) -->
    <q-card v-else-if="retryState" flat bordered class="q-mx-auto confirm-card">
      <q-card-section class="text-center q-pa-xl">
        <q-icon name="o_error_outline" color="warning" size="64px" />
        <div class="text-h6 text-weight-bold q-mt-md">Payment not completed</div>
        <div class="text-body2 text-grey-8 q-mt-sm">{{ retryState.message }}</div>
        <div class="text-caption text-grey-6 q-mt-sm">Your cart is saved — you can try again.</div>
        <div class="row justify-center q-gutter-sm q-mt-lg">
          <q-btn
            unelevated
            color="primary"
            no-caps
            icon="o_refresh"
            label="Retry payment"
            :loading="retrying"
            @click="retryPayment"
          />
          <q-btn flat color="primary" no-caps label="Change payment method" @click="retryState = null" />
        </div>
      </q-card-section>
    </q-card>

    <!-- Empty cart -->
    <q-banner
      v-else-if="cart && !items.length"
      class="bg-grey-2 rounded-borders q-my-md"
    >
      Your cart is empty — there is nothing to check out.
      <template #action>
        <q-btn flat no-caps color="primary" label="Continue shopping" :to="{ name: 'shop-home' }" />
      </template>
    </q-banner>

    <!-- Checkout form -->
    <div v-else-if="items.length" class="row q-col-gutter-lg">
      <!-- Left: address, shipping, payment -->
      <div class="col-12 col-md-7">
        <q-form ref="formRef" greedy>
          <!-- Contact + delivery address -->
          <q-card flat bordered class="q-mb-lg">
            <q-card-section>
              <div class="text-subtitle1 text-weight-bold q-mb-md">
                {{ isPickup ? 'Contact &amp; pickup store' : 'Contact &amp; delivery address' }}
              </div>

              <!-- Deliver vs collect — only offered when a store actually opts in to pickup -->
              <q-btn-toggle
                v-if="pickupOffered"
                v-model="fulfilment"
                class="q-mb-md sf-fulfilment-toggle"
                no-caps
                unelevated
                spread
                toggle-color="primary"
                color="grey-2"
                text-color="grey-8"
                :options="fulfilmentOptions"
              />

              <!-- Pickup: choose the store to collect from; the info icon carries its address -->
              <div v-if="isPickup" class="column q-gutter-sm q-mb-md">
                <q-item
                  v-for="s in pickupStores"
                  :key="s.id"
                  tag="label"
                  clickable
                  class="sf-address-option rounded-borders"
                  :class="{ 'sf-address-option--active': pickupStoreId === s.id }"
                >
                  <q-item-section avatar>
                    <q-radio v-model="pickupStoreId" :val="s.id" color="primary" />
                  </q-item-section>
                  <q-item-section>
                    <q-item-label class="text-weight-medium row items-center no-wrap">
                      {{ s.name }}
                      <q-icon name="o_info" size="18px" color="primary" class="q-ml-xs cursor-pointer">
                        <q-tooltip anchor="top middle" self="bottom middle" class="sf-store-tooltip text-body2">
                          <div class="text-weight-medium">{{ s.name }}</div>
                          <div>{{ formatAddr(s) || 'Address not available' }}</div>
                        </q-tooltip>
                      </q-icon>
                    </q-item-label>
                    <q-item-label caption>{{ formatAddr(s) }}</q-item-label>
                  </q-item-section>
                </q-item>
              </div>

              <!-- Signed-in: pick a saved address or add a new one (delivery only) -->
              <div v-if="!isPickup && isAuthed && savedAddresses.length" class="column q-gutter-sm q-mb-md">
                <q-item
                  v-for="a in savedAddresses"
                  :key="a.id"
                  tag="label"
                  clickable
                  class="sf-address-option rounded-borders"
                  :class="{ 'sf-address-option--active': selectedAddressId === a.id }"
                >
                  <q-item-section avatar top>
                    <q-radio :model-value="selectedAddressId" :val="a.id" @update:model-value="selectSaved(a)" />
                  </q-item-section>
                  <q-item-section>
                    <q-item-label class="text-weight-medium">
                      {{ a.firstName }} {{ a.lastName }}
                      <q-badge v-if="a.isDefault" color="primary" label="Default" class="q-ml-xs" />
                    </q-item-label>
                    <q-item-label caption>{{ formatAddr(a) }}</q-item-label>
                    <q-item-label v-if="a.phoneNumber" caption>{{ a.phoneNumber }}</q-item-label>
                  </q-item-section>
                  <q-item-section side top>
                    <q-btn
                      v-if="!a.isDefault"
                      flat
                      dense
                      no-caps
                      size="sm"
                      color="primary"
                      icon="o_push_pin"
                      label="Set as default"
                      :loading="settingDefaultId === a.id"
                      @click.stop.prevent="setSavedAddressDefault(a)"
                    />
                  </q-item-section>
                </q-item>

                <q-item
                  tag="label"
                  clickable
                  class="sf-address-option rounded-borders"
                  :class="{ 'sf-address-option--active': selectedAddressId === 'new' }"
                >
                  <q-item-section avatar>
                    <q-radio :model-value="selectedAddressId" val="new" @update:model-value="selectNew()" />
                  </q-item-section>
                  <q-item-section>
                    <q-item-label class="text-primary text-weight-medium">
                      <q-icon name="o_add" size="18px" class="q-mr-xs" />Use a new address
                    </q-item-label>
                  </q-item-section>
                </q-item>
              </div>

              <!-- Contact recap when a saved address is in use -->
              <div v-if="!isPickup && isAuthed && !showAddressForm" class="text-caption text-grey-7 q-mb-sm">
                Order updates go to <strong>{{ profileEmail }}</strong>
              </div>

              <!-- Manual entry: guest, no saved addresses, "new address" chosen — or any pickup order -->
              <template v-if="showContactForm">
                <div class="row q-col-gutter-x-md q-mb-sm">
                  <div class="col-12 col-sm-6">
                    <AppTextField
                      label="Email"
                      type="email"
                      required
                      :model-value="email"
                      :v="emailV"
                      placeholder="you@example.com"
                      @update:model-value="email = $event"
                    />
                  </div>
                  <div class="col-12 col-sm-6">
                    <AppPhoneInput
                      label="Phone"
                      required
                      :model-value="addr.phoneNumber"
                      :default-country="addr.countryCode || 'US'"
                      @update:model-value="addr.phoneNumber = $event"
                    />
                  </div>
                </div>
                <!-- Pickup needs only who is collecting; delivery takes the names from the address form. -->
                <div v-if="isPickup" class="row q-col-gutter-x-md">
                  <div class="col-12 col-sm-6">
                    <AppTextField
                      label="First name"
                      required
                      :model-value="addr.firstName"
                      @update:model-value="addr.firstName = $event"
                    />
                  </div>
                  <div class="col-12 col-sm-6">
                    <AppTextField
                      label="Last name"
                      required
                      :model-value="addr.lastName"
                      @update:model-value="addr.lastName = $event"
                    />
                  </div>
                </div>
                <AppAddressForm v-else v-model="addr" required :show-company="false" :show-phone="false" />
                <q-toggle
                  v-if="!isPickup && isAuthed"
                  v-model="saveNewAddress"
                  label="Save this address to my account"
                  dense
                  class="q-mt-sm"
                />
              </template>

              <div class="q-mt-md">
                <q-btn
                  unelevated
                  color="primary"
                  no-caps
                  :label="quoteLabel"
                  :icon="isPickup ? 'o_storefront' : 'o_local_shipping'"
                  :loading="quoting"
                  :disable="!canQuote || quoting"
                  @click="runQuote"
                />
              </div>
            </q-card-section>
          </q-card>
        </q-form>

        <!-- Address hint before a quote is available -->
        <q-banner v-if="!quote && !quoting && !quoteError" dense class="bg-blue-1 text-blue-9 rounded-borders q-mb-lg">
          <template #avatar><q-icon name="o_info" color="blue-9" /></template>
          <template v-if="isPickup">
            Choose a store and confirm your contact details, then <strong>Calculate total</strong> to see live totals.
          </template>
          <template v-else>
            Complete your delivery address, then <strong>Save &amp; calculate shipping</strong> to see options and live totals.
          </template>
        </q-banner>

        <q-banner v-if="quoteError" dense class="bg-red-1 text-red-9 rounded-borders q-mb-lg">
          <template #avatar><q-icon name="o_error" color="red-9" /></template>
          {{ quoteError }}
        </q-banner>

        <!-- Not routable to any store -->
        <q-banner
          v-if="quote && !isRoutable"
          dense
          class="bg-orange-1 text-orange-9 rounded-borders q-mb-lg"
        >
          <template #avatar><q-icon name="o_local_shipping" color="orange-9" /></template>
          We're unable to deliver to this address right now. Please try a different address.
        </q-banner>

        <!-- Guest ordering not permitted — offer express account creation (AC-CHK-003.2 / AC-STR-001.5) -->
        <q-card
          v-if="quote && isRoutable && !guestOrderingAllowed"
          flat
          bordered
          class="q-mb-lg bg-amber-1"
        >
          <q-card-section>
            <div class="row items-center no-wrap q-mb-sm">
              <q-icon name="o_lock" color="amber-9" size="28px" class="q-mr-md" />
              <div class="col">
                <div class="text-subtitle2 text-weight-bold">One quick step to place your order</div>
                <div class="text-body2 text-grey-8">
                  This store requires an account. We'll create one from the details above and email you a password —
                  sign in and your cart &amp; details will be right here to finish, no re-typing.
                </div>
              </div>
            </div>
            <div class="row items-center justify-end q-gutter-sm">
              <q-btn flat no-caps color="primary" label="I already have an account" :to="loginTo" />
              <q-btn
                unelevated
                color="primary"
                no-caps
                icon="o_person_add"
                label="Create account &amp; finish"
                :loading="registering"
                :disable="!canQuote || registering"
                @click="registerAndContinue"
              />
            </div>
          </q-card-section>
        </q-card>

        <!-- Shipping method -->
        <q-card v-if="canCollectDetails" flat bordered class="q-mb-lg">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold q-mb-sm">{{ isPickup ? 'Collection' : 'Shipping method' }}</div>
            <div v-if="shippingCarriers.length" class="row items-center q-gutter-xs q-mb-sm">
              <span class="text-caption text-grey-6">Rates from</span>
              <q-chip
                v-for="c in shippingCarriers"
                :key="c.carrier"
                dense
                square
                :icon="c.icon"
                :label="c.label"
                class="sf-partner-chip q-my-none"
              />
            </div>
            <q-inner-loading :showing="quoting" color="primary" />

            <q-list v-if="shippingOptions.length" separator>
              <q-item
                v-for="opt in primaryShippingOptions"
                :key="opt.methodId"
                tag="label"
                clickable
              >
                <q-item-section avatar>
                  <q-radio
                    :model-value="selectedShippingMethodId"
                    :val="opt.methodId"
                    color="primary"
                    @update:model-value="onShippingChange"
                  />
                </q-item-section>
                <q-item-section>
                  <q-item-label>
                    {{ opt.name }}
                    <q-badge v-if="opt.isRecommended" color="primary" class="q-ml-sm" label="Recommended" />
                  </q-item-label>
                  <q-item-label caption>
                    {{ opt.carrier }}
                    <span v-if="opt.estimatedDeliveryDays != null"> · {{ deliveryText(opt) }}</span>
                  </q-item-label>
                </q-item-section>
                <q-item-section side>
                  <span class="text-weight-medium text-dark">{{ format(opt.rate) }}</span>
                </q-item-section>
              </q-item>
            </q-list>

            <!-- Under automatic selection the recommendation is shown alone; the rest stay one click away
                 so the customer keeps the choice rather than being locked into the scored pick. -->
            <q-expansion-item
              v-if="otherShippingOptions.length"
              v-model="showOtherShipping"
              dense
              class="q-mt-sm"
              :label="`Other delivery options (${otherShippingOptions.length})`"
              header-class="text-primary text-body2"
            >
              <q-list separator>
                <q-item
                  v-for="opt in otherShippingOptions"
                  :key="opt.methodId"
                  tag="label"
                  clickable
                >
                  <q-item-section avatar>
                    <q-radio
                      :model-value="selectedShippingMethodId"
                      :val="opt.methodId"
                      color="primary"
                      @update:model-value="onShippingChange"
                    />
                  </q-item-section>
                  <q-item-section>
                    <q-item-label>{{ opt.name }}</q-item-label>
                    <q-item-label caption>
                      {{ opt.carrier }}
                      <span v-if="opt.estimatedDeliveryDays != null"> · {{ deliveryText(opt) }}</span>
                    </q-item-label>
                  </q-item-section>
                  <q-item-section side>
                    <span class="text-weight-medium text-dark">{{ format(opt.rate) }}</span>
                  </q-item-section>
                </q-item>
              </q-list>
            </q-expansion-item>

            <div v-else-if="shippingUnavailable" class="text-body2 text-negative row items-start no-wrap">
              <q-icon name="o_error_outline" size="20px" class="q-mr-sm q-mt-xs" />
              <span>
                No delivery options are available for this address. Try a different address<span v-if="pickupOffered">, pick up in store</span>,
                or contact us and we'll help.
              </span>
            </div>
          </q-card-section>
        </q-card>

        <!-- Payment method (AC-PAY-001.4/5) -->
        <q-card v-if="canCollectDetails" flat bordered class="q-mb-lg">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold q-mb-sm">Payment method</div>

            <div v-if="paymentMethods.length" class="column q-gutter-sm">
              <q-item
                v-for="m in paymentMethods"
                :key="m.value"
                tag="label"
                clickable
                class="sf-payment-option rounded-borders"
                :class="{ 'sf-payment-option--active': paymentMethod === m.value }"
              >
                <q-item-section avatar>
                  <q-avatar square rounded size="42px" class="sf-payment-icon">
                    <q-icon :name="m.icon" size="24px" />
                  </q-avatar>
                </q-item-section>
                <q-item-section>
                  <q-item-label class="text-weight-medium">
                    {{ m.label }}
                    <q-badge
                      v-if="m.isProduction === false"
                      color="warning"
                      text-color="dark"
                      label="Sandbox"
                      class="q-ml-xs"
                    />
                    <q-badge
                      v-else-if="m.isProduction === true"
                      color="positive"
                      label="Live"
                      class="q-ml-xs"
                    />
                    <q-badge
                      v-else
                      color="grey-5"
                      text-color="dark"
                      label="Manual"
                      class="q-ml-xs"
                    />
                  </q-item-label>
                  <q-item-label caption>{{ m.hint }}</q-item-label>
                </q-item-section>
                <q-item-section side>
                  <q-radio :model-value="paymentMethod" :val="m.value" color="primary" @update:model-value="paymentMethod = m.value" />
                </q-item-section>
              </q-item>
            </div>

            <q-banner v-else dense class="bg-red-1 text-negative rounded-borders">
              <template #avatar><q-icon name="o_error_outline" color="negative" /></template>
              No payment methods are available for this order.
            </q-banner>

            <div v-if="paymentMethods.length" class="row items-center text-caption text-grey-6 q-mt-sm no-wrap">
              <q-icon name="o_lock" size="16px" class="q-mr-xs" />
              Payments are processed securely — the storefront never stores your card details.
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Right: order summary -->
      <div class="col-12 col-md-5">
        <q-card flat bordered class="summary-card">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold q-mb-md">Order summary</div>

            <!-- Line items -->
            <div v-for="item in items" :key="item.id" class="row items-start q-mb-sm">
              <div class="col">
                <div class="text-body2">{{ item.productName }}</div>
                <div class="text-caption text-grey-6">Qty {{ item.quantity }} · {{ format(item.unitPrice) }}</div>
              </div>
              <div class="text-body2 text-weight-medium">{{ format(item.lineTotal) }}</div>
            </div>

            <q-separator class="q-my-md" />

            <div v-if="appliedCouponCode" class="row items-center q-mb-sm">
              <q-chip
                dense
                color="green-1"
                text-color="green-9"
                icon="o_local_offer"
                :label="appliedCouponCode"
              />
            </div>

            <!-- Totals -->
            <div class="row items-center justify-between q-mb-xs">
              <span class="text-grey-8">Subtotal</span>
              <span>{{ format(quote ? quote.subtotal : subtotal) }}</span>
            </div>

            <template v-if="quote">
              <div
                v-for="d in quote.discounts"
                :key="d.discountId"
                class="row items-center justify-between q-mb-xs text-green-8"
              >
                <span>{{ d.name || 'Discount' }}</span>
                <span>-{{ format(d.amount) }}</span>
              </div>
              <div
                v-if="quote.discountTotal > 0 && !quote.discounts.length"
                class="row items-center justify-between q-mb-xs text-green-8"
              >
                <span>Discount</span>
                <span>-{{ format(quote.discountTotal) }}</span>
              </div>

              <div class="row items-center justify-between q-mb-xs">
                <span class="text-grey-8">
                  Shipping<span v-if="selectedShipping"> ({{ selectedShipping.name }})</span>
                  <q-icon
                    v-if="shippingIntegrationName"
                    name="o_info"
                    size="15px"
                    class="q-ml-xs text-grey-6 cursor-pointer"
                    style="vertical-align: middle"
                  >
                    <q-tooltip anchor="top middle" self="bottom middle" class="text-body2">
                      Shipping calculation is processed by {{ shippingIntegrationName }}
                    </q-tooltip>
                  </q-icon>
                </span>
                <span>{{ format(quote.shippingTotal) }}</span>
              </div>

              <div class="row items-center justify-between q-mb-xs">
                <span class="text-grey-8">
                  Tax
                  <q-icon
                    v-if="taxProviderInfo"
                    name="o_info"
                    size="15px"
                    class="q-ml-xs text-grey-6 cursor-pointer"
                    style="vertical-align: middle"
                  >
                    <q-tooltip anchor="top middle" self="bottom middle" class="text-body2">
                      Tax calculation is processed by {{ taxProviderInfo.label }}
                    </q-tooltip>
                  </q-icon>
                </span>
                <span>{{ format(quote.taxTotal) }}</span>
              </div>
              <div v-if="quote.tax && quote.tax.fallbackApplied" class="text-caption text-orange-8 q-mb-xs">
                Estimated tax — {{ taxProviderInfo ? `${taxProviderInfo.label} was unavailable, so a` : 'a' }} fallback rate was applied.
              </div>

              <q-separator class="q-my-sm" />

              <div class="row items-center justify-between text-subtitle1 text-weight-bold">
                <span>Total</span>
                <span>{{ format(quote.total) }}</span>
              </div>
            </template>

            <div v-else class="text-caption text-grey-6 q-mt-sm">
              Shipping and taxes are calculated once your address is complete.
            </div>

            <!-- Place order / retry -->
            <q-banner v-if="placeError" dense class="bg-red-1 text-red-9 rounded-borders q-mt-md">
              <template #avatar><q-icon name="o_error" color="red-9" /></template>
              {{ placeError }}
            </q-banner>

            <q-btn
              v-if="!(quote && isRoutable && !guestOrderingAllowed)"
              unelevated
              color="primary"
              class="full-width q-mt-md"
              no-caps
              :label="placeError ? 'Retry payment' : 'Place order'"
              icon-right="o_lock"
              :loading="placing"
              :disable="!canPlace"
              @click="placeOrder"
            />
            <q-btn
              v-else
              unelevated
              color="primary"
              class="full-width q-mt-md"
              no-caps
              label="Sign in to continue"
              :to="loginTo"
            />

            <q-btn
              flat
              no-caps
              color="primary"
              class="full-width q-mt-sm"
              icon="o_arrow_back"
              label="Back to cart"
              :to="{ name: 'shop-cart' }"
            />
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup>
/*
 * Storefront one-page checkout (WO-30, REQ-CHK-003). A single view that collects
 * the contact + delivery address and, as soon as it is complete, calls
 * checkout/quote to price the cart — routing result, shipping options, discounts,
 * tax and live totals (AC-CHK-003.7). Choosing a shipping method re-quotes so the
 * totals stay real-time. A payment-method selector plus a live order summary lead
 * to checkout/place, which shows an order-confirmation panel on success
 * (AC-CHK-003.8) or a retryable error on a declined payment (AC-PAY-001.3). When
 * the quote reports guest ordering is not allowed, the buyer is prompted to sign
 * in instead (AC-CHK-003.2 / AC-STR-001.5).
 */
import { ref, computed, watch, onMounted } from 'vue'
import { LocalStorage } from 'quasar'
import { useRouter, useRoute } from 'vue-router'
import AppAddressForm from 'components/common/AppAddressForm.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppPhoneInput from 'components/common/AppPhoneInput.vue'
import { isPossiblePhoneNumber } from 'libphonenumber-js'
import { emptyAddress } from 'composables/useAddress'
import { checkoutApi } from 'modules/storefront/api'
import { customerAuthApi, accountApi } from 'modules/storefront/account-api'
import { useCustomerAuthStore } from 'stores/customerAuth'
import { useCart } from 'modules/storefront/composables/useCart'
import { useCurrency } from 'modules/storefront/composables/useCurrency'
import { useRecaptcha } from 'modules/storefront/composables/useRecaptcha'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'

const { sessionId, cart, items, subtotal, appliedCouponCode, loading, refresh } = useCart()
const { format, load: loadCurrencies } = useCurrency()
const { getToken: getRecaptchaToken } = useRecaptcha()
const notify = useNotify()
const router = useRouter()
const route = useRoute()
const customerAuth = useCustomerAuthStore()

// ---- Address form (standard AppAddressForm; email kept separate as the order contact) ----------
const formRef = ref(null)
const email = ref('')
const addr = ref(emptyAddress())

// ---- Fulfilment: deliver, or collect from a store (AC-SHP-004.1) --------------------------------
// Pickup skips routing and carrier rates entirely: the server fulfils at the chosen store and prices a
// single zero-cost "Pickup in store" option, so there is no delivery address to collect — only who is
// collecting. The choice is only offered when at least one store actually opts in to pickup.
const pickupStores = ref([])
const fulfilment = ref('deliver')
const pickupStoreId = ref(null)
const isPickup = computed(() => fulfilment.value === 'pickup')
const pickupOffered = computed(() => pickupStores.value.length > 0)
const fulfilmentOptions = [
  { label: 'Deliver to me', value: 'deliver', icon: 'o_local_shipping' },
  { label: 'Pick up in store', value: 'pickup', icon: 'o_storefront' }
]

async function loadPickupStores () {
  try {
    const stores = await checkoutApi.pickupStores()
    pickupStores.value = Array.isArray(stores) ? stores : []
    // Pre-select the only store rather than making the buyer click the one choice available.
    if (pickupStores.value.length === 1) pickupStoreId.value = pickupStores.value[0].id
  } catch (e) {
    // No pickup stores reachable → the option simply isn't offered; delivery is unaffected.
    pickupStores.value = []
  }
}

// ---- Signed-in address book (Amazon-style: pick a saved address or add a new one) ---------------
const isAuthed = computed(() => customerAuth.isAuthenticated)
const savedAddresses = ref([])
const selectedAddressId = ref('new') // a saved address id, or 'new' for manual entry
const profile = ref(null)
const profileEmail = ref('')
const saveNewAddress = ref(true)
const settingDefaultId = ref(null) // id of the saved address whose "Set as default" is in flight

// Show the manual form for guests, signed-in users with no saved address, or when "new" is chosen.
const showAddressForm = computed(() =>
  !isAuthed.value || !savedAddresses.value.length || selectedAddressId.value === 'new'
)
// Contact is always collected inline under pickup: there is no saved "pickup address" to select from, and
// the store still needs to know who is collecting.
const showContactForm = computed(() => isPickup.value || showAddressForm.value)

function fillFromAddress (a) {
  addr.value = {
    ...emptyAddress(),
    firstName: a.firstName || '', lastName: a.lastName || '', company: a.company || '',
    addressLine1: a.addressLine1 || '', addressLine2: a.addressLine2 || '', landmark: a.landmark || '',
    city: a.city || '', stateProvince: a.stateProvince || '', postalCode: a.postalCode || '',
    countryCode: a.countryCode || '', phoneNumber: a.phoneNumber || ''
  }
}
function selectSaved (a) {
  selectedAddressId.value = a.id
  fillFromAddress(a)
}
function selectNew () {
  selectedAddressId.value = 'new'
  addr.value = {
    ...emptyAddress(),
    firstName: profile.value?.firstName || '',
    lastName: profile.value?.lastName || '',
    phoneNumber: profile.value?.phoneNumber || ''
  }
}
function formatAddr (a) {
  return [a.addressLine1, a.addressLine2, a.landmark, a.city, a.stateProvince, a.postalCode, a.countryCode]
    .filter(Boolean).join(', ')
}

// Load the signed-in buyer's profile (contact email) + saved shipping addresses, and pre-select the
// default so checkout is one tap away.
async function loadAccountAddresses () {
  try {
    const [prof, book] = await Promise.all([
      accountApi.getProfile().catch(() => null),
      accountApi.addressBook().catch(() => null)
    ])
    profile.value = prof
    if (prof && prof.email) { email.value = prof.email; profileEmail.value = prof.email }
    const shipping = Array.isArray(book && book.shipping) ? book.shipping : []
    savedAddresses.value = shipping
    if (shipping.length) selectSaved(shipping.find((a) => a.isDefault) || shipping[0])
    else selectNew()
  } catch (e) {
    selectedAddressId.value = 'new'
  }
}

// Persist the just-entered delivery address to the signed-in buyer's address book. Returns the created
// address DTO (with its new id) so the caller can select it.
function saveCurrentAddress () {
  const a = addr.value
  return accountApi.createAddress({
    addressType: 'Shipping',
    isDefault: savedAddresses.value.length === 0,
    firstName: (a.firstName || '').trim(),
    lastName: (a.lastName || '').trim(),
    company: null,
    addressLine1: (a.addressLine1 || '').trim(),
    addressLine2: (a.addressLine2 || '').trim() || null,
    landmark: (a.landmark || '').trim() || null,
    city: (a.city || '').trim(),
    stateProvince: (a.stateProvince || '').trim() || null,
    postalCode: (a.postalCode || '').trim(),
    countryCode: (a.countryCode || '').toUpperCase().trim(),
    phoneNumber: (a.phoneNumber || '').trim() || null
  })
}

// Persist the current NEW address to the signed-in buyer's book when they calculate shipping (this is what
// the "Save & calculate shipping" action now does). Best-effort and guarded so recalculating never creates
// duplicates; a no-op for guests, when the "save" toggle is off, or when a saved address is already chosen.
async function maybeSaveNewAddress () {
  // Pickup collects no address — without this it would post the empty form to the buyer's address book.
  if (isPickup.value) return
  if (!isAuthed.value || !saveNewAddress.value || selectedAddressId.value !== 'new') return
  try {
    const created = await saveCurrentAddress()
    if (created && created.id) {
      // Insert + select the saved row WITHOUT touching `addr`, so the just-priced quote stays valid.
      // The optimistic insert survives a failed refresh; the refresh then replaces it with the
      // authoritative DTO from the address book.
      savedAddresses.value = [...savedAddresses.value, created]
      selectedAddressId.value = created.id
      await refreshSavedAddresses()
      notify.success('Address saved to your account')
    }
  } catch (e) { /* non-fatal: the order can still be placed with this address */ }
}

// Reload the saved shipping addresses (refreshes the Default badges) without disturbing the current
// selection or the entered address, so an in-progress quote is preserved.
async function refreshSavedAddresses () {
  try {
    const book = await accountApi.addressBook()
    if (Array.isArray(book && book.shipping)) savedAddresses.value = book.shipping
  } catch (e) { /* ignore */ }
}

// Make an already-saved address the default (active) delivery address. Uses the dedicated set-default
// endpoint (only the id) so it works even if the address is missing a field the full-update validator
// requires — flipping the default must not depend on re-validating the whole address.
async function setSavedAddressDefault (a) {
  if (a.isDefault || settingDefaultId.value) return
  settingDefaultId.value = a.id
  try {
    await accountApi.setDefaultAddress(a.id)
    // Move the default flag in the local list right away so the badge/button update immediately and
    // reliably — independent of any re-fetch (a cached address-book GET could otherwise show the old
    // default). The chosen address becomes the only default; all others lose it (one default per type).
    savedAddresses.value = savedAddresses.value.map((x) => ({ ...x, isDefault: x.id === a.id }))
    // Also make it the selected delivery address (fills the form) — setting default selects it too.
    selectSaved(a)
    notify.success('Default delivery address updated')
  } catch (e) {
    notify.error(getApiErrorMessage(e))
  } finally {
    settingDefaultId.value = null
  }
}

// ---- Guest contact persistence ----------------------------------------------
// Save the contact + delivery details to localStorage so a guest who must create an account can sign
// in and come straight back to a fully-filled checkout (cleared once the order is placed). Restored on
// mount for everyone, so returning after login/registration re-populates the form automatically.
const CHECKOUT_KEY = 'sf.checkout.contact'
function persistContact () {
  try {
    LocalStorage.set(CHECKOUT_KEY, { email: email.value, addr: addr.value })
  } catch (e) { /* storage disabled — non-fatal */ }
}
function restoreContact () {
  try {
    const saved = LocalStorage.getItem(CHECKOUT_KEY)
    if (saved && typeof saved === 'object') {
      if (typeof saved.email === 'string' && !email.value) email.value = saved.email
      if (saved.addr && typeof saved.addr === 'object') addr.value = { ...emptyAddress(), ...saved.addr }
    }
  } catch (e) { /* ignore malformed */ }
}
function clearContact () {
  try { LocalStorage.remove(CHECKOUT_KEY) } catch (e) { /* ignore */ }
}
watch([email, addr], persistContact, { deep: true })

const emailValid = computed(() => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim()))

// Vuelidate-style field object driving AppTextField's inline error (stacked label, shown on blur).
const emailTouched = ref(false)
const emailV = computed(() => {
  const errors = []
  if (emailTouched.value) {
    const val = email.value.trim()
    if (!val) errors.push({ $message: 'Email is required' })
    else if (!emailValid.value) errors.push({ $message: 'Enter a valid email' })
  }
  return { $error: errors.length > 0, $errors: errors, $touch: () => { emailTouched.value = true } }
})

// The phone emits E.164 when valid, otherwise raw digits; isValidPhoneNumber gates on a real number.
const phoneValid = computed(() => {
  const p = (addr.value.phoneNumber || '').trim()
  return !!p && isPossiblePhoneNumber(p)
})

// Who the order is for — needed either way, and all pickup needs.
const contactComplete = computed(() => {
  const a = addr.value
  return (
    !!(a.firstName || '').trim() &&
    !!(a.lastName || '').trim() &&
    emailValid.value &&
    phoneValid.value
  )
})

const requiredComplete = computed(() => {
  if (!contactComplete.value) return false
  // Collecting in store — nothing is shipped anywhere, so a postal address would be noise.
  if (isPickup.value) return true
  const a = addr.value
  return (
    !!(a.addressLine1 || '').trim() &&
    !!(a.stateProvince || '').trim() &&
    !!(a.postalCode || '').trim() &&
    !!a.countryCode
  )
})

// ---- Quote ------------------------------------------------------------------
const quote = ref(null)
const quoting = ref(false)
const quoteError = ref('')
const selectedShippingMethodId = ref(null)

const shippingOptions = computed(() => quote.value?.shippingOptions || [])
// An empty option list never means "nothing to ship": no product can be marked non-shippable, and pickup
// prices its own zero-cost option. It always means no rate source could quote this address — a broken
// shipping setup or an unserviceable destination — so it blocks placement rather than waving the order
// through with no carrier and no shipping cost.
const shippingUnavailable = computed(
  () => !!quote.value && !quoting.value && !shippingOptions.value.length
)

// The server recommends at most one option, and only when selection is automatic. When it does, show that
// one and collapse the rest; otherwise every option is primary and the expander stays empty.
const recommendedShipping = computed(() => shippingOptions.value.find((o) => o.isRecommended) || null)
const primaryShippingOptions = computed(() =>
  recommendedShipping.value ? [recommendedShipping.value] : shippingOptions.value
)
const otherShippingOptions = computed(() =>
  recommendedShipping.value ? shippingOptions.value.filter((o) => !o.isRecommended) : []
)
const showOtherShipping = ref(false)

function deliveryText (opt) {
  const days = opt.estimatedDeliveryDays
  if (days == null) return ''
  if (days === 0) return 'same day'
  return days === 1 ? 'arrives in 1 day' : `arrives in ${days} days`
}

// ---- Active calculation partners (shipping carriers + tax provider), shown like the payment partners ----
// Distinct carriers behind the current shipping options — each option already reports its carrier.
const CARRIER_META = {
  Custom: { label: 'Store rates', icon: 'o_store' },
  Pickup: { label: 'Store pickup', icon: 'o_storefront' },
  USPS: { label: 'USPS', icon: 'o_local_post_office' },
  UPS: { label: 'UPS', icon: 'o_local_shipping' },
  FedEx: { label: 'FedEx', icon: 'o_local_shipping' },
  'DHL Express': { label: 'DHL Express', icon: 'o_local_shipping' }
}
const shippingCarriers = computed(() => {
  const seen = new Set()
  const out = []
  for (const o of shippingOptions.value) {
    if (!o.carrier || seen.has(o.carrier)) continue
    seen.add(o.carrier)
    out.push({ carrier: o.carrier, ...(CARRIER_META[o.carrier] || { label: o.carrier, icon: 'o_local_shipping' }) })
  }
  return out
})

// The active tax provider reported by the quote, mapped to a label + icon for the summary.
const TAX_PROVIDER_META = {
  TaxJar: { label: 'TaxJar', icon: 'o_receipt_long' },
  StripeTax: { label: 'Stripe Tax', icon: 'o_credit_card' },
  FlatRate: { label: 'Flat rate', icon: 'o_percent' }
}
const taxProviderInfo = computed(() => {
  const p = quote.value?.taxProvider
  return p ? (TAX_PROVIDER_META[p] || { label: p, icon: 'o_account_balance' }) : null
})
const isRoutable = computed(() => !!quote.value?.isRoutable)
const guestOrderingAllowed = computed(() => (quote.value ? quote.value.guestOrderingAllowed !== false : true))
const canCollectDetails = computed(() => !!quote.value && isRoutable.value && guestOrderingAllowed.value)
const selectedShipping = computed(
  () => shippingOptions.value.find((o) => o.methodId === selectedShippingMethodId.value) || null
)
// Friendly name of the integration/carrier behind the selected shipping method (summary tooltip).
const shippingIntegrationName = computed(() => {
  const c = selectedShipping.value?.carrier
  return c ? (CARRIER_META[c] || { label: c }).label : null
})
const canQuote = computed(() =>
  requiredComplete.value && items.value.length > 0 && (!isPickup.value || !!pickupStoreId.value)
)

// Mode-aware calls to action: under pickup there is no shipping to calculate, only a total.
const quoteLabel = computed(() => {
  if (isPickup.value) return quote.value ? 'Recalculate total' : 'Calculate total'
  return quote.value ? 'Recalculate shipping' : 'Save & calculate shipping'
})

// ---- Guest → account (express registration) ---------------------------------
// When a store disallows guest ordering, turn the already-entered checkout details into an account:
// the backend creates a verified customer, emails a password and saves the delivery address. The buyer
// signs in and returns here (details preserved) to place the order.
const registering = ref(false)
const loginTo = computed(() => ({ name: 'shop-login', query: { redirect: route.fullPath } }))

async function registerAndContinue () {
  if (!canQuote.value || registering.value) return
  registering.value = true
  try {
    const recaptchaToken = await getRecaptchaToken('register')
    const a = addr.value
    const res = await customerAuthApi.registerAtCheckout({
      email: email.value.trim(),
      firstName: (a.firstName || '').trim(),
      lastName: (a.lastName || '').trim(),
      phoneNumber: (a.phoneNumber || '').trim() || null,
      addressLine1: (a.addressLine1 || '').trim(),
      addressLine2: (a.addressLine2 || '').trim() || null,
      landmark: (a.landmark || '').trim() || null,
      city: (a.city || '').trim(),
      stateProvince: (a.stateProvince || '').trim() || null,
      postalCode: (a.postalCode || '').trim(),
      countryCode: a.countryCode,
      recaptchaToken
    })
    persistContact()
    notify.success(
      `We've emailed a password to ${res.email}. Sign in to finish your order — your details are saved.`
    )
    router.push({ name: 'shop-login', query: { redirect: route.fullPath } })
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    registering.value = false
  }
}

function buildShipTo () {
  const a = addr.value
  const contact = {
    firstName: (a.firstName || '').trim(),
    lastName: (a.lastName || '').trim(),
    email: email.value.trim(),
    phoneNumber: (a.phoneNumber || '').trim() || null
  }
  // A pickup order ships nowhere — routing and tax both use the store's own address. Send the collector's
  // contact details with no postal address rather than stamping the order with one it never delivers to
  // (a signed-in buyer's saved address is still sitting in `addr` here).
  if (isPickup.value) {
    return { ...contact, line1: '', line2: null, city: '', region: null, postalCode: null, countryCode: '', landmark: null }
  }
  return {
    ...contact,
    line1: (a.addressLine1 || '').trim(),
    line2: (a.addressLine2 || '').trim() || null,
    city: (a.city || '').trim(),
    region: (a.stateProvince || '').trim() || null,
    postalCode: (a.postalCode || '').trim() || null,
    countryCode: a.countryCode,
    landmark: (a.landmark || '').trim() || null
  }
}

async function runQuote () {
  if (!canQuote.value) return
  quoting.value = true
  quoteError.value = ''
  try {
    const q = await checkoutApi.quote({
      cartId: null,
      sessionId: sessionId.value,
      shipTo: buildShipTo(),
      shippingMethodId: selectedShippingMethodId.value || null,
      pickupInStore: isPickup.value,
      pickupStoreId: isPickup.value ? pickupStoreId.value : null
    })
    quote.value = q
    // Keep a valid shipping selection, defaulting to whatever the quote actually priced. This mirrors the
    // server's rule (recommended, else cheapest) — if the two disagreed, the radio would show one option
    // while the totals reflected another.
    const opts = q.shippingOptions || []
    if (!opts.some((o) => o.methodId === selectedShippingMethodId.value)) {
      const recommended = opts.find((o) => o.isRecommended)
      const cheapest = [...opts].sort((a, b) => (a.rate ?? 0) - (b.rate ?? 0))[0]
      const fallback = recommended || cheapest
      selectedShippingMethodId.value = fallback ? fallback.methodId : null
    }
    // Keep a selection made from the collapsed group visible rather than appearing to lose it.
    if (selectedShippingMethodId.value && opts.some((o) => o.methodId === selectedShippingMethodId.value && !o.isRecommended) &&
        opts.some((o) => o.isRecommended)) {
      showOtherShipping.value = true
    }
    // Shipping is calculated → persist the new address to the signed-in buyer's account (best-effort).
    await maybeSaveNewAddress()
  } catch (err) {
    quote.value = null
    quoteError.value = getApiErrorMessage(err)
  } finally {
    quoting.value = false
  }
}

// The buyer explicitly calculates shipping via the button. Any change to a pricing-relevant field
// afterwards invalidates the quote so they must recalculate before Place order re-enables.
const quoteSignature = computed(() => {
  const a = addr.value
  return JSON.stringify([
    (a.addressLine1 || '').trim(),
    (a.addressLine2 || '').trim(),
    (a.city || '').trim(),
    (a.stateProvince || '').trim(),
    (a.postalCode || '').trim(),
    a.countryCode || '',
    email.value.trim(),
    // Switching fulfilment, or collecting from a different store, re-prices everything (tax included).
    fulfilment.value,
    pickupStoreId.value || ''
  ])
})

watch(quoteSignature, () => {
  quote.value = null
  quoteError.value = ''
  selectedShippingMethodId.value = null
})

// Selecting a different shipping method re-quotes immediately for real-time totals.
function onShippingChange (methodId) {
  selectedShippingMethodId.value = methodId
  runQuote()
}

// ---- Payment ----------------------------------------------------------------
// Label + icon for every supported gateway. The checkout shows only the methods the quote reports as
// available (active/enabled gateways + Cash on Delivery when the fulfilling store allows it).
const PAYMENT_METHOD_META = {
  CashOnDelivery: { label: 'Cash on delivery', icon: 'o_payments', hint: 'Pay in cash when your order is delivered' },
  Stripe: { label: 'Stripe', icon: 'o_credit_card', hint: 'Visa, Mastercard, Amex & more — via Stripe' },
  PayPal: { label: 'PayPal', icon: 'o_account_balance_wallet', hint: 'Pay with your PayPal account' },
  Razorpay: { label: 'Razorpay', icon: 'o_payment', hint: 'Cards, UPI, netbanking & wallets' },
  Square: { label: 'Square', icon: 'o_qr_code_2', hint: 'Secure card payment via Square' },
  AuthorizeNet: { label: 'Authorize.Net', icon: 'o_credit_score', hint: 'Secure card payment' },
  BankTransfer: { label: 'Bank transfer', icon: 'o_account_balance', hint: 'Transfer to our bank account; we confirm once received' }
}
// The quote reports each offered method with its live/sandbox mode: { method, isProduction }
// (isProduction is null for manual methods like COD/Bank transfer, which have no gateway environment).
const paymentMethods = computed(() =>
  (quote.value?.availablePaymentMethods || [])
    .filter((m) => PAYMENT_METHOD_META[m.method])
    .map((m) => ({ value: m.method, isProduction: m.isProduction, ...PAYMENT_METHOD_META[m.method] }))
)
const paymentMethod = ref(null)
// Keep the selection valid as availability changes — default to the first offered method.
watch(paymentMethods, (methods) => {
  if (!methods.length) paymentMethod.value = null
  else if (!methods.some((m) => m.value === paymentMethod.value)) paymentMethod.value = methods[0].value
}, { immediate: true })

// ---- Place ------------------------------------------------------------------
const placing = ref(false)
const placeError = ref('')
const orderResult = ref(null)
// Redirect-payment (Stripe Checkout) return handling.
const confirming = ref(false)
const retrying = ref(false)
const retryState = ref(null) // { orderId, message } while a payment is cancelled/unconfirmed

const canPlace = computed(
  () =>
    !!quote.value &&
    isRoutable.value &&
    guestOrderingAllowed.value &&
    items.value.length > 0 &&
    !!selectedShippingMethodId.value &&
    !!paymentMethod.value &&
    !placing.value
)

async function placeOrder () {
  placeError.value = ''
  const ok = await formRef.value.validate()
  if (!ok || !canPlace.value) return
  placing.value = true
  try {
    // Signed-in buyer entering a new address who opted to save it → add it to their account (best-effort).
    if (!isPickup.value && isAuthed.value && selectedAddressId.value === 'new' && saveNewAddress.value) {
      await saveCurrentAddress().catch(() => {})
    }
    // reCAPTCHA token for the guest-checkout form (no-op when reCAPTCHA is disabled/unconfigured).
    const recaptchaToken = await getRecaptchaToken('guestCheckout')
    const result = await checkoutApi.place({
      cartId: null,
      sessionId: sessionId.value,
      shipTo: buildShipTo(),
      selectedShippingMethodId: selectedShippingMethodId.value,
      paymentMethod: paymentMethod.value,
      paymentToken: null,
      couponCode: null,
      recaptchaToken,
      pickupInStore: isPickup.value,
      pickupStoreId: isPickup.value ? pickupStoreId.value : null
    })
    if (result && result.redirectUrl) {
      // Redirect gateway (Stripe Checkout): hand off to the hosted payment page. The buyer returns to
      // this page with ?order=…&session_id=… (success) or ?order=…&cancelled=1, handled on mount.
      window.location.href = result.redirectUrl
      return
    }
    if (result && result.success) {
      orderResult.value = result
      // Order placed — the saved checkout details are no longer needed.
      clearContact()
      // The cart is now checked out; refreshing yields a fresh empty cart (badge resets).
      await refresh().catch(() => {})
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } else {
      placeError.value = (result && result.error) || 'Payment could not be processed. Please try again.'
      notify.error(placeError.value)
    }
  } catch (err) {
    placeError.value = getApiErrorMessage(err)
    notify.error(placeError.value)
  } finally {
    placing.value = false
  }
}

// Handle the buyer's return from a redirect gateway (Stripe Checkout). Success carries ?order&session_id
// (verify the payment server-side and finalize); ?cancelled shows the retry card. The payment params are
// stripped from the URL so a refresh doesn't re-trigger.
async function handlePaymentReturn () {
  const q = route.query || {}
  const orderId = q.order
  if (!orderId) return

  const cleanQuery = { ...q }
  delete cleanQuery.order
  delete cleanQuery.session_id
  delete cleanQuery.cancelled
  router.replace({ query: cleanQuery }).catch(() => {})

  if (q.cancelled) {
    retryState.value = { orderId, message: 'Your payment was cancelled before it completed.' }
    return
  }

  confirming.value = true
  try {
    const result = await checkoutApi.confirm(orderId)
    if (result && result.success) {
      orderResult.value = result
      clearContact()
      await refresh().catch(() => {})
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } else {
      retryState.value = { orderId, message: (result && result.error) || 'We could not confirm your payment.' }
    }
  } catch (err) {
    retryState.value = { orderId, message: getApiErrorMessage(err) }
  } finally {
    confirming.value = false
  }
}

// Re-open a payment session for the pending order and send the buyer back to the gateway.
async function retryPayment () {
  if (!retryState.value) return
  retrying.value = true
  try {
    const result = await checkoutApi.retryPayment(retryState.value.orderId)
    if (result && result.redirectUrl) {
      window.location.href = result.redirectUrl
      return
    }
    if (result && result.success) {
      // The order was already paid — show the confirmation instead.
      orderResult.value = result
      retryState.value = null
      await refresh().catch(() => {})
    } else {
      notify.error((result && result.error) || 'Could not restart payment. Please try again.')
    }
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    retrying.value = false
  }
}

onMounted(() => {
  loadCurrencies()
  refresh().catch((err) => notify.error(getApiErrorMessage(err)))
  // Returning from a redirect payment (Stripe Checkout)? Verify/settle it before anything else.
  handlePaymentReturn()
  // Which stores allow collection — decides whether the fulfilment choice is offered at all.
  loadPickupStores()
  // Signed-in buyers get their profile + saved addresses (pick-or-add); guests restore any draft.
  if (isAuthed.value) loadAccountAddresses()
  else restoreContact()
})
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1200px;
  margin: 0 auto;
}

.sf-address-option {
  border: 1px solid rgba(0, 0, 0, 0.16);
  transition: border-color 0.15s ease, background 0.15s ease;
}

// Let a store's address wrap onto a few readable lines instead of one long strip.
.sf-store-tooltip {
  max-width: 260px;
}
.sf-address-option--active {
  border-color: var(--q-primary);
  background: rgba(0, 0, 0, 0.02);
}
body.body--dark .sf-address-option {
  border-color: rgba(255, 255, 255, 0.22);
}
body.body--dark .sf-address-option--active {
  background: rgba(255, 255, 255, 0.05);
}

.sf-payment-option {
  border: 1px solid rgba(0, 0, 0, 0.16);
  transition: border-color 0.15s ease, background 0.15s ease, box-shadow 0.15s ease;
}
.sf-payment-option:hover {
  border-color: var(--q-primary);
}
.sf-payment-option--active {
  border-color: var(--q-primary);
  background: rgba(0, 0, 0, 0.02);
  box-shadow: inset 0 0 0 1px var(--q-primary);
}
.sf-payment-icon {
  background: rgba(0, 0, 0, 0.05);
  color: var(--q-primary);
}
body.body--dark .sf-payment-option {
  border-color: rgba(255, 255, 255, 0.22);
}
body.body--dark .sf-payment-option--active {
  background: rgba(255, 255, 255, 0.05);
}
body.body--dark .sf-payment-icon {
  background: rgba(255, 255, 255, 0.08);
}

.confirm-card {
  max-width: 640px;
}
.sf-txn-id {
  font-family: 'Roboto Mono', monospace;
  word-break: break-all;
}
.sf-partner-chip {
  background: rgba(0, 0, 0, 0.05);
}
body.body--dark .sf-partner-chip {
  background: rgba(255, 255, 255, 0.08);
}

.summary-card {
  position: sticky;
  top: 88px;
}
</style>
