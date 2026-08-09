using 'main.bicep'

param location = 'canadacentral'
param environmentName = 'prd'
param apiImageTag = 'latest'
param webImageTag = 'latest'
param createdBy = 'cameron.mckay'

// Secrets — supply at deployment time via --parameters or environment variables:
// param jwtSigningKey = ''
// param googleClientId = ''
// param googleClientSecret = ''
