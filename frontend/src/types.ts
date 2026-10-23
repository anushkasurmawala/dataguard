export interface Summary {
  mean: number;
  median: number;
  standardDeviation: number;
  minimum: number;
  maximum: number;
  q1: number;
  q3: number;
  outlierCount: number;
  outlierPercentage: number;
}
export interface Feature {
  featureName: string;
  dataType: string;
  missingCount: number;
  missingPercentage: number;
  uniqueCount: number;
  numeric: Summary | null;
  categories: Record<string, number> | null;
}
export interface Dataset {
  id: string;
  fileName: string;
  datasetType: string;
  profile: {
    rowCount: number;
    columnCount: number;
    duplicateRowCount: number;
    duplicatePercentage: number;
    totalMissingValues: number;
    overallMissingPercentage: number;
    features: Feature[];
  };
}
export interface FeatureResult {
  featureName: string;
  baselineMissingPercentage: number;
  currentMissingPercentage: number;
  missingChange: number;
  baselineStatistics: Summary | null;
  currentStatistics: Summary | null;
  exclusionReason: string | null;
  drift: {
    psi: number;
    status: string;
    boundaries: number[];
    bins: {
      label: string;
      baselinePercentage: number;
      currentPercentage: number;
    }[];
  } | null;
}
export interface Comparison {
  id: string;
  baseline: Dataset;
  current: Dataset;
  schemaChanges: {
    columnName: string;
    changeType: string;
    baselineType: string | null;
    currentType: string | null;
  }[];
  features: FeatureResult[];
}
