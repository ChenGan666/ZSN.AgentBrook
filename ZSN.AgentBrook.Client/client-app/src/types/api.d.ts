export interface ApiResponse<T = any> {
  Success: boolean
  ErrorCode: number
  ErrorDesc: string
  Data: T
}

export interface TokenResponse {
  AccessToken: string
  Expirein: number
  MemberToken: string
  MemberRefreshToken: string
}

export interface PageData<T> {
  Data: T
  pagetotal: number
  total: number
}

export interface ApiRequest {
  AppID: string
  Data: string
  Timestamp: string
  Sign: string
}
