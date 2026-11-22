export interface SearchableProject {
  name: string
  city?: string | null
  projectType?: string | null
  structureType?: string | null
  stage?: string | null
}

export function emptyProjectSearch() {
  return { keyword: '', city: '', projectType: '', structureType: '', stage: '' }
}

export type ProjectSearchFilters = ReturnType<typeof emptyProjectSearch>

export function matchesProject(project: SearchableProject, filters: ProjectSearchFilters) {
  const keyword = filters.keyword.trim().toLowerCase()
  return (!keyword || [project.name, project.city].some(value => value?.toLowerCase().includes(keyword)))
    && (!filters.city || project.city === filters.city)
    && (!filters.projectType || project.projectType === filters.projectType)
    && (!filters.structureType || project.structureType === filters.structureType)
    && (!filters.stage || project.stage === filters.stage)
}
