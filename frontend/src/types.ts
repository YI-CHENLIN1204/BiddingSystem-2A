export type JsonRecord = Record<string, unknown>

export interface SystemOptions {
  projectTypes: string[]
  structureTypes: string[]
  cities: string[]
  materials: MaterialOption[]
}

export interface MaterialOption {
  materialId: number
  materialName: string
  unitName: string
  category?: string
}


export interface ProjectSummary {
  projectId: number
  projectName: string
  projectType?: string
  structureType?: string
  city?: string
  totalFloorArea?: number
  siteArea?: number
  floorCount?: number
  basementFloorCount?: number
  startDate?: string
  hasFinalBid?: boolean
  finalBidItemCount?: number
  historicalTransactionCount?: number
  historyStatus?: string
  historyApprovedAt?: string | null
  historyApprovedBy?: string | null
}

export interface ProjectForm {
  projectName: string
  projectType: string
  structureType: string
  city: string
  totalFloorArea: number | null
  siteArea: number | null
  floorCount: number | null
  basementFloorCount: number | null
  projectMonth: string
}
