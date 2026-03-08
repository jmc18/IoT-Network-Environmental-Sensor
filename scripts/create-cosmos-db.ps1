# Create Cosmos DB database and container for IoT sensor readings
# Requires: Az.CosmosDB module (Install-Module Az.CosmosDB)
# Usage: .\create-cosmos-db.ps1 -ResourceGroupName "myRG" -AccountName "mycosmosaccount"

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    [Parameter(Mandatory = $true)]
    [string]$AccountName,
    [string]$DatabaseName = "iot-sensors",
    [string]$ContainerName = "readings",
    [string]$PartitionKeyPath = "/nodeId"
)

$location = "westeurope"

Write-Host "Creating Cosmos DB account $AccountName (if not exists)..."
$account = New-AzCosmosDBAccount -ResourceGroupName $ResourceGroupName -Name $AccountName -Location $location -DefaultConsistencyLevel Session -ErrorAction SilentlyContinue
if (-not $account) {
    $account = Get-AzCosmosDBAccount -ResourceGroupName $ResourceGroupName -Name $AccountName
}

Write-Host "Creating database $DatabaseName..."
New-AzCosmosDBSqlDatabase -AccountName $AccountName -ResourceGroupName $ResourceGroupName -Name $DatabaseName -ErrorAction SilentlyContinue

Write-Host "Creating container $ContainerName with partition key $PartitionKeyPath..."
$throughput = 400  # RU/s
New-AzCosmosDBSqlContainer -AccountName $AccountName -ResourceGroupName $ResourceGroupName -DatabaseName $DatabaseName -Name $ContainerName -PartitionKeyPath $PartitionKeyPath -Throughput $throughput

Write-Host "Done. Get connection string from Azure Portal: Cosmos DB account -> Keys"
