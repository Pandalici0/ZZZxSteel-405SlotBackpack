$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
    Write-Host "[Backpack Config] $msg"
}

function Clamp([int]$value, [int]$min, [int]$max) {
    if ($value -lt $min) { return $min }
    if ($value -gt $max) { return $max }
    return $value
}

function Normalize-Slots([int]$slots) {
    $slots = Clamp $slots 9 45
    $rounded = [Math]::Round($slots / 9.0) * 9
    return [int](Clamp $rounded 9 45)
}

function Get-PackMuleValues([int]$tabs, [int]$slotsPerPage) {
    $baseCarryCapacity = 27
    $rankCount = 5
    $pages = @()

    if ($tabs -le 1) {
        for ($i = 0; $i -lt $rankCount; $i++) { $pages += 1 }
    }
    else {
        for ($rank = 1; $rank -le $rankCount; $rank++) {
            $ratio = ($rank - 1) / ($rankCount - 1)
            $pageValue = 1 + [Math]::Round(($tabs - 1) * $ratio)
            if ($pageValue -lt 1) { $pageValue = 1 }
            if ($pageValue -gt $tabs) { $pageValue = $tabs }
            $pages += [int]$pageValue
        }
    }

    $values = @()
    foreach ($pageCount in $pages) {
        $carryValue = ($pageCount * $slotsPerPage) - $baseCarryCapacity
        if ($carryValue -lt 0) { $carryValue = 0 }
        $values += [string][int]$carryValue
    }

    return ($values -join ',')
}

try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    Set-Location $scriptDir

    $configPath = Join-Path $scriptDir 'config.json'
    $windowsPath = Join-Path $scriptDir 'Config/XUi/windows.xml'
    $entityPath = Join-Path $scriptDir 'Config/entityclasses.xml'
    $progressionPath = Join-Path $scriptDir 'Config/progression.xml'

    if (-not (Test-Path $configPath)) { throw "config.json wurde nicht gefunden." }
    if (-not (Test-Path $windowsPath)) { throw "Config/XUi/windows.xml wurde nicht gefunden." }
    if (-not (Test-Path $entityPath)) { throw "Config/entityclasses.xml wurde nicht gefunden." }
    if (-not (Test-Path $progressionPath)) { throw "Config/progression.xml wurde nicht gefunden." }

    $config = Get-Content $configPath -Raw | ConvertFrom-Json

    $tabs = Clamp ([int]$config.TabsCount) 1 10
    $slots = Normalize-Slots ([int]$config.SlotsPerPage)

    $rows = [int]($slots / 9)
    $bagSize = [int]($tabs * $slots)
    $packMuleValues = Get-PackMuleValues $tabs $slots

    Write-Info "TabsCount = $tabs"
    Write-Info "SlotsPerPage = $slots"
    Write-Info "Rows = $rows"
    Write-Info "BagSize = $bagSize"
    Write-Info "perkPackMule CarryCapacity = $packMuleValues"

    Copy-Item $windowsPath "$windowsPath.bak" -Force
    Copy-Item $entityPath "$entityPath.bak" -Force
    Copy-Item $progressionPath "$progressionPath.bak" -Force

    $tabsXml = @()
    for ($i = 1; $i -le $tabs; $i++) {
        $tabsXml += @"
                            <rect controller="TabSelectorTab" tab_key="$i" >
                                <grid depth="10" name="inventory" rows="$rows" cols="9" pos="3,-3" cell_width="67" cell_height="67" repeat_content="true" >
                                    <backpack_item_stack name="0" on_scroll="true" />
                                </grid>
                            </rect>
"@
    }

    $tabsXmlText = ($tabsXml -join "`r`n")

    $windowsXml = @"
<xSteel>

<insertAfter xpath="//window[@name='windowBackpack']//panel[@name='content']">
    
            <rect name="PaginatedBackpack" pos="-3,-45"  controller="TabSelector" depth="100" >
                <rect name="tabsHeader" pos="4,-340" visible="false" >
                    <grid name="tabButtons" rows="1" cols="$tabs" cell_width="67" cell_height="32" repeat_content="true" >
                        <rect controller="TabSelectorButton">
                            <simplebutton depth="100" name="tabButton" width="65" height="30" font_size="22" sound="[paging_click]" disabled_font_color="0,0,0,0" caption="{tab_name_localized}"  />
                      </rect>
                    </grid>
                </rect>

                <panel name="content" depth="0" disableautobackground="true">
                        <rect name="tabsContents" controller="Backpack" >
$tabsXmlText
                        </rect>
                </panel>
            </rect>
            
</insertAfter>

<remove xpath="//window[@name='windowBackpack']/panel[@name='content']"/>

</xSteel>
"@

    $entityXml = @"
<xSteel>
	<set xpath="//entity_class[@name='playerMale']//passive_effect[@name='BagSize']/@value">$bagSize</set>
</xSteel>
"@

    $progressionXml = @"
<xSteel>
	<!-- Pack Mule progression for the expanded backpack. -->
	<set xpath="//perk[@name='perkPackMule']/effect_group/passive_effect[@name='CarryCapacity' and @value]/@value">$packMuleValues</set>
</xSteel>
"@

    $utf8 = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($windowsPath, $windowsXml, $utf8)
    [System.IO.File]::WriteAllText($entityPath, $entityXml, $utf8)
    [System.IO.File]::WriteAllText($progressionPath, $progressionXml, $utf8)

    Write-Info "Fertig. windows.xml, entityclasses.xml und progression.xml wurden aktualisiert."
    Write-Info "Backups wurden als .bak angelegt."
    exit 0
}
catch {
    Write-Host "[Backpack Config] FEHLER: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
