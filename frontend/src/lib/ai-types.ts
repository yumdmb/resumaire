export interface AiProviderSummary { id: string; name: string; privacyUrl: string }
export interface AiModel { id: string; name: string; supported: boolean; recommended: boolean; usesTrainingData: boolean; note: string | null }
export interface AiCatalog { models: AiModel[]; isFallback: boolean; notice: string | null }
export interface AiSettings {
  hasKey: boolean
  providerId: string | null
  modelId: string | null
  keyLastFour: string | null
  credentialVersion: string | null
  lastValidatedAt: string | null
  trainingAcknowledged: boolean
  providers: AiProviderSummary[]
}
export interface AiConnectionInput {
  providerId: string
  apiKey?: string
  modelId?: string
  refresh?: boolean
  trainingAcknowledged?: boolean
  validationToken?: string
  expectedVersion?: string | null
}
export interface AiTestResult { validationToken: string; testedAt: string; message: string }
