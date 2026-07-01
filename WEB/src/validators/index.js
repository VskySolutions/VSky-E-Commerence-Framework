/*
 * Shared validators (WO-94 Step 5/8 support).
 *
 * Re-exports the @vuelidate/validators primitives and adds project validators
 * built on `validator` (emails/urls) and `libphonenumber-js` (phone numbers).
 * Each custom rule is wrapped with a human-readable message via helpers.
 */
import {
  required,
  requiredIf,
  minLength,
  maxLength,
  minValue,
  maxValue,
  numeric,
  integer,
  sameAs,
  helpers
} from '@vuelidate/validators'
import validator from 'validator'
import { isValidPhoneNumber } from 'libphonenumber-js'

const { withMessage, req } = helpers

// Email using the `validator` library (stricter than a naive regex).
export const email = withMessage(
  'Enter a valid email address',
  (value) => !req(value) || validator.isEmail(String(value))
)

// Absolute URL.
export const url = withMessage(
  'Enter a valid URL',
  (value) =>
    !req(value) ||
    validator.isURL(String(value), { require_protocol: true, require_valid_protocol: true })
)

// Strong-ish password: >= 8 chars, at least one letter and one number.
export const strongPassword = withMessage(
  'Password must be at least 8 characters and include a letter and a number',
  (value) =>
    !req(value) ||
    validator.isStrongPassword(String(value), {
      minLength: 8,
      minLowercase: 0,
      minUppercase: 0,
      minNumbers: 1,
      minSymbols: 0
    }) ||
    (/[A-Za-z]/.test(String(value)) && /\d/.test(String(value)) && String(value).length >= 8)
)

// Hex colour (#rgb / #rrggbb).
export const hexColor = withMessage(
  'Enter a valid hex colour',
  (value) => !req(value) || validator.isHexColor(String(value))
)

// Phone number for a given ISO country (defaults to permissive international).
export function phone (defaultCountry) {
  return withMessage('Enter a valid phone number', (value) => {
    if (!req(value)) return true
    try {
      return isValidPhoneNumber(String(value), defaultCountry)
    } catch (e) {
      return false
    }
  })
}

export {
  required,
  requiredIf,
  minLength,
  maxLength,
  minValue,
  maxValue,
  numeric,
  integer,
  sameAs,
  helpers
}

export default {
  required,
  requiredIf,
  minLength,
  maxLength,
  minValue,
  maxValue,
  numeric,
  integer,
  sameAs,
  email,
  url,
  strongPassword,
  hexColor,
  phone
}
