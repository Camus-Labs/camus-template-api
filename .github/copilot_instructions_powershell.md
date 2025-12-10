# GitHub Copilot Instructions for Powershell Scripts


## Project Context
**AI Role**: Maintain consistent, standards-based code across all project files


## 🚨 CRITICAL ENFORCEMENT RULES


### 1. NAMING CONVENTIONS (MANDATORY)
| Type | Convention | Examples |
|------|------------|----------|
| **Constants** | `UPPER_SNAKE_CASE` | `$SA_ENTERPRISE`, `$VALID_PARAMS`, `$HEALTH_CHECKS` |
| **Global Variables** | `PascalCase` | `$Result`, `$DownloadPath`, `$InstallationStatus` |
| **Local Variables** | `camelCase` | `$failedCount`, `$checkResult`, `$filePath` |
| **Script Parameters** | `snake_case` | `$sas_token`, `$exec_mode`, `$binaries_type` |
| **Functions** | `Verb-Noun` | `Get-FormattedEdition`, `Test-PathAvailability` |
| **Private Functions** | `_PascalCase` | `_ValidateInput`, `_LogMessage` |
| **Function Parameters** | `PascalCase` | `ValueToTest`, `ValidLog` |
| **JSON Output** | `snake_case` | `instance_name`, `status_code`, `check_details` |


### 2. DOCUMENTATION STANDARDS (ALWAYS REQUIRED)


#### Script Header (Required at top of every script):
```powershell
# ======================================================================================
# <SCRIPT NAME>
# ======================================================================================
# Purpose: <WHAT THE SCRIPT DOES>
# Usage  : <HOW TO RUN THE SCRIPT>
# Output : <WHAT THE SCRIPT PRODUCES>
# Dependencies: <REQUIRED TOOLS/MODULES>
# ======================================================================================
```


#### Function Header (Required above every function):
```powershell
# -----------------------------------------------------------------------------
# Function: Verb-Noun
# Purpose : <IMPERATIVE PURPOSE>
# Params  : <PARAM_NAME (TYPE) - DESCRIPTION>
# Returns : <TYPE AND MEANING>
# Notes   : <SIDE EFFECTS/ERRORS> (optional)
# -----------------------------------------------------------------------------
```


### 3. ERROR HANDLING PATTERN


#### Structure (MANDATORY):
```powershell
$Result = @{ status_code = -1; status_details = 'Not Completed' }


try {
    # Main Logic:
    # On Success:
    $Result.status_code = 0
    $Result.status_details = "Completed Successfully"
   
} catch {
    $Result.status_code = 1
    $Result.status_details = "Failed: $($_.Exception)"
   
} finally {
    Write-Host ($Result | ConvertTo-Json -Depth 3)
}
```


#### Status Code Rules:
- `-1` = Incomplete execution (initial state)
- `0` = Complete success → Success JSON format
- `1` = Complete failure → Error JSON format (ONLY in catch block)
- `2` = Other scenarios as defined by user.


### 4. SCRIPT STRUCTURE STANDARDS


#### Variable Initialization:
```powershell
# Constants (readonly, script-level)
$CONSTANT_NAME = 'value'


# Globals (script-level, mutable)  
$GlobalVar = $paramValue ?? 'default'


# Locals (function-level)
$localVar = $ParamValue ?? $defaultValue
```


#### Function Design:
- **Initialize all locals** at function start
- **Semantic naming** - names reflect purpose
- **Single exit point** - exactly one final `return` statement per function
- **Dynamic return values** - use variables, not hardcoded outputs (e.g., `return $Result"` not `return 'hardcoded'`)
- **No early exits** - all function exits occur only at the final statement (no returns inside conditional blocks)
- **No Where-Object filtering** - PowerShell pipeline filtering can be unreliable in certain scenarios


### 5. INPUT/OUTPUT STANDARDS


#### Parameter Processing:
```powershell
param(
    [Parameter(Mandatory = $true)]
    [string]$required_param,
   
    [Parameter(Mandatory = $false)]
    [string]$optional_param = "default"
)


# Process with validation
$processedValue = $required_param ?? 'fallback'
```


#### JSON Output:
```powershell
# Success Format (status_code = 0 only)
@{
    status_code = 0
    status_details = "descriptive message"
    task_specific_data = "values"
}


# Error Format (status_code = 1 or 2)
@{
    status_code = 1
    status_details = "descriptive error message"
}
```


### 6. CRITICAL VALIDATION CHECKLIST


Before completing any PowerShell script changes:
- [ ] All constants use `UPPER_SNAKE_CASE`
- [ ] All globals use `PascalCase`
- [ ] All locals use `camelCase`  
- [ ] All script parameters use `snake_case` and function parameters use `PascalCase`
- [ ] Script header present and complete
- [ ] Function headers present for all functions
- [ ] Error handling follows try-catch-finally pattern
- [ ] Status code 1 only set in catch block
- [ ] JSON output uses correct format based on status_code
- [ ] Variable initialization follows scoping rules
- [ ] Function output requirements followed (see Function Design section)
- [ ] Manual counting used instead of Where-Object filtering for collection operations


---
**File Purpose**: Ensure consistent, maintainable PowerShell code 