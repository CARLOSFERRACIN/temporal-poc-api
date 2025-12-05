# PowerShell script to create Search Attributes in Temporal
# Run this from PowerShell

Write-Host "Creating Search Attributes in Temporal..." -ForegroundColor Cyan

# Create ProfileId as Keyword (with auto-confirmation)
docker exec temporal sh -c "echo Y | tctl --address temporal:7233 admin cluster add-search-attributes --name ProfileId --type Keyword"

# Create ExternalOperationId as Keyword (with auto-confirmation)
docker exec temporal sh -c "echo Y | tctl --address temporal:7233 admin cluster add-search-attributes --name ExternalOperationId --type Keyword"

# Create OperationType as Keyword (with auto-confirmation)
docker exec temporal sh -c "echo Y | tctl --address temporal:7233 admin cluster add-search-attributes --name OperationType --type Keyword"

Write-Host "`nSearch Attributes created successfully!" -ForegroundColor Green
Write-Host "ProfileId (Keyword)" -ForegroundColor Yellow
Write-Host "ExternalOperationId (Keyword)" -ForegroundColor Yellow
Write-Host "OperationType (Keyword)" -ForegroundColor Yellow

