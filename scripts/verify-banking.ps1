[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://127.0.0.1:8080/api',
    [Parameter(Mandatory)] [guid]$SourceAccountId,
    [Parameter(Mandatory)] [guid]$DestinationAccountId,
    [string]$Amount = '100.00'
)

$ErrorActionPreference = 'Stop'
$culture = [Globalization.CultureInfo]::InvariantCulture
$maximum = [decimal]::Parse('9999999999999999.99', $culture)
$api = $BaseUrl.TrimEnd('/')

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}

function Read-Money([string]$value) {
    return [decimal]::Parse($value, $culture)
}

function Read-All([string]$path) {
    $items = @()
    $page = 1
    do {
        $result = Invoke-RestMethod "$api${path}?page=$page&pageSize=100"
        $items += @($result.items)
        $page++
    } while ($items.Count -lt $result.totalCount)
    return $items
}

function Snapshot([guid]$id) {
    return @{
        Balance = Invoke-RestMethod "$api/accounts/$id/balance"
        History = @(Read-All "/accounts/$id/transactions")
    }
}

function Equal-Json($actual, $expected, [string]$message) {
    Assert-True ((ConvertTo-Json -InputObject $actual -Depth 30 -Compress) -ceq
        (ConvertTo-Json -InputObject $expected -Depth 30 -Compress)) $message
}

function Assert-PreviousRecords($before, $after, [string]$name) {
    $byId = @{}
    foreach ($item in $after) { $byId[$item.id] = $item }
    foreach ($item in $before) {
        Assert-True ($byId.ContainsKey($item.id)) "$name lost an existing record."
        Equal-Json $byId[$item.id] $item "$name changed an existing record."
    }
}

Assert-True ($SourceAccountId -ne $DestinationAccountId) 'Select distinct accounts.'
Assert-True ($Amount -cmatch '^[0-9]{1,16}(\.[0-9]{1,2})?$') 'Amount must be an exact decimal string.'
$amountValue = Read-Money $Amount
Assert-True ($amountValue -gt 0 -and $amountValue -le $maximum) 'Amount is outside the supported range.'
$sourceBefore = Snapshot $SourceAccountId
$destinationBefore = Snapshot $DestinationAccountId
$transfersBefore = @(Read-All '/transfers')
$sourceValue = Read-Money $sourceBefore.Balance.balance
$destinationValue = Read-Money $destinationBefore.Balance.balance
Assert-True ($sourceBefore.Balance.currency -ceq 'THB' -and $destinationBefore.Balance.currency -ceq 'THB') 'Both accounts must use THB.'
Assert-True ($sourceValue -ge $amountValue) 'Source has insufficient funds for the successful transfer.'
Assert-True ($destinationValue -le $maximum - $amountValue) 'Destination cannot accept the amount.'
$body = @{ sourceAccountId = "$SourceAccountId"; destinationAccountId = "$DestinationAccountId"; amount = $Amount; currency = 'THB' } | ConvertTo-Json -Compress
$response = Invoke-WebRequest "$api/transfers" -Method Post -ContentType 'application/json' -Body $body -SkipHttpErrorCheck
Assert-True ($response.StatusCode -eq 201) "Expected 201 for the successful transfer; got $($response.StatusCode)."
$completed = $response.Content | ConvertFrom-Json
$sourceAfter = Snapshot $SourceAccountId
$destinationAfter = Snapshot $DestinationAccountId
$transfersAfter = @(Read-All '/transfers')
Assert-True ((Read-Money $sourceAfter.Balance.balance) -eq ($sourceValue - $amountValue)) 'Source balance delta is incorrect.'
Assert-True ((Read-Money $destinationAfter.Balance.balance) -eq ($destinationValue + $amountValue)) 'Destination balance delta is incorrect.'
Assert-True ($sourceAfter.History.Count -eq $sourceBefore.History.Count + 1) 'Source history count is incorrect.'
Assert-True ($destinationAfter.History.Count -eq $destinationBefore.History.Count + 1) 'Destination history count is incorrect.'
Assert-True ($transfersAfter.Count -eq $transfersBefore.Count + 1) 'Transfer count is incorrect.'
Assert-PreviousRecords $sourceBefore.History $sourceAfter.History 'Source history'
Assert-PreviousRecords $destinationBefore.History $destinationAfter.History 'Destination history'
Assert-PreviousRecords $transfersBefore $transfersAfter 'Transfer list'
Assert-True ($completed.entries.Count -eq 2) 'Expected two transfer entries.'
$outgoing = $completed.entries[0]
$incoming = $completed.entries[1]
Assert-True ($outgoing.type -ceq 'TRANSFER_OUT' -and $incoming.type -ceq 'TRANSFER_IN') 'Transfer entry directions are incorrect.'
Assert-True ($outgoing.transferId -ceq $completed.transfer.id -and $incoming.transferId -ceq $completed.transfer.id) 'Transfer IDs differ.'
Assert-True ($outgoing.accountId -ceq "$SourceAccountId" -and $incoming.accountId -ceq "$DestinationAccountId") 'Transfer entry accounts differ.'
Assert-True ($outgoing.counterpartyAccountId -ceq "$DestinationAccountId" -and $incoming.counterpartyAccountId -ceq "$SourceAccountId") 'Counterparties differ.'
Assert-True ((Read-Money $outgoing.amount) -eq $amountValue -and (Read-Money $incoming.amount) -eq $amountValue) 'Transfer amounts differ.'
Assert-True ($outgoing.completedAt -ceq $incoming.completedAt -and $outgoing.completedAt -ceq $completed.transfer.completedAt) 'Transfer timestamps differ.'
Assert-True ($outgoing.currency -ceq 'THB' -and $incoming.currency -ceq 'THB' -and $completed.transfer.currency -ceq 'THB') 'Transfer currencies differ.'
Assert-True ((Read-Money $outgoing.resultingBalance) -eq (Read-Money $sourceAfter.Balance.balance) -and
    (Read-Money $incoming.resultingBalance) -eq (Read-Money $destinationAfter.Balance.balance)) 'Resulting balances differ.'
