export interface StatsOverview {
  onlineClientCount: number
  activeWebTunnelCount: number
  activeForwardTunnelCount: number
  totalTokenCount: number
}

export interface TrafficSeries {
  token: string
  label: string
  data: number[]
}

export interface TrafficResponse {
  hours: string[]
  series: TrafficSeries[]
}
