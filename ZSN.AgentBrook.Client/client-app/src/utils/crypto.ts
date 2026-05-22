import CryptoJS from 'crypto-js'

const APP_ID = import.meta.env.VITE_APP_ID || ''
const APP_SECRET = import.meta.env.VITE_APP_SECRET || ''

export { APP_ID, APP_SECRET }

export function md5(str: string): string {
  return CryptoJS.MD5(str).toString(CryptoJS.enc.Hex)
}

export function doubleMd5(str: string): string {
  return md5(md5(str))
}

export function aesEncrypt(plaintext: string, key: string): string {
  const keyStr = key.substring(0, 16).padEnd(16, ' ')
  const keyUtf8 = CryptoJS.enc.Utf8.parse(keyStr)
  const ivUtf8 = CryptoJS.enc.Utf8.parse(keyStr)

  const encrypted = CryptoJS.AES.encrypt(plaintext, keyUtf8, {
    iv: ivUtf8,
    mode: CryptoJS.mode.CBC,
    padding: CryptoJS.pad.Pkcs7,
  })

  return encrypted.toString()
}

export function aesDecrypt(ciphertext: string, key: string): string {
  const keyStr = key.substring(0, 16).padEnd(16, ' ')
  const keyUtf8 = CryptoJS.enc.Utf8.parse(keyStr)
  const ivUtf8 = CryptoJS.enc.Utf8.parse(keyStr)

  const decrypted = CryptoJS.AES.decrypt(ciphertext, keyUtf8, {
    iv: ivUtf8,
    mode: CryptoJS.mode.CBC,
    padding: CryptoJS.pad.Pkcs7,
  })

  return decrypted.toString(CryptoJS.enc.Utf8)
}

export function generateSign(
  apiRequest: { AppID: string; Data: string; Timestamp: string },
  secret: string,
): string {
  const dic: Record<string, string> = {
    AppID: apiRequest.AppID,
    Data: apiRequest.Data,
    Timestamp: apiRequest.Timestamp,
  }

  const sortedKeys = Object.keys(dic).sort((a, b) =>
    a.toLowerCase().localeCompare(b.toLowerCase()),
  )

  let sb = ''
  for (const key of sortedKeys) {
    sb += key.trim()
    sb += dic[key] ?? ''
  }

  const raw = sb.trim() + 'AppKEY' + secret
  return md5(raw).toUpperCase()
}

export interface ApiRequest {
  AppID: string
  Data: string
  Timestamp: string
  Sign: string
}

export function createApiRequest(
  businessData: unknown,
  encryptKey: string,
  secret: string,
): ApiRequest {
  const json = JSON.stringify(businessData)
  const encryptedData = aesEncrypt(json, encryptKey)
  const timestamp = Math.floor(Date.now() / 1000).toString()

  const partial: Omit<ApiRequest, 'Sign'> = {
    AppID: APP_ID,
    Data: encryptedData,
    Timestamp: timestamp,
  }

  const sign = generateSign(partial, secret)

  return { ...partial, Sign: sign }
}