Assert-True ($completed.transfer.sourceAccountId -ceq "$SourceAccountId" -and
    $completed.transfer.destinationAccountId -ceq "$DestinationAccountId" -and
    (Read-Money $completed.transfer.amount) -eq $amountValue) 'Completed transfer fields differ.'
Equal-Json (@($transfersAfter | Where-Object id -eq $completed.transfer.id)[0]) $completed.transfer 'Completed transfer differs from stored record.'
Equal-Json $sourceAfter.Balance $completed.sourceBalance 'Source result differs from the committed balance.'
Equal-Json $destinationAfter.Balance $completed.destinationBalance 'Destination result differs from the committed balance.'
Equal-Json (@($sourceAfter.History | Where-Object id -eq $outgoing.id)[0]) $outgoing 'Outgoing entry differs from history.'
Equal-Json (@($destinationAfter.History | Where-Object id -eq $incoming.id)[0]) $incoming 'Incoming entry differs from history.'

$excess = (Read-Money $sourceAfter.Balance.balance) + 0.01
Assert-True ($excess -le $maximum) 'Cannot construct a valid excessive amount for this source.'
$rejectedBody = @{ sourceAccountId = "$SourceAccountId"; destinationAccountId = "$DestinationAccountId"; amount = $excess.ToString('F2', $culture); currency = 'THB' } | ConvertTo-Json -Compress
$rejected = Invoke-WebRequest "$api/transfers" -Method Post -ContentType 'application/json' -Body $rejectedBody -SkipHttpErrorCheck
Assert-True ($rejected.StatusCode -eq 409) "Expected 409 for insufficient funds; got $($rejected.StatusCode)."
Assert-True (($rejected.Content | ConvertFrom-Json).code -ceq 'insufficient_funds') 'Unexpected rejection code.'
$sourceFinal = Snapshot $SourceAccountId
$destinationFinal = Snapshot $DestinationAccountId
Equal-Json $sourceFinal.Balance $sourceAfter.Balance 'Rejected transfer changed source balance.'
Equal-Json $destinationFinal.Balance $destinationAfter.Balance 'Rejected transfer changed destination balance.'
Equal-Json $sourceFinal.History $sourceAfter.History 'Rejected transfer changed source history.'
Equal-Json $destinationFinal.History $destinationAfter.History 'Rejected transfer changed destination history.'
Equal-Json @(Read-All '/transfers') $transfersAfter 'Rejected transfer changed transfers.'
Write-Host "Verified transfer $($completed.transfer.id) and unchanged state after insufficient-funds rejection."
