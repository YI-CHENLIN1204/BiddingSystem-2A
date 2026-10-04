export type JsonRecord = Record<string, unknown>

export interface SystemOptions {
  projectTypes: string[]
  structureTypes: string[]
  cities: string[]
  materials: MaterialOption[]
  coreMaterials: CoreMaterial[]
}

export interface MaterialOption {
  materialId: number
  materialName: string
  unitName: string
  category?: string
}

export interface CoreMaterial extends MaterialOption {
  projectType?: string
  structureType?: string
  isCoreEstimation: boolean
  validProjectCount?: number
  historyRowCount?: number
}

export interface ProjectSummary {
  projectId: number
  projectName: string
  projectType?: string
  structureType?: string
  city?: string
  totalFloorArea?: number
  floorCount?: number
  basementFloorCount?: number
  startDate?: string
  hasFinalBid?: boolean
  finalBidItemCount?: number
  historicalTransactionCount?: number
}

export interface ProjectForm {
  projectName: string
  projectType: string
  structureType: string
  city: string
  totalFloorArea: number | null
  floorCount: number | null
  basementFloorCount: number | null
  projectMonth: string
}
