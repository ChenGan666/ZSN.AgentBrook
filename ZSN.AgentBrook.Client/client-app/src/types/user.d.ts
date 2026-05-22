export interface MemberInfo {
  id: string
  name: string
  phone: string
  avatar: string
  email?: string
}

export interface ServerMemberInfo {
  ID: string
  MNickName: string
  MPhoneNumber: string
  MAvatar: string
  MEmail?: string
  MCompany?: string
}

export interface DeviceInfo {
  SystemId: string
  ComputerName: string
  OSDescription: string
  OSArchitecture: string
  Is64BitOS: boolean
  DotNetVersion: string
  SystemBootTime: string
  CurrentUserName: string
  SystemDirectory: string
  CpuInfo: string
  MemoryInfo: string
  HostName: string
  PrimaryIpAddress: string
  PrimaryMacAddress: string
  IsNetworkAvailable: boolean
  NetworkInterfaces: string[]
  UpTimeMilliseconds: number
  FormattedUpTime: string
  CollectionTime: string
}
